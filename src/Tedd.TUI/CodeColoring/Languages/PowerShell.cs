using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class PowerShellLanguage : ILanguage
{
    public string Id => "powershell";
    public string[] Aliases => new string[0];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        grammar.Add("comment",
        [
            new Pattern(@"(^|[^`])<#[\s\S]*?#>", lookbehind: true),
            new Pattern(@"(^|[^`])#.*", lookbehind: true)
        ]);

        var stringInside = new Grammar(); // Defined later

        grammar.Add("string",
        [
            new Pattern(@"""(?:`[\s\S]|[^`""])*""", greedy: true, inside: stringInside),
            new Pattern(@"'(?:[^']|'')*'", greedy: true)
        ]);

        grammar.Add("namespace", new Pattern(@"\[[a-z](?:\[(?:\[[^\]]*\]|[^\[\]])*\]|[^\[\]])*\]", regexOptions: "i"));
        grammar.Add("boolean", new Pattern(@"\$(?:false|true)\b", regexOptions: "i"));
        grammar.Add("variable", new Pattern(@"\$\w+\b"));

        grammar.Add("function",
        [
            new Pattern(@"\b(?:Add|Approve|Assert|Backup|Block|Checkpoint|Clear|Close|Compare|Complete|Compress|Confirm|Connect|Convert|ConvertFrom|ConvertTo|Copy|Debug|Deny|Disable|Disconnect|Dismount|Edit|Enable|Enter|Exit|Expand|Export|Find|ForEach|Format|Get|Grant|Group|Hide|Import|Initialize|Install|Invoke|Join|Limit|Lock|Measure|Merge|Move|New|Open|Optimize|Out|Ping|Pop|Protect|Publish|Push|Read|Receive|Redo|Register|Remove|Rename|Repair|Request|Reset|Resize|Resolve|Restart|Restore|Resume|Revoke|Save|Search|Select|Send|Set|Show|Skip|Sort|Split|Start|Step|Stop|Submit|Suspend|Switch|Sync|Tee|Test|Trace|Unblock|Undo|Uninstall|Unlock|Unprotect|Unpublish|Unregister|Update|Use|Wait|Watch|Where|Write)-[a-z]+\b", regexOptions: "i"),
            new Pattern(@"\b(?:ac|cat|chdir|clc|cli|clp|clv|compare|copy|cp|cpi|cpp|cvpa|dbp|del|diff|dir|ebp|echo|epal|epcsv|epsn|erase|fc|fl|ft|fw|gal|gbp|gc|gci|gcs|gdr|gi|gl|gm|gp|gps|group|gsv|gu|gv|gwmi|iex|ii|ipal|ipcsv|ipsn|irm|iwmi|iwr|kill|lp|ls|measure|mi|mount|move|mp|mv|nal|ndr|ni|nv|ogv|popd|ps|pushd|pwd|rbp|rd|rdr|ren|ri|rm|rmdir|rni|rnp|rp|rv|rvpa|rwmi|sal|saps|sasv|sbp|sc|select|set|shcm|si|sl|sleep|sls|sort|sp|spps|spsv|start|sv|swmi|tee|trcm|type|write)\b", regexOptions: "i")
        ]);

        grammar.Add("keyword", new Pattern(@"\b(?:Begin|Break|Catch|Class|Continue|Data|Define|Do|DynamicParam|Else|ElseIf|End|Exit|Filter|Finally|For|ForEach|From|Function|If|InlineScript|Parallel|Param|Process|Return|Sequence|Switch|Throw|Trap|Try|Until|Using|Var|While|Workflow)\b", regexOptions: "i"));

        grammar.Add("operator", new Pattern(@"(^|\W)(?:!|-(?:b?(?:and|x?or)|as|(?:Not)?(?:Contains|In|Like|Match)|eq|ge|gt|is(?:Not)?|Join|le|lt|ne|not|Replace|sh[lr])\b|-[-=]?|\+[+=]?|[*\/%]=?)", regexOptions: "i", lookbehind: true));

        grammar.Add("punctuation", new Pattern(@"[|{}[\];(),.]"));

        // Fill stringInside
        stringInside.Add("function", new Pattern(@"(^|[^`])\$\((?:\$\([^\r\n()]*\)|(?!\$\()[^\r\n)])*\)", lookbehind: true, inside: grammar));
        stringInside.Add("boolean", grammar["boolean"]);
        stringInside.Add("variable", grammar["variable"]);

        return grammar;
    }
}
