using System;
using System.Collections.Generic;
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
        public NavigationItemViewModel(NavigationItemViewModel parent, string title, PackIconMaterialKind icon, Color iconColor, ObservableCollection<DuoGridItemViewModel> fileInfo, BinarySerializable serializableObject, FileEditorViewModel editorViewModel, PatchedFooter.RelocatedStruct relocatedStruct, ArchiveFile parentArchiveFile, ArchiveFile compressedParentArchiveFile)
        {
            Parent = parent;
            Title = title;
            Icon = icon;
            IconColor = new SolidColorBrush(iconColor);
            FileInfo = fileInfo;
            SerializableObject = serializableObject;
            EditorViewModel = editorViewModel;
            RelocatedStruct = relocatedStruct;
            ParentArchiveFile = parentArchiveFile;
            CompressedParentArchiveFile = compressedParentArchiveFile;
            NavigationItems = new ObservableCollection<NavigationItemViewModel>();

            ExportBinaryCommand = new RelayCommand(ExportBinary);
            ImportBinaryCommand = new RelayCommand(ImportBinary);
            RefreshCommand = new RelayCommand(Refresh);

            EditorViewModel?.Init(this);

            if (SerializableObject != null && !IsWithinCompressedArchive)
                Offset = BinaryHelpers.GetROMPointer(SerializableObject.Offset);
        }

        private bool _isSelected;
        private bool _hasLoaded;

        public ICommand ExportBinaryCommand { get; }
        public ICommand ImportBinaryCommand { get; }
        public ICommand RefreshCommand { get; }

        public NavigationItemViewModel Parent { get; }
        public string Title { get; }
        public PackIconMaterialKind Icon { get; }
        public SolidColorBrush IconColor { get; }
        public ObservableCollection<DuoGridItemViewModel> FileInfo { get; }
        public BinarySerializable SerializableObject { get; }
        public ArchiveFile ParentArchiveFile { get; }
        public ArchiveFile CompressedParentArchiveFile { get; } // If not null then ParentArchiveFile is the parent to this file instead
        public bool IsWithinCompressedArchive => CompressedParentArchiveFile != null;
        public bool IsNull => SerializableObject == null;
        public Pointer Offset { get; }
        public FileEditorViewModel EditorViewModel { get; }
        public PatchedFooter.RelocatedStruct RelocatedStruct { get; }
        public bool Relocated => RelocatedStruct != null;
        public string OverrideFileName { get; init; }

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
        public bool CanExportBinary => 
            // Only base files can be exported as we need the size of the file
            SerializableObject is BaseFile f && 
            // The file can only be exported if a size is specified
            f.Pre_FileSize != -1 && 
            // Files within compressed archives can currently not be exported as the offset is no longer valid (due to being in a compressed StreamFile)
            !IsWithinCompressedArchive;

        public bool CanImportBinary => SerializableObject is not ArchiveFile && !IsNull;

        public string DisplayOffset
        {
            get
            {
                if (IsNull)
                    return "NULL";
                else if (IsWithinCompressedArchive)
                    return $"0x{BinaryHelpers.GetROMPointer(CompressedParentArchiveFile.Offset).StringAbsoluteOffset}_{SerializableObject.Offset.StringFileOffset}";
                else if (Relocated)
                    return $"0x{RelocatedStruct.OriginalPointer.StringAbsoluteOffset}";
                else
                    return $"0x{Offset.StringAbsoluteOffset}";
            }
        }

        public string DisplayName
        {
            get
            {
                var displayOffset = DisplayOffset;

                if (RelocatedStruct != null)
                    displayOffset += $"->0x{RelocatedStruct.NewPointer.StringAbsoluteOffset}";

                return $"{displayOffset} ({OverrideFileName ?? Title})";
            }
        }

        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

        public IEnumerable<NavigationItemViewModel> GetAllChildren(bool includeSelf = false)
        {
            if (includeSelf)
                yield return this;

            foreach (NavigationItemViewModel child in NavigationItems)
            {
                if (child == null)
                    continue;

                foreach (NavigationItemViewModel subChild in child.GetAllChildren(true))
                    yield return subChild;
            }
        }

        public void RelocateFile()
        {
            if (IsNull)
            {
                MessageBox.Show("Null files can't be relocated", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // If we're within a compressed archive we want to relocate that instead
            BinarySerializable obj = IsWithinCompressedArchive ? CompressedParentArchiveFile : SerializableObject;

            AppViewModel.Current.AddRelocatedData(new RelocatedData(obj, ParentArchiveFile.OffsetTable)
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
                FileName = $"{DisplayOffset}.bin"
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
            }
            finally
            {
                SerializableObject.Init(originalOffset);
                context.RemoveFile(streamFile);
            }

            // Re-initialize
            EditorViewModel?.Init(this);

            // Re-load
            EditorViewModel?.Load(true);

            // Relocate the file
            RelocateFile();
        }

        public void Refresh()
        {
            EditorViewModel?.Load(true);
        }
    }
}