using System.Collections.Generic;
using BinarySerializer.Klonoa.KH;

namespace KlonoaHeroesPatcher
{
    public class TextFileEditorViewModel : BaseTextFileEditorViewModel
    {
        public TextCommands TextFile => (TextCommands)SerializableObject;

        protected override IEnumerable<TextCommands> GetTextCommands() => new TextCommands[]
        {
            TextFile
        };

        protected override void RelocateTextCommands()
        {
            // Relocate the data
            RelocateFile();
        }
    }
}