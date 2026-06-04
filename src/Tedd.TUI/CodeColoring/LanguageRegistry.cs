using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tedd.TUI.CodeColoring;

// Intent: Make LanguageRegistry thread-safe for initialize and lazy-load operations to prevent race conditions during parallel execution.
// Why:
// - Parallel test execution in xUnit caused concurrent writes/reads on the non-thread-safe _grammars and _languageTypes dictionaries, resulting in corrupted lookups.
// Constraints/Invariants:
// - LanguageRegistry initialization and grammar lookup must be fully synchronized.
// Failure modes:
// - Concurrent access can cause Dictionary internal state corruption, leading to KeyNotFoundException, NullReferenceException, or missing registrations.
// Verification:
// - Run full dotnet test suite in parallel multiple times.
public static class LanguageRegistry
{
    private static Dictionary<string, Grammar> _grammars = [];
    private static Dictionary<string, Type> _languageTypes = [];
    private static bool _initialized = false;
    private static readonly System.Threading.Lock _lock = new();

    private static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;
            _initialized = true;

            // Scan assemblies for ILanguage implementations
            // We scan EntryAssembly and ExecutingAssembly (this assembly)
            var assemblies = new List<Assembly>();
            if (Assembly.GetEntryAssembly() != null) assemblies.Add(Assembly.GetEntryAssembly()!);
            assemblies.Add(Assembly.GetExecutingAssembly());

            // Remove duplicates
            assemblies = assemblies.Distinct().ToList();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => typeof(ILanguage).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in types)
                    {
                        // Create instance to get metadata? Or use static properties?
                        // Interface requires instance methods.
                        // Ideally we should singleton them or create on demand.
                        // For discovery we need to instantiate or have attributes.
                        // Let's instantiate once to register.
                        try
                        {
                            if (Activator.CreateInstance(type) is ILanguage instance)
                            {
                                RegisterLanguage(instance.Id, type);
                                if (instance.Aliases != null)
                                {
                                    foreach (var alias in instance.Aliases)
                                    {
                                        RegisterLanguage(alias, type);
                                    }
                                }
                            }
                        }
                        catch { /* Ignore instantiation errors during scan */ }
                    }
                }
                catch { /* Ignore assembly load errors */ }
            }
        }
    }

    private static void RegisterLanguage(string id, Type type)
    {
        _languageTypes[id.ToLower()] = type;
    }

    public static Grammar? GetGrammar(string language)
    {
        Initialize();

        string key = language.ToLower();
        lock (_lock)
        {
            if (_grammars.TryGetValue(key, out var grammar))
            {
                return grammar;
            }

            // Lazy loading
            if (_languageTypes.TryGetValue(key, out var type))
            {
                if (Activator.CreateInstance(type) is ILanguage instance)
                {
                    grammar = instance.GetGrammar();
                    _grammars[key] = grammar;
                    return grammar;
                }
            }
        }

        return null;
    }
}
