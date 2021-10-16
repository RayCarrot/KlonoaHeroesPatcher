using BinarySerializer.Klonoa.KH;

namespace KlonoaHeroesPatcher
{
    public class TextFileEditorViewModel : BaseTextFileEditorViewModel
    {
        public TextCommands TextFile => (TextCommands)SerializableObject;

        protected override TextCommand[] GetTextCommands() => TextFile.Commands;

        protected override void RelocateTextCommands(TextCommand[] cmds)
        {
            TextFile.Commands = cmds;

            // Relocate the data
            RelocateFile();
        }
    }
}