using BinarySerializer;
using BinarySerializer.Klonoa;
using BinarySerializer.Klonoa.KH;

namespace KlonoaHeroesPatcher
{
    /// <summary>
    /// A variant of <see cref="Cutscene_File"/> where only the text is parsed. This is required as the <see cref="Cutscene_File"/>
    /// implementation currently does not parse all the data due to a lot of it being referenced from command offsets. The text is always
    /// at the end of the file, so if we simply parse the initial data as raw bytes and then modify the end we can safely modify the text
    /// without it breaking the script commands. This is hopefully just a temporary solution until <see cref="Cutscene_File"/> is fully implemented
    /// </summary>
    public class CutsceneTextOnly_File : BaseFile
    {
        public long Pre_ScriptsLength { get; set; }

        public byte[] Scripts { get; set; }
        public CutsceneTextCommand[] TextCommands { get; set; }

        public override void SerializeImpl(SerializerObject s)
        {
            Scripts = s.SerializeArray<byte>(Scripts, Pre_ScriptsLength, name: nameof(Scripts));
            TextCommands = s.SerializeObjectArrayUntil(TextCommands, x => x.Command == CutsceneTextCommand.CommandType.End, name: nameof(TextCommands));
        }
    }
}