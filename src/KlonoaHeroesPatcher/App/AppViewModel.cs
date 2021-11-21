using BinarySerializer;
using BinarySerializer.GBA;
using BinarySerializer.Klonoa;
using BinarySerializer.Klonoa.KH;
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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json.Converters;

namespace KlonoaHeroesPatcher
{
    public class AppViewModel : BaseViewModel
    {
        #region Constructor

        public AppViewModel()
        {
            PatchViewModels = new ObservableCollection<PatchViewModel>();
            PendingRelocatedData = new List<RelocatedData>();
            OpenFileCommand = new AsyncRelayCommand(OpenFileAsync);
            SaveFileCommand = new AsyncRelayCommand(SaveFileAsync);
            SaveFileAsCommand = new AsyncRelayCommand(SaveFileAsAsync);
            SelectFontFileCommand = new RelayCommand(() => FilesPageViewModel?.SelectFontFile(ROM));
            GenerateConfigCommand = new RelayCommand(GenerateConfig);
            OpenURLCommand = new RelayCommand(x => OpenURL(x?.ToString()));

            SetTitle();
        }

        #endregion

        #region Logger

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constant Fields

        /// <summary>
        /// The application base path to use for WPF related operations
        /// </summary>
        public const string WPFApplicationBasePath = "pack://application:,,,/KlonoaHeroesPatcher;component/";

        #endregion

        #region Public Static Properties

        public static AppViewModel Current => App.Current.AppViewModel;

        #endregion

        #region Commands

        // File
        public ICommand OpenFileCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand SaveFileAsCommand { get; }
        
        // Edit
        public ICommand SelectFontFileCommand { get; }
        
        // Tools
        public ICommand GenerateConfigCommand { get; }
        
        // Help
        public ICommand OpenURLCommand { get; }

        #endregion

        #region Public Properties

        public string Title { get; set; }
        public Version CurrentAppVersion => new Version(1, 6, 3, 0);

        public const string ConfigFileName = "Config.json";
        public const string LogFileName = "Log.txt";
        public AppConfig Config { get; set; }

        public bool IsLoading { get; set; }

        public Context Context { get; set; }
        public KlonoaHeroesROM ROM { get; set; }
        public PatchedFooter Footer { get; set; }
        public string ROMInfo { get; set; }

        public ObservableCollection<PatchViewModel> PatchViewModels { get; }

        public List<RelocatedData> PendingRelocatedData { get; }
        public bool UnsavedChanges => PendingRelocatedData.Any(x => x.IsNewData);

        // Pages
        public FilesPageViewModel FilesPageViewModel { get; set; }

        #endregion

        #region Private Methods

        private AppConfig LoadConfig()
        {
            if (!File.Exists(ConfigFileName))
                return AppConfig.Default;

            try
            {
                return JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(ConfigFileName), new StringEnumConverter());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load config. Default settings will be used. Error: {ex.Message}", "Error loading config", MessageBoxButton.OK, MessageBoxImage.Error);
                return AppConfig.Default;
            }
        }

        #endregion

        #region Public Methods

        public async Task RunAsync(Action action)
        {
            // Throw if already loading. We could add a lock to wait, but we don't want this to ever happen as the loading should disable the UI.
            if (IsLoading)
                throw new Exception("Attempted to run an async operation while one was already running");

            IsLoading = true;

            try
            {
                await Task.Run(action);
            }
            finally
            {
                IsLoading = false;
            }
        }

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

            Logger.Trace("Set app title to {0}", Title);
        }

        public async Task OpenFileAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open ROM file",
                Filter = "All files (*.*)|*.*",
            };

            var result = dialog.ShowDialog();

            if (result != true)
                return;

            Logger.Info("Opening file...");

            await LoadAsync(dialog.FileName);
        }

        public async Task SaveFileAsync()
        {
            Logger.Info("Saving file...");

            await SaveAsync(ROM.Offset.File.AbsolutePath);
        }

        public async Task SaveFileAsAsync()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save ROM file",
                Filter = "All files (*.*)|*.*",
            };

            var result = dialog.ShowDialog();

            if (result != true)
                return;

            Logger.Info("Saving file as...");

            await SaveAsync(dialog.FileName);
        }

        public async Task LoadAsync(string romPath)
        {
            Unload();
         
            Logger.Info("Loading ROM");

            // Verify the path
            if (!File.Exists(romPath))
            {
                Logger.Warn("ROM file {0} not found", romPath);

                MessageBox.Show($"The specified ROM file '{romPath}' does not exist", "Error opening file", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetTitle(romPath);

            try
            {
                string basePath = Path.GetDirectoryName(romPath);
                string romName = Path.GetFileName(romPath);

                Logger.Trace("Base path: {0}", basePath);
                Logger.Trace("ROM name: {0}", romName);

                // Create the context and dispose after finished reading
                using (Context = new KlonoaContext(basePath, Config.SerializerLogPath))
                {
                    // Add the game settings
                    var settings = new KlonoaSettings_KH();
                    Context.AddKlonoaSettings(settings);

                    // Add the pointers
                    Context.AddPreDefinedPointers(DefinedPointers.GBA_JP);

                    // Add the ROM to the context
                    var romFile = new MemoryMappedFile(Context, romName, GBAConstants.Address_ROM);
                    Context.AddFile(romFile);

                    // Read the patched footer in the ROM first
                    Footer = ReadFooter(romName);

                    // Add the relocated files to the settings to correctly determine the archived file sizes
                    settings.RelocatedFiles = Footer.RelocatedStructs.
                        GroupBy(x => x.ParentArchivePointer).
                        ToDictionary(x => x.Key, x => new HashSet<KlonoaSettings.RelocatedFile>(x.Select(r => new KlonoaSettings.RelocatedFile(r.OriginalPointer, r.NewPointer, r.DataSize))));

                    KlonoaHeroesROM.SerializeDataFlags serializeFlags = KlonoaHeroesROM.SerializeDataFlags.Packs;
                    
                    // Don't parse the map pack normally since it's too slow (due to the map tile objects)
                    serializeFlags &= ~KlonoaHeroesROM.SerializeDataFlags.MapsPack;

                    var s = Context.Deserializer;
                    ArchiveFile<RawData_File> rawMapsPack = null;

                    // Read the ROM
                    await RunAsync(() =>
                    {
                        ROM = FileFactory.Read<KlonoaHeroesROM>(romName, Context, (_, r) => r.Pre_SerializeFlags = serializeFlags);

                        // Read the maps pack as raw data
                        rawMapsPack = s.DoAt(Context.GetPreDefinedPointer(DefinedPointer.MapsPack, ROM.Offset.File), () => s.SerializeObject<ArchiveFile<RawData_File>>(default, x =>
                        {
                            x.Pre_Type = ArchiveFileType.KH_KW;
                            x.Pre_ArchivedFilesEncoder = new BytePairEncoder();
                        }, name: "RawMapsPack"));

                        Logger.Info("Read ROM with {0} relocated structs", Footer.RelocatedStructsCount);

                        foreach (PatchedFooter.RelocatedStruct relocatedStruct in Footer.RelocatedStructs)
                        {
                            var rawData = s.DoAt(relocatedStruct.NewPointer, () => s.SerializeObject<Array<byte>>(default, x => x.Length = relocatedStruct.DataSize));
                            var parentArchiveOffsetTable = Context.Cache.FromOffset<OffsetTable>(relocatedStruct.ParentArchivePointer);

                            AddRelocatedData(new RelocatedData(rawData, parentArchiveOffsetTable)
                            {
                                OriginPointer = relocatedStruct.OriginalPointer
                            });
                        }
                    });

                    FilesPageViewModel = new FilesPageViewModel();

                    FilesPageViewModel.AddNavigationItem(null, Footer, FilesPageViewModel.NavigationItems, nameof(ROM.MenuPack), ROM.MenuPack, null);
                    FilesPageViewModel.AddNavigationItem(null, Footer, FilesPageViewModel.NavigationItems, nameof(ROM.EnemyAnimationsPack), ROM.EnemyAnimationsPack, null);
                    FilesPageViewModel.AddNavigationItem(null, Footer, FilesPageViewModel.NavigationItems, nameof(ROM.GameplayPack), ROM.GameplayPack, null);
                    FilesPageViewModel.AddNavigationItem(null, Footer, FilesPageViewModel.NavigationItems, nameof(ROM.ItemsPack), ROM.ItemsPack, null);
                    FilesPageViewModel.AddNavigationItem(null, Footer, FilesPageViewModel.NavigationItems, nameof(ROM.UIPack), ROM.UIPack, null);
                    FilesPageViewModel.AddNavigationItem(null, Footer, FilesPageViewModel.NavigationItems, nameof(ROM.StoryPack), ROM.StoryPack, null);
                    FilesPageViewModel.AddNavigationItem(null, Footer, FilesPageViewModel.NavigationItems, nameof(ROM.MapsPack), rawMapsPack, null);
                    FilesPageViewModel.AddNavigationItem(null, Footer, FilesPageViewModel.NavigationItems, nameof(ROM.WorldMapPack), ROM.WorldMapPack, null);

                    // Create the patches
                    var patches = new Patch[]
                    {
                        new VariableWidthFontPatch(),
                    };

                    // Create patch view models
                    foreach (Patch patch in patches)
                    {
                        // Attempt to find the patch under applied patch data
                        var patchData = Footer.PatchDatas.FirstOrDefault(x => x.ID == patch.ID);
                        var enabled = patchData != null;

                        // Load the patch if enabled
                        if (enabled)
                        {
                            // Go to the data
                            s.Goto(patchData.DataPointer);

                            patch.Load(s, romFile);
                        }

                        PatchViewModels.Add(new PatchViewModel(patch, enabled));
                    }

                    // Set the ROM info, useful for debugging
                    RefreshROMInfo();
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

            // Verify the footer integrity
            if (footer.RelocatedStructs.Any(x => x.OriginalPointer.AbsoluteOffset >= Config.ROMEndPointer))
                MessageBox.Show("Some relocated data has the original pointer set to after the defined ROM end pointer", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            if (footer.RelocatedStructs.GroupBy(x => x.OriginalPointer).Any(x => x.Count() > 1))
                MessageBox.Show("Multiple relocated data structs share the same original pointer", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            return footer;
        }

        public void RefreshROMInfo()
        {
            var str = new StringBuilder();

            str.AppendLine($"=== ROM ===");
            str.AppendLine($"Title: {ROM.Header.GameTitle}");
            str.AppendLine($"Code: {ROM.Header.GameCode}");
            str.AppendLine($"Maker Code: {ROM.Header.MakerCode}");
            str.AppendLine($"Version: {ROM.Header.SoftwareVersion}");

            if (Footer.Offset != null)
            {
                str.AppendLine();
                str.AppendLine();

                str.AppendLine($"=== PATCH ===");
                str.AppendLine($"Offset: 0x{Footer.Offset.StringAbsoluteOffset}");
                str.AppendLine($"Version: {Footer.EditorVersion}");
                str.AppendLine($"Relocated Structs: {Footer.RelocatedStructsCount}");

                foreach (PatchedFooter.RelocatedStruct r in Footer.RelocatedStructs)
                {
                    str.AppendLine($"\t" +
                                   $"Original: 0x{r.OriginalPointer.StringAbsoluteOffset}, " +
                                   $"New: 0x{r.NewPointer.StringAbsoluteOffset}, " +
                                   $"Archive: 0x{r.ParentArchivePointer.StringAbsoluteOffset}, " +
                                   $"Size: {r.DataSize} bytes");
                }

                str.AppendLine($"Patches: {Footer.PatchDatasCount}");

                foreach (PatchedFooter.PatchData p in Footer.PatchDatas)
                {
                    str.AppendLine($"\t" +
                                   $"ID: {p.ID}, " +
                                   $"Data: 0x{p.DataPointer}, " +
                                   $"Size: {p.DataSize} bytes");
                }
            }

            ROMInfo = str.ToString();
        }

        public async Task SaveAsync(string romPath)
        {
            Logger.Info("Saving ROM");

            using (Context)
            {
                try
                {
                    // Get the file from the context
                    PhysicalFile file = (PhysicalFile)ROM.Offset.File;

                    // Set the destination path
                    file.DestinationPath = Context.NormalizePath(romPath, false);
                    file.RecreateOnWrite = false;

                    Logger.Trace("ROM source: {0}", file.SourcePath);
                    Logger.Trace("ROM destination: {0}", file.DestinationPath);

                    // If we're saving to another file we copy the original one to retain all the same unmodified ROM data
                    if (file.SourcePath != file.DestinationPath)
                        File.Copy(file.SourcePath, file.DestinationPath, true);

                    // Trim the file
                    using (var f = File.OpenWrite(file.DestinationPath))
                        f.SetLength(Config.ROMEndPointer - file.StartPointer.AbsoluteOffset);

                    // Go to the end of the ROM where we'll start appending data
                    var relocatePointer = new Pointer(Config.ROMEndPointer, file);
                    var s = Context.Serializer;
                    s.Goto(relocatePointer);
                    s.Align();

                    Footer.PatchDatasCount = PatchViewModels.Count(x => x.IsEnabled);
                    Footer.PatchDatas = new PatchedFooter.PatchData[Footer.PatchDatasCount];

                    var patchIndex = 0;

                    // Apply or revert patches
                    foreach (PatchViewModel patchVM in PatchViewModels)
                    {
                        if (patchVM.IsEnabled)
                        {
                            var dataPointer = s.CurrentPointer;
                            patchVM.Patch.Apply(s, file);

                            Footer.PatchDatas[patchIndex] = new PatchedFooter.PatchData
                            {
                                ID = patchVM.Patch.ID,
                                DataPointer = dataPointer,
                                DataSize = (uint)(s.CurrentFileOffset - dataPointer.FileOffset),
                            };
                            patchIndex++;
                        }
                        else if (patchVM.WasEnabled)
                        {
                            patchVM.Patch.Revert(s, file);
                        }

                        s.Align();
                    }

                    Logger.Info("Adding {0} relocated structs to 0x{1}", PendingRelocatedData.Count, s.CurrentPointer.StringAbsoluteOffset);

                    // Relocate the structs
                    Footer.RelocatedStructsCount = PendingRelocatedData.Count;
                    Footer.RelocatedStructs = new PatchedFooter.RelocatedStruct[Footer.RelocatedStructsCount];

                    for (var i = 0; i < PendingRelocatedData.Count; i++)
                    {
                        Footer.RelocatedStructs[i] = PendingRelocatedData[i].Relocate(s);
                        s.Align();
                    }

                    s.SerializeObject<PatchedFooter>(Footer, name: nameof(Footer));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save file. Error: {ex.Message}", "Error saving file", MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }
            }

            Logger.Info("Saved ROM");

            MessageBox.Show("The file was successfully saved", "Saved successfully");

            // Reload the file
            await LoadAsync(romPath);
        }

        public void Unload()
        {
            Logger.Info("Unloading ROM...");

            Context?.Dispose();
            Context = null;
            ROM = null;
            Footer = null;
            ROMInfo = null;
            FilesPageViewModel = null;
            PatchViewModels.Clear();
            PendingRelocatedData.Clear();
            SetTitle();

            Logger.Info("Unloaded ROM");
        }

        public void GenerateConfig()
        {
            File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(AppConfig.Default, Formatting.Indented, new StringEnumConverter()));

            Logger.Trace("Generated config");

            MessageBox.Show($"Config generated as {ConfigFileName}");
        }

        public void OpenURL(string url)
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}")
            {
                CreateNoWindow = true
            });

            Logger.Trace("Opened URL {0}", url);
        }

        public void AddRelocatedData(RelocatedData data)
        {
            Pointer romOffset = data.Offset;
            RelocatedData existingData = PendingRelocatedData.FirstOrDefault(x => x.Offset == romOffset);

            if (existingData != null)
            {
                // Use the origin pointer from the existing data
                data = data with
                {
                    OriginPointer = existingData.OriginPointer
                };

                PendingRelocatedData.Remove(existingData);
            }

            if (existingData != null)
                Logger.Info("Replaced relocated data from 0x{0} with origin 0x{1}", data.Offset.StringAbsoluteOffset, data.OriginPointer?.StringAbsoluteOffset);
            else
                Logger.Info("Added relocated data from 0x{0} with origin 0x{1}", data.Offset.StringAbsoluteOffset, data.OriginPointer?.StringAbsoluteOffset);

            PendingRelocatedData.Add(data);
        }

        #endregion
    }
}