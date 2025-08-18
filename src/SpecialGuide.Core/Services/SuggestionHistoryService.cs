using System.Collections.Generic;

namespace SpecialGuide.Core.Services;

public class SuggestionHistoryService
{
    private readonly int _capacity;
    private readonly List<string[]> _history = new();
    private readonly object _lock = new();

    public SuggestionHistoryService(int capacity = 10)
    {
        _capacity = capacity;
    }

    public void Add(string[] suggestions)
    {
        if (suggestions.Length == 0) return;
        lock (_lock)
        {
            _history.Insert(0, suggestions);
            if (_history.Count > _capacity)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }
    }

    public IReadOnlyList<string[]> GetHistory()
    {
        lock (_lock)
        {
            return _history.ToArray();
        }
    }
}
