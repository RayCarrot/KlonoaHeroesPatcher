using System.Windows;
using System.Windows.Controls;

namespace KlonoaHeroesPatcher
{
    /// <summary>
    /// Interaction logic for GraphicsFileEditor.xaml
    /// </summary>
    public partial class GraphicsFileEditor : UserControl
    {
        public GraphicsFileEditor()
        {
            InitializeComponent();
        }

        private void BasePaletteSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            (DataContext as GraphicsFileEditorViewModel)?.RefreshPreviewImage();
        }
    }
}