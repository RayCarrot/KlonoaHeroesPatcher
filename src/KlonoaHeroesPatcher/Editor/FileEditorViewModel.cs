using BinarySerializer;
using BinarySerializer.Klonoa;

namespace KlonoaHeroesPatcher
{
    public abstract class FileEditorViewModel : BaseViewModel
    {
        public object EditorUI { get; private set; }
        public ArchiveFile ParentArchiveFile { get; private set; }
        public BinarySerializable SerializableObject { get; private set; }
        public NavigationItemViewModel NavigationItem { get; private set; }

        protected abstract void Load(bool firstLoad);
        protected abstract object GetEditor();

        protected void RelocateFile(BinarySerializable obj = null)
        {
            obj ??= SerializableObject;

            AppViewModel.Current.AddRelocatedData(new RelocatedData(obj, ParentArchiveFile)
            {
                Encoder = (obj as BaseFile)?.Pre_FileEncoder,
            });
            NavigationItem.UnsavedChanges = true;
        }

        public void Init(NavigationItemViewModel navigationItem)
        {
            if (SerializableObject != null)
                return;

            SerializableObject = navigationItem.SerializableObject;
            ParentArchiveFile = navigationItem.ParentArchiveFile;
            NavigationItem = navigationItem;
            EditorUI = GetEditor();
            Load(true);
        }
    }
}