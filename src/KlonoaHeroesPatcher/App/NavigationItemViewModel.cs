using System.Collections.ObjectModel;
using System.Windows.Media;
using BinarySerializer;
using BinarySerializer.Klonoa;
using MahApps.Metro.IconPacks;

namespace KlonoaHeroesPatcher
{
    public class NavigationItemViewModel : BaseViewModel
    {
        public NavigationItemViewModel(string title, PackIconMaterialKind icon, Color iconColor, ObservableCollection<DuoGridItemViewModel> fileInfo, BinarySerializable serializableObject, ArchiveFile parentArchiveFile, FileEditorViewModel editorViewModel, bool relocated)
        {
            Title = title;
            Icon = icon;
            IconColor = new SolidColorBrush(iconColor);
            FileInfo = fileInfo;
            SerializableObject = serializableObject;
            ParentArchiveFile = parentArchiveFile;
            EditorViewModel = editorViewModel;
            Relocated = relocated;
            NavigationItems = new ObservableCollection<NavigationItemViewModel>();

            if (SerializableObject == null)
                return;

            Offset = BinaryHelpers.GetROMPointer(SerializableObject.Offset);
        }

        private bool _isSelected;

        public string Title { get; }
        public PackIconMaterialKind Icon { get; }
        public SolidColorBrush IconColor { get; }
        public ObservableCollection<DuoGridItemViewModel> FileInfo { get; }
        public BinarySerializable SerializableObject { get; }
        public ArchiveFile ParentArchiveFile { get; }
        public Pointer Offset { get; }
        public FileEditorViewModel EditorViewModel { get; }
        public bool Relocated { get; }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;

                if (IsSelected)
                    EditorViewModel?.Init(this);
            }
        }
        public bool CanBeEdited => EditorViewModel != null;
        public bool UnsavedChanges { get; set; }

        public string DisplayName => $"{Offset?.StringAbsoluteOffset ?? "NULL"} ({Title})";
        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }
    }
}