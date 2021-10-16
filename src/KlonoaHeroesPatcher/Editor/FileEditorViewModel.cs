using BinarySerializer;
using BinarySerializer.Klonoa;

namespace KlonoaHeroesPatcher
{
    public abstract class FileEditorViewModel : BaseViewModel
    {
        public ArchiveFile ParentArchiveFile { get; private set; }
        public BinarySerializable SerializableObject { get; private set; }
        public NavigationItemViewModel NavigationItem { get; private set; }

        protected abstract void Load(bool firstLoad);

        protected void RelocateFile(BinarySerializable obj = null) => NavigationItem.RelocateFile(obj);

        public void Init(NavigationItemViewModel navigationItem)
        {
            SerializableObject = navigationItem.SerializableObject;
            ParentArchiveFile = navigationItem.ParentArchiveFile;
            NavigationItem = navigationItem;
            Load(true);
        }
    }
}