using System.Collections.Generic;
using BinarySerializer.Klonoa.KH;

namespace KlonoaHeroesPatcher
{
    public class TextCollectionFileEditorViewModel : BaseTextFileEditorViewModel
    {
        public TextCollection_File TextFile => (TextCollection_File)SerializableObject;

        protected override IEnumerable<TextCommands> GetTextCommands() => TextFile.Text;
    }
}