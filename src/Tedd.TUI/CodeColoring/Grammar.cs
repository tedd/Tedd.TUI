using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Tedd.TUI.CodeColoring;

public class Grammar : IDictionary<string, List<Pattern>>
{
    // Use List for order and Dictionary for lookup
    private readonly List<string> _keys = new List<string>();
    private readonly Dictionary<string, List<Pattern>> _dictionary = new Dictionary<string, List<Pattern>>();

    public Grammar() { }

    public void Add(string name, List<Pattern> patterns)
    {
        if (!_dictionary.ContainsKey(name))
        {
            _keys.Add(name);
            _dictionary[name] = new List<Pattern>();
        }
        _dictionary[name].AddRange(patterns);
    }

    public void Add(string name, Pattern pattern)
    {
        Add(name, new List<Pattern> { pattern });
    }

    // IDictionary implementation
    public ICollection<string> Keys => _keys;
    public ICollection<List<Pattern>> Values
    {
        get
        {
            var values = new List<List<Pattern>>();
            foreach (var key in _keys)
            {
                values.Add(_dictionary[key]);
            }
            return values;
        }
    }

    public int Count => _dictionary.Count;
    public bool IsReadOnly => false;

    public List<Pattern> this[string key]
    {
        get => _dictionary[key];
        set
        {
            if (!_dictionary.ContainsKey(key))
            {
                _keys.Add(key);
            }
            _dictionary[key] = value;
        }
    }

    // Explicit implementation for IDictionary
    void IDictionary<string, List<Pattern>>.Add(string key, List<Pattern> value)
    {
        if (_dictionary.ContainsKey(key))
        {
            throw new System.ArgumentException($"An item with the same key has already been added. Key: {key}");
        }
        _keys.Add(key);
        _dictionary.Add(key, value);
    }

    public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

    public bool Remove(string key)
    {
        if (_dictionary.Remove(key))
        {
            _keys.Remove(key);
            return true;
        }
        return false;
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out List<Pattern> value) => _dictionary.TryGetValue(key, out value);

    public void Add(KeyValuePair<string, List<Pattern>> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        _dictionary.Clear();
        _keys.Clear();
    }

    public bool Contains(KeyValuePair<string, List<Pattern>> item)
    {
        return _dictionary.TryGetValue(item.Key, out var value) && value == item.Value; // Reference equality for list?
    }

    public void CopyTo(KeyValuePair<string, List<Pattern>>[] array, int arrayIndex)
    {
        int index = arrayIndex;
        foreach (var key in _keys)
        {
            array[index++] = new KeyValuePair<string, List<Pattern>>(key, _dictionary[key]);
        }
    }

    public bool Remove(KeyValuePair<string, List<Pattern>> item)
    {
        if (Contains(item))
        {
            return Remove(item.Key);
        }
        return false;
    }

    public IEnumerator<KeyValuePair<string, List<Pattern>>> GetEnumerator()
    {
        foreach (var key in _keys)
        {
            yield return new KeyValuePair<string, List<Pattern>>(key, _dictionary[key]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Prism.languages.extend
    public static Grammar Extend(Grammar baseGrammar, Grammar newTokens)
    {
        var result = new Grammar();
        foreach (var kvp in baseGrammar)
        {
            result[kvp.Key] = new List<Pattern>(kvp.Value);
        }
        foreach (var kvp in newTokens)
        {
            result[kvp.Key] = new List<Pattern>(kvp.Value); // Overwrite or append? Prism extends overwrites keys.
        }
        return result;
    }

    // Prism.languages.insertBefore
    public void InsertBefore(string beforeKey, Grammar newTokens)
    {
        int index = _keys.IndexOf(beforeKey);
        if (index == -1)
        {
            // If not found, append? Prism appends if not found usually? Or does nothing?
            // "if (token == before) { ... }"
            // If beforeKey not found, just append to end.
            foreach (var kvp in newTokens)
            {
                this[kvp.Key] = kvp.Value;
            }
            return;
        }

        // Insert new keys at index
        foreach (var kvp in newTokens)
        {
            if (_dictionary.ContainsKey(kvp.Key))
            {
                // Remove existing to re-insert at new position?
                // Or just update value?
                // If we update value, position doesn't change.
                // We want to insert.
                _keys.Remove(kvp.Key);
                _dictionary.Remove(kvp.Key);
            }
        }

        // Now insert
        // Since we are modifying _keys, we need to be careful with index.
        // We removed items, so index might shift?
        // We found index of beforeKey. beforeKey is still there.
        // If we removed items that were BEFORE beforeKey, index shifts.
        // But we re-find index.

        index = _keys.IndexOf(beforeKey);

        foreach (var kvp in newTokens)
        {
            _keys.Insert(index++, kvp.Key);
            _dictionary[kvp.Key] = kvp.Value;
        }
    }
}
