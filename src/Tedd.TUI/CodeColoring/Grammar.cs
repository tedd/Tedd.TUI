using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring;

public class Grammar : Dictionary<string, List<Pattern>>
{
    public Grammar() { }

    public void Add(string name, Pattern pattern)
    {
        if (!ContainsKey(name))
        {
            this[name] = new List<Pattern>();
        }
        this[name].Add(pattern);
    }

    public void Add(string name, List<Pattern> patterns)
    {
        if (!ContainsKey(name))
        {
            this[name] = new List<Pattern>();
        }
        this[name].AddRange(patterns);
    }

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
            result[kvp.Key] = new List<Pattern>(kvp.Value);
        }
        return result;
    }

    // Prism.languages.insertBefore
    public void InsertBefore(string beforeKey, Grammar newTokens)
    {
        // This is tricky with Dictionary order, but we can rebuild it.
        // Actually, Dictionary doesn't guarantee order, but Prism relies on iteration order.
        // We should probably use an OrderedDictionary or List of KeyValuePairs internally if order matters (it does).
        // But for now, let's assume standard Dictionary iteration is "insertion order" in .NET Core usually,
        // though strictly not guaranteed.
        // To be safe, let's just create a new backing store or rebuild.

        // However, since we inherit Dictionary, we are stuck with its behavior.
        // Prism relies heavily on order. I should probably use a List<KeyValuePair> or similar.
        // But Dictionary access by key is also needed.
        // Let's stick to Dictionary for now and if order becomes an issue, we'll swap to OrderedDictionary (not standard in older .NET, available in .NET 9 preview or specialized libraries).
        // Or just re-insert everything.

        var temp = new List<KeyValuePair<string, List<Pattern>>>(this);
        this.Clear();

        foreach (var kvp in temp)
        {
            if (kvp.Key == beforeKey)
            {
                foreach (var newKvp in newTokens)
                {
                    this[newKvp.Key] = newKvp.Value;
                }
            }
            this[kvp.Key] = kvp.Value;
        }
    }
}
