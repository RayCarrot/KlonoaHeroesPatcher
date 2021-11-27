using System;
using System.Collections;
using AutoCompleteTextBox.Editors;

namespace KlonoaHeroesPatcher;

/// <summary>
/// A base suggestion provider
/// </summary>
public class BaseSuggestionProvider : ISuggestionProvider
{
    public BaseSuggestionProvider(Func<string, IEnumerable> getSuggestionsFunc)
    {
        GetSuggestionsFunc = getSuggestionsFunc;
    }

    protected Func<string, IEnumerable> GetSuggestionsFunc { get; }

    public IEnumerable GetSuggestions(string filter) => GetSuggestionsFunc(filter);
}