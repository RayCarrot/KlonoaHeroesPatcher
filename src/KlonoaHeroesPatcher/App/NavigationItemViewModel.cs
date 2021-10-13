using System.Collections.ObjectModel;
using System.Windows.Media;
using BinarySerializer;
using BinarySerializer.Klonoa;
using MahApps.Metro.IconPacks;

namespace KlonoaHeroesPatcher
{
    public class NavigationItemViewModel : BaseViewModel
    {
        public NavigationItemViewModel(string title, PackIconMaterialKind icon, Color iconColor, BinarySerializable serializableObject, ArchiveFile parentArchiveFile, FileEditorViewModel editorViewModel = null)
        {
            Title = title;
            Icon = icon;
            IconColor = new SolidColorBrush(iconColor);
            SerializableObject = serializableObject;
            ParentArchiveFile = parentArchiveFile;
            EditorViewModel = editorViewModel;
            NavigationItems = new ObservableCollection<NavigationItemViewModel>();

            if (SerializableObject == null)
                return;

            if (SerializableObject.Offset.File is VirtualFile virtualFile)
                Offset = virtualFile.ParentPointer;
            else
                Offset = SerializableObject.Offset;
        }

        private bool _isSelected;

        public string Title { get; }
        public PackIconMaterialKind Icon { get; }
        public SolidColorBrush IconColor { get; }
        public BinarySerializable SerializableObject { get; }
        public ArchiveFile ParentArchiveFile { get; }
        public Pointer Offset { get; }
        public FileEditorViewModel EditorViewModel { get; }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;

                if (IsSelected)
                    EditorViewModel?.Init(SerializableObject, ParentArchiveFile);
            }
        }
        public bool CanBeEdited => EditorViewModel != null;

        public string DisplayName => $"{Offset?.StringAbsoluteOffset ?? "NULL"} ({Title})";
        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }
    }
}