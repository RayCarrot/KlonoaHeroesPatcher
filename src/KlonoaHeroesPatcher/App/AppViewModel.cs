using BinarySerializer;
using BinarySerializer.GBA;
using BinarySerializer.Klonoa;
using BinarySerializer.Klonoa.KH;
using MahApps.Metro.IconPacks;
using Microsoft.Win32;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace KlonoaHeroesPatcher
{
    public class AppViewModel : BaseViewModel
    {
        #region Constructor

        public AppViewModel()
        {
            NavigationItems = new ObservableCollection<NavigationItemViewModel>();
            PendingRelocatedData = new List<RelocatedData>();

            OpenFileCommand = new RelayCommand(OpenFile);
            SaveFileCommand = new RelayCommand(SaveFile);
            SaveFileAsCommand = new RelayCommand(SaveFileAs);
            GenerateConfigCommand = new RelayCommand(GenerateConfig);
            OpenURLCommand = new RelayCommand(x => OpenURL(x?.ToString()));

            SetTitle();
        }

        #endregion

        #region Logger

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Public Static Properties

        public static AppViewModel Current => App.Current.AppViewModel;

        #endregion

        #region Commands

        public ICommand OpenFileCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand SaveFileAsCommand { get; }
        public ICommand GenerateConfigCommand { get; }
        public ICommand OpenURLCommand { get; }

        #endregion

        #region Public Properties

        public string Title { get; set; }
        public Version CurrentAppVersion => new Version(0, 1, 0, 0);

        public const string ConfigFileName = "Config.json";
        public const string LogFileName = "Log.txt";
        public AppConfig Config { get; set; }

        public Context Context { get; set; }
        public KlonoaHeroesROM ROM { get; set; }
        public PatchedFooter Footer { get; set; }

        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }
        public NavigationItemViewModel SelectedNavigationItem { get; set; }

        public List<RelocatedData> PendingRelocatedData { get; }
        public bool UnsavedChanges => PendingRelocatedData.Any(x => x.IsNewData); // TODO: Warn user when closing window

        #endregion

        #region Private Static Methods

        private static AppConfig LoadConfig()
        {
            if (!File.Exists(ConfigFileName))
                return AppConfig.Default;

            try
            {
                return JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(ConfigFileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load config. Default settings will be used. Error: {ex.Message}", "Error loading config", MessageBoxButton.OK, MessageBoxImage.Error);
                return AppConfig.Default;
            }
        }

        private static void AddNavigationItem(ICollection<NavigationItemViewModel> collection, string title, BinarySerializable obj, ArchiveFile parentArchiveFile)
        {
            FileEditorViewModel editor;
            PackIconMaterialKind icon;
            Color iconColor;

            if (obj is Graphics_File)
            {
                editor = new GraphicsFileEditorViewModel();
                icon = PackIconMaterialKind.FileImageOutline;
                iconColor = Color.FromRgb(0xFF, 0xC1, 0x07);
            }
            else if (obj is Cutscene_File)
            {
                editor = new CutsceneFileEditorViewModel();
                icon = PackIconMaterialKind.FileDocumentOutline;
                iconColor = Color.FromRgb(0x8B, 0xC3, 0x4A);
            }
            else if (obj is ArchiveFile)
            {
                editor = null;
                icon = PackIconMaterialKind.FolderOutline;
                iconColor = Color.FromRgb(0xEF, 0x6C, 0x00);
            }
            else
            {
                editor = obj is RawData_File { Data: { } } ? new BinaryFileEditorViewModel() : null;

                icon = PackIconMaterialKind.FileOutline;
                iconColor = Color.FromRgb(0x8B, 0x00, 0x8B);
            }

            var navItem = new NavigationItemViewModel(title, icon, iconColor, obj, parentArchiveFile, editor);
            collection.Add(navItem);

            if (obj is not ArchiveFile archive)
                return;

            foreach ((BinarySerializable file, string fileName) in archive.ParsedFiles)
                AddNavigationItem(navItem.NavigationItems, fileName, file, archive);
        }

        #endregion

        #region Public Methods

        public void Init()
        {
            Config = LoadConfig();
            InitializeLogging();

            Logger.Info("App initialized");
        }

        public void InitializeLogging()
        {
            // Create a new logging configuration
            var logConfig = new LoggingConfiguration();

#if DEBUG
            // On debug we default it to log trace
            LogLevel logLevel = LogLevel.Trace;
#else
            // If not on debug we default to log info
            LogLevel logLevel = LogLevel.Info;
#endif

            // If the config specifies a log level we use that
            if (Config.LogLevel != null)
                logLevel = LogLevel.FromString(Config.LogLevel);

            const string logLayout = "${time:invariant=true}|${level:uppercase=true}|${logger}|${message}${onexception:${newline}${exception:format=tostring}}";

            // Log to file
            if (Config.UseFileLogging)
            {
                logConfig.AddRule(logLevel, LogLevel.Fatal, new FileTarget("file")
                {
                    KeepFileOpen = true,
                    DeleteOldFileOnStartup = true,
                    FileName = LogFileName,
                    Layout = logLayout,
                });
            }

            // Apply config
            LogManager.Configuration = logConfig;
        }

        public void SetTitle(string status = null)
        {
            Title = $"Klonoa Heroes Patcher ({CurrentAppVersion})";

            if (status != null)
                Title += $" - {status}";
        }

        public void OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open ROM file",
                Filter = "All files (*.*)|*.*",
            };

            var result = dialog.ShowDialog();

            if (result != true)
                return;

            Load(dialog.FileName);
        }

        public void SaveFile()
        {
            Save(ROM.Offset.File.AbsolutePath);
        }

        public void SaveFileAs()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save ROM file",
                Filter = "All files (*.*)|*.*",
            };

            var result = dialog.ShowDialog();

            if (result != true)
                return;

            Save(dialog.FileName);
        }

        public void Load(string romPath)
        {
            Unload();

            // Verify the path
            if (!File.Exists(romPath))
            {
                MessageBox.Show($"The specified ROM file '{romPath}' does not exist", "Error opening file", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetTitle(romPath);

            try
            {
                string basePath = Path.GetDirectoryName(romPath);
                string romName = Path.GetFileName(romPath);

                // Create the context and dispose after finished reading
                using (Context = new KlonoaContext(basePath, Config.SerializerLogPath))
                {
                    // Add the game settings
                    Context.AddKlonoaSettings(new KlonoaSettings_KH());

                    // Add the ROM to the context
                    Context.AddFile(new MemoryMappedFile(Context, romName, GBAConstants.Address_ROM));

                    // Read the patched footer in the ROM first
                    Footer = ReadFooter(romName);

                    // Read the ROM
                    ROM = FileFactory.Read<KlonoaHeroesROM>(romName, Context);

                    // TODO: Add prev relocated data from footer to pending relocate as we need to relocate it again when saving

                    NavigationItems.Clear();

                    AddNavigationItem(NavigationItems, "UIPack", ROM.UIPack, null);
                    AddNavigationItem(NavigationItems, "StoryPack", ROM.StoryPack, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load file. Error: {ex.Message}", "Error loading file", MessageBoxButton.OK, MessageBoxImage.Error);
                Unload();
            }
        }

        public PatchedFooter ReadFooter(string romName)
        {
            BinaryDeserializer s = Context.Deserializer;
            BinaryFile file = Context.GetFile(romName);
            s.Goto(file.StartPointer);

            PatchedFooter footer = new PatchedFooter();

            footer.TryReadFromEnd(s, file.StartPointer);

            return footer;
        }

        public void Save(string romPath)
        {
            using (Context)
            {
                try
                {
                    // Get the file from the context
                    PhysicalFile file = (PhysicalFile)ROM.Offset.File;

                    // Set the destination path
                    file.DestinationPath = Context.NormalizePath(romPath, false);
                    file.RecreateOnWrite = false;

                    if (file.SourcePath != file.DestinationPath)
                        File.Copy(file.SourcePath, file.DestinationPath, true);

                    var relocatePointer = new Pointer(Config.ROMEndPointer, file);
                    var s = Context.Serializer;
                    s.Goto(relocatePointer);

                    Footer.RelocatedStructsCount = PendingRelocatedData.Count;
                    Footer.RelocatedStructs = new PatchedFooter.RelocatedStruct[Footer.RelocatedStructsCount];

                    for (var i = 0; i < PendingRelocatedData.Count; i++)
                    {
                        Footer.RelocatedStructs[i] = PendingRelocatedData[i].Relocate(s);
                        s.Align();
                    }

                    s.SerializeObject<PatchedFooter>(Footer, name: nameof(Footer));

                    MessageBox.Show("The file was successfully saved", "Saved successfully");

                    // Reload the file
                    Load(romPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save file. Error: {ex.Message}", "Error saving file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void Unload()
        {
            Context?.Dispose();
            Context = null;
            ROM = null;
            NavigationItems.Clear();
            SelectedNavigationItem = null;
            PendingRelocatedData.Clear();
            SetTitle();
        }

        public void GenerateConfig()
        {
            File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(AppConfig.Default, Formatting.Indented));

            MessageBox.Show($"Config generated as {ConfigFileName}");
        }

        public void OpenURL(string url)
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}")
            {
                CreateNoWindow = true
            });
        }

        public void AddRelocatedData(RelocatedData data)
        {
            RelocatedData existingData = PendingRelocatedData.FirstOrDefault(x => x.Obj.Offset == data.Obj.Offset);

            if (existingData != null)
                PendingRelocatedData.Remove(existingData);

            PendingRelocatedData.Add(data);
        }

        #endregion
    }
}