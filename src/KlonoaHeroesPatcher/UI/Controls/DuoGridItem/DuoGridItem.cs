using System.Windows;
using System.Windows.Controls;

namespace KlonoaHeroesPatcher;

/// <summary>
/// A duo grid item
/// </summary>
public class DuoGridItem : Control
{
    /// <summary>
    /// The header
    /// </summary>
    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(DuoGridItem));

    /// <summary>
    /// The text to display
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(DuoGridItem));
}