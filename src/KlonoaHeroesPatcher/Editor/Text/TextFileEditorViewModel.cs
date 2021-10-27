using System.Collections.Generic;
using BinarySerializer.Klonoa.KH;

namespace KlonoaHeroesPatcher
{
    public class TextFileEditorViewModel : BaseTextFileEditorViewModel
    {
        public TextCommands TextFile => (TextCommands)SerializableObject;

        protected override IEnumerable<TextItemViewModel> GetTextCommandViewModels() => new TextItemViewModel[]
        {
            new TextItemViewModel(this, TextFile, null)
        };
    }
}