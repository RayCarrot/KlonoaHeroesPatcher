using System.Collections.Generic;
using System.Linq;
using BinarySerializer.Klonoa.KH;

namespace KlonoaHeroesPatcher
{
    public class TextCollectionFileEditorViewModel : BaseTextFileEditorViewModel
    {
        public TextCollection_File TextFile => (TextCollection_File)SerializableObject;

        protected override IEnumerable<TextItemViewModel> GetTextCommandViewModels() => TextFile.Text.Select((x, i) => new TextItemViewModel(this, x, $"{i}"));
    }
}