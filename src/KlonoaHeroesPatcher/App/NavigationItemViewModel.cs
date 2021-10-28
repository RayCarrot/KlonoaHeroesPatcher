using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BinarySerializer;
using BinarySerializer.Klonoa;
using MahApps.Metro.IconPacks;
using Microsoft.Win32;

namespace KlonoaHeroesPatcher
{
    public class NavigationItemViewModel : BaseViewModel
    {
        public NavigationItemViewModel(string title, PackIconMaterialKind icon, Color iconColor, ObservableCollection<DuoGridItemViewModel> fileInfo, BinarySerializable serializableObject, ArchiveFile parentArchiveFile, FileEditorViewModel editorViewModel, bool relocated, string overrideFileName = null)
        {
            Title = title;
            Icon = icon;
            IconColor = new SolidColorBrush(iconColor);
            FileInfo = fileInfo;
            SerializableObject = serializableObject;
            ParentArchiveFile = parentArchiveFile;
            EditorViewModel = editorViewModel;
            Relocated = relocated;
            OverrideFileName = overrideFileName;
            NavigationItems = new ObservableCollection<NavigationItemViewModel>();

            ExportBinaryCommand = new RelayCommand(ExportBinary);
            ImportBinaryCommand = new RelayCommand(ImportBinary);

            EditorViewModel?.Init(this);

            if (SerializableObject == null)
                return;

            Offset = BinaryHelpers.GetROMPointer(SerializableObject.Offset, throwOnError: false);
        }

        private bool _isSelected;
        private bool _hasLoaded;

        public ICommand ExportBinaryCommand { get; }
        public ICommand ImportBinaryCommand { get; }

        public string Title { get; }
        public PackIconMaterialKind Icon { get; }
        public SolidColorBrush IconColor { get; }
        public ObservableCollection<DuoGridItemViewModel> FileInfo { get; }
        public BinarySerializable SerializableObject { get; }
        public ArchiveFile ParentArchiveFile { get; }
        public bool IsNull => SerializableObject == null;
        public Pointer Offset { get; }
        public FileEditorViewModel EditorViewModel { get; }
        public bool Relocated { get; }
        public string OverrideFileName { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;

                if (IsSelected)
                {
                    EditorViewModel?.Load(!_hasLoaded);
                    _hasLoaded = true;
                }
                else if (!IsSelected)
                {
                    EditorViewModel?.Unload();
                }
            }
        }
        public bool IsExpanded { get; set; }
        public bool CanBeEdited => EditorViewModel != null;
        public bool UnsavedChanges { get; set; }
        public bool CanExportBinary => SerializableObject is BaseFile f && f.Pre_FileSize != -1 && Offset != null;
        public bool CanImportBinary => CanExportBinary && SerializableObject is not ArchiveFile;

        public string DisplayName => $"{Offset?.StringAbsoluteOffset ?? (IsNull ? "NULL" : "_")} ({OverrideFileName ?? Title})";
        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

        public void RelocateFile(BinarySerializable obj = null)
        {
            if (IsNull || Offset == null)
            {
                MessageBox.Show("The file can't be relocated. Most likely it's inside of a compressed archive which currently doesn't support relocating.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            obj ??= SerializableObject;

            AppViewModel.Current.AddRelocatedData(new RelocatedData(obj, ParentArchiveFile)
            {
                Encoder = (obj as BaseFile)?.Pre_FileEncoder,
            });
            UnsavedChanges = true;
        }

        public void ExportBinary()
        {
            var dialog = new SaveFileDialog()
            {
                Title = "Export binary",
                Filter = "All files (*.*)|*.*",
                FileName = $"{Offset.StringAbsoluteOffset}.bin"
            };

            var result = dialog.ShowDialog();

            if (result != true)
                return;

            Context context = AppViewModel.Current.Context;
            BaseFile file = (BaseFile)SerializableObject;

            using (context)
            {
                BinaryDeserializer s = context.Deserializer;

                s.Goto(Offset);
                byte[] bytes = s.DoEncodedIf(file.Pre_FileEncoder, file.Pre_IsCompressed, () => s.SerializeArray<byte>(default, file.Pre_FileSize));

                File.WriteAllBytes(dialog.FileName, bytes);

                MessageBox.Show($"The file was successfully exported");
            }
        }

        public void ImportBinary()
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Import binary",
                Filter = "All files (*.*)|*.*",
            };

            var result = dialog.ShowDialog();

            if (result != true)
                return;

            Context context = AppViewModel.Current.Context;

            // Open the file
            using var file = File.OpenRead(dialog.FileName);

            using var streamFile = new StreamFile(context, "BinaryImport", file, allowLocalPointers: true);

            context.AddFile(streamFile);

            var originalOffset = SerializableObject.Offset;

            try
            {
                BinaryDeserializer s = context.Deserializer;

                s.Goto(streamFile.StartPointer);

                SerializableObject.Init(s.CurrentPointer);
                SerializableObject.SerializeImpl(s);

                // Re-initialize
                EditorViewModel?.Init(this);

                // Re-load
                EditorViewModel?.Load(true);

                // Relocate the file
                RelocateFile();
            }
            finally
            {
                SerializableObject.Init(originalOffset);
                context.RemoveFile(streamFile);
            }
        }
    }
}