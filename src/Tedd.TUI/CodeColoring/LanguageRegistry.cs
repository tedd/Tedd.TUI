using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tedd.TUI.CodeColoring;

public static class LanguageRegistry
{
    private static Dictionary<string, Grammar> _grammars = [];
    private static Dictionary<string, Type> _languageTypes = [];
    private static bool _initialized = false;

    private static void Initialize()
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

    private static void RegisterLanguage(string id, Type type)
    {
        _languageTypes[id.ToLower()] = type;
    }

    public static Grammar? GetGrammar(string language)
    {
        Initialize();

        string key = language.ToLower();
        if (_grammars.ContainsKey(key))
        {
            return _grammars[key];
        }

        // Lazy loading
        if (_languageTypes.TryGetValue(key, out var type))
        {
            if (Activator.CreateInstance(type) is ILanguage instance)
            {
                var grammar = instance.GetGrammar();
                _grammars[key] = grammar;
                return grammar;
            }
        }

        return null;
    }
}
