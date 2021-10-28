using BinarySerializer;
using BinarySerializer.Klonoa;

namespace KlonoaHeroesPatcher
{
    public abstract class FileEditorViewModel : BaseViewModel
    {
        public ArchiveFile ParentArchiveFile { get; private set; }
        public BinarySerializable SerializableObject { get; private set; }
        public NavigationItemViewModel NavigationItem { get; private set; }

        public abstract void Load(bool firstLoad);
        public abstract void Unload();

        public void RelocateFile() => NavigationItem.RelocateFile();

        public void Init(NavigationItemViewModel navigationItem)
        {
            SerializableObject = navigationItem.SerializableObject;
            ParentArchiveFile = navigationItem.ParentArchiveFile;
            NavigationItem = navigationItem;
        }
    }
}