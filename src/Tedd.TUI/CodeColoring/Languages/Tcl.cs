using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class TclLanguage : ILanguage
{
    public string Id => "tcl";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"(^|[^\\])#.*", lookbehind: true));
        grammar.Add("string", new Pattern(@"""(?:[^""\\\r\n]|\\(?:\r\n|[\s\S]))*""", greedy: true));
        grammar.Add("variable", new List<Pattern>
        {
            new Pattern(@"(\$)(?:::)?(?:[a-zA-Z0-9]+::)*\w+", lookbehind: true),
            new Pattern(@"(\$)\{[^}]+\}", lookbehind: true),
            new Pattern(@"(^[\t ]*set[ \t]+)(?:::)?(?:[a-zA-Z0-9]+::)*\w+", regexOptions: "m", lookbehind: true)
        });
        grammar.Add("function", new Pattern(@"(^[\t ]*proc[ \t]+)\S+", regexOptions: "m", lookbehind: true));
        grammar.Add("builtin", new List<Pattern>
        {
            new Pattern(@"(^[\t ]*)(?:break|class|continue|error|eval|exit|for|foreach|if|proc|return|switch|while)\b", regexOptions: "m", lookbehind: true),
            new Pattern(@"\b(?:else|elseif)\b")
        });
        grammar.Add("scope", new Pattern(@"(^[\t ]*)(?:global|upvar|variable)\b", regexOptions: "m", lookbehind: true, alias: "constant"));
        grammar.Add("keyword", new Pattern(@"(^[\t ]*|\[)(?:Safe_Base|Tcl|after|append|apply|array|auto_(?:execok|import|load|mkindex|qualify|reset)|automkindex_old|bgerror|binary|catch|cd|chan|clock|close|concat|dde|dict|encoding|eof|exec|expr|fblocked|fconfigure|fcopy|file(?:event|name)?|flush|gets|glob|history|http|incr|info|interp|join|lappend|lassign|lindex|linsert|list|llength|load|lrange|lrepeat|lreplace|lreverse|lsearch|lset|lsort|math(?:func|op)|memory|msgcat|namespace|open|package|parray|pid|pkg_mkIndex|platform|puts|pwd|re_syntax|read|refchan|regexp|registry|regsub|rename|scan|seek|set|socket|source|split|string|subst|tcl(?:_endOfWord|_findLibrary|startOf(?:Next|Previous)Word|test|vars|wordBreak(?:After|Before))|tell|time|tm|trace|unknown|unload|unset|update|uplevel|vwait)\b", regexOptions: "m", lookbehind: true));
        grammar.Add("operator", new Pattern(@"!=?|\*\*?|==|&&?|\|\|?|<[=<]?|>[=>]?|[-+~\/%?^]|\b(?:eq|in|ne|ni)\b"));
        grammar.Add("punctuation", new Pattern(@"[{}()\[\]]"));
        return grammar;
    }
}
