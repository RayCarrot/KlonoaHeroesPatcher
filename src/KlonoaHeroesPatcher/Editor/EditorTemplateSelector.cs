using System.Windows;
using System.Windows.Controls;

namespace KlonoaHeroesPatcher;

public class EditorTemplateSelector : DataTemplateSelector
{
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            BinaryFileEditorViewModel _ => (DataTemplate)Application.Current.FindResource("EditorTemplate.Binary"),
            CutsceneFileEditorViewModel _ => (DataTemplate)Application.Current.FindResource("EditorTemplate.Cutscene"),
            GraphicsFileEditorViewModel _ => (DataTemplate)Application.Current.FindResource("EditorTemplate.Graphics"),
            TextFileEditorViewModel _ => (DataTemplate)Application.Current.FindResource("EditorTemplate.Text"),
            TextCollectionFileEditorViewModel _ => (DataTemplate)Application.Current.FindResource("EditorTemplate.Text"),
            AnimationFileEditorViewModel _ => (DataTemplate)Application.Current.FindResource("EditorTemplate.Animation"),
            ItemsCollectionFileEditorViewModel _ => (DataTemplate)Application.Current.FindResource("EditorTemplate.ItemsCollection"),
            _ => null
        };
    }
}