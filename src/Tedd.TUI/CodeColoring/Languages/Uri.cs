using System.Collections.Generic;
using Tedd.TUI.CodeColoring;
using static Tedd.TUI.CodeColoring.RegexUtils;

namespace Tedd.TUI.CodeColoring.Languages;

public class UriLanguage : ILanguage
{
    public string Id => "uri";
    public string[] Aliases => [ "url"  ];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        var schemeInside = new Grammar();
        schemeInside.Add("scheme-delimiter", new Pattern(@":$"));
        grammar.Add("scheme", new Pattern(@"^[a-z][a-z0-9+.-]*:", regexOptions: "im", greedy: true, inside: schemeInside));

        var fragmentInside = new Grammar();
        fragmentInside.Add("fragment-delimiter", new Pattern(@"^#"));
        grammar.Add("fragment", new Pattern(@"#[\w\-.~!$&'()*+,;=%:@/?]*", inside: fragmentInside));

        var queryInside = new Grammar();
        queryInside.Add("query-delimiter", new Pattern(@"^\?", greedy: true));
        queryInside.Add("pair-delimiter", new Pattern(@"[&;]"));

        var pairInside = new Grammar();
        pairInside.Add("key", new Pattern(@"^[^=]+"));
        pairInside.Add("value", new Pattern(@"(^=)[\s\S]+", lookbehind: true));

        queryInside.Add("pair", new Pattern(@"^[^=][\s\S]*", inside: pairInside));

        grammar.Add("query", new Pattern(@"\?[\w\-.~!$&'()*+,;=%:@/?]*", inside: queryInside));

        // Authority
        string ipv4 = @"(?:(?:[03-9]\d?|[12]\d{0,2})\.){3}(?:[03-9]\d?|[12]\d{0,2})";
        var authorityInside = new Grammar();
        authorityInside.Add("authority-delimiter", new Pattern(@"^\/\/"));

        var userInfoInside = new Grammar();
        userInfoInside.Add("user-info-delimiter", new Pattern(@"@$"));
        userInfoInside.Add("user-info", new Pattern(@"^[\w\-.~!$&'()*+,;=%:@]+"));
        authorityInside.Add("user-info-segment", new Pattern(@"^[\w\-.~!$&'()*+,;=%:@]*@", inside: userInfoInside));

        var portInside = new Grammar();
        portInside.Add("port-delimiter", new Pattern(@"^:"));
        portInside.Add("port", new Pattern(@"^\d+"));
        authorityInside.Add("port-segment", new Pattern(@":\d*$", inside: portInside));

        var hostInside = new Grammar();
        var ipLiteralInside = new Grammar();
        ipLiteralInside.Add("ip-literal-delimiter", new Pattern(@"^\[|\]$"));
        ipLiteralInside.Add("ipv-future", new Pattern(@"^v[\s\S]+"));
        ipLiteralInside.Add("ipv6-address", new Pattern(@"^[\s\S]+"));
        hostInside.Add("ip-literal", new Pattern(@"^\[[\s\S]+\]$", inside: ipLiteralInside));
        hostInside.Add("ipv4-address", new Pattern("^" + ipv4 + "$"));

        authorityInside.Add("host", new Pattern(@"[\s\S]+", inside: hostInside));

        grammar.Add("authority", new Pattern(Replace(@"^\/\/(?:[\w\-.~!$&'()*+,;=%:]*@)?(?:\[(?:[0-9a-fA-F:.]{2,48}|v[0-9a-fA-F]+\.[\w\-.~!$&'()*+,;=]+)\]|[\w\-.~!$&'()*+,;=%]*)(?::\d*)?", ""), regexOptions: "m", inside: authorityInside));

        grammar.Add("path", new Pattern(@"^[\w\-.~!$&'()*+,;=%:@/]+", regexOptions: "m", inside: new Grammar { { "path-separator", new List<Pattern> { new Pattern(@"\/") } } }));

        return grammar;
    }
}
