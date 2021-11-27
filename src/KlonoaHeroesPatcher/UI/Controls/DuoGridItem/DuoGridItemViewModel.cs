namespace KlonoaHeroesPatcher;

/// <summary>
/// A duo grid item view model
/// </summary>
public class DuoGridItemViewModel : BaseViewModel
{
    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="header">The header</param>
    /// <param name="text">The text to display</param>
    public DuoGridItemViewModel(string header, string text)
    {
        Header = header;
        Text = text;
    }

    /// <summary>
    /// The header
    /// </summary>
    public string Header { get; set; }

    /// <summary>
    /// The text to display
    /// </summary>
    public string Text { get; set; }
}