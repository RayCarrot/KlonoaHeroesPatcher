using BinarySerializer;
using BinarySerializer.Klonoa;

namespace KlonoaHeroesPatcher
{
    public abstract class FileEditorViewModel : BaseViewModel
    {
        public object EditorUI { get; private set; }
        public ArchiveFile ParentArchiveFile { get; private set; }
        public BinarySerializable SerializableObject { get; private set; }

        protected abstract void Load();
        protected abstract object GetEditor();

        protected void RelocateFile() => AppViewModel.Current.AddRelocatedData(new RelocatedData(SerializableObject, ParentArchiveFile, true));

        public void Init(BinarySerializable obj, ArchiveFile parentArchiveFile)
        {
            if (SerializableObject != null)
                return;

            SerializableObject = obj;
            ParentArchiveFile = parentArchiveFile;
            EditorUI = GetEditor();
            Load();
        }
    }
}