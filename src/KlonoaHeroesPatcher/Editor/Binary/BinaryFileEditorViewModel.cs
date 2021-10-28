using BinarySerializer;
using BinarySerializer.Klonoa;

namespace KlonoaHeroesPatcher
{
    public class BinaryFileEditorViewModel : FileEditorViewModel
    {
        public RawData_File RawFile => (RawData_File)SerializableObject;

        public string HexString { get; set; }

        public override void Load(bool firstLoad)
        {
            HexString = RawFile.Data.ToHexString(align: 16);
        }

        public override void Unload()
        {
            HexString = null;
        }
    }
}