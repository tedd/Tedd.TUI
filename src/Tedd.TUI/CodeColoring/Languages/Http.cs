using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class HttpLanguage : ILanguage
{
    public string Id => "http";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        var requestLineInside = new Grammar();
        requestLineInside.Add("method", new Pattern(@"^[A-Z]+\b", alias: "property"));
        requestLineInside.Add("request-target", new Pattern(@"^(\s)(?:https?:\/\/|\/)\S*(?=\s)", lookbehind: true, alias: "url", inside: new UriLanguage().GetGrammar()));
        requestLineInside.Add("http-version", new Pattern(@"^(\s)HTTP\/[\d.]+", lookbehind: true, alias: "property"));

        grammar.Add("request-line", new Pattern(@"^(?:CONNECT|DELETE|GET|HEAD|OPTIONS|PATCH|POST|PRI|PUT|SEARCH|TRACE)\s(?:https?:\/\/|\/)\S*\sHTTP\/[\d.]+", regexOptions: "m", inside: requestLineInside));

        var responseStatusInside = new Grammar();
        responseStatusInside.Add("http-version", new Pattern(@"^HTTP\/[\d.]+", alias: "property"));
        responseStatusInside.Add("status-code", new Pattern(@"^(\s)\d+(?=\s)", lookbehind: true, alias: "number"));
        responseStatusInside.Add("reason-phrase", new Pattern(@"^(\s).+", lookbehind: true, alias: "string"));

        grammar.Add("response-status", new Pattern(@"^HTTP\/[\d.]+ \d+ .+", regexOptions: "m", inside: responseStatusInside));

        // Bodies highlighted by Content-Type. The empty line before the body may
        // be omitted as long as the first body line does not look like a header.
        string bodySuffix = @"(?:(?:\r\n?|\n)[\w-].*)*(?:\r(?:\n|(?!\n))|\n))[^ \t\w-][\s\S]*";
        grammar.Add("application-json", new Pattern(@"(content-type:\s*(?:application/json|\w+/(?:[\w.-]+\+)+json(?![+\w.-]))" + bodySuffix, regexOptions: "i", lookbehind: true, inside: new JsonLanguage().GetGrammar()));
        grammar.Add("application-javascript", new Pattern(@"(content-type:\s*application/javascript" + bodySuffix, regexOptions: "i", lookbehind: true, inside: new JavaScriptLanguage().GetGrammar()));
        grammar.Add("application-xml", new Pattern(@"(content-type:\s*(?:application/xml|\w+/(?:[\w.-]+\+)+xml(?![+\w.-]))" + bodySuffix, regexOptions: "i", lookbehind: true, inside: new MarkupLanguage().GetGrammar()));
        grammar.Add("text-xml", new Pattern(@"(content-type:\s*text/xml" + bodySuffix, regexOptions: "i", lookbehind: true, inside: new MarkupLanguage().GetGrammar()));
        grammar.Add("text-html", new Pattern(@"(content-type:\s*text/html" + bodySuffix, regexOptions: "i", lookbehind: true, inside: new MarkupLanguage().GetGrammar()));
        grammar.Add("text-css", new Pattern(@"(content-type:\s*text/css" + bodySuffix, regexOptions: "i", lookbehind: true, inside: new CssLanguage().GetGrammar()));

        var headerInside = new Grammar();
        headerInside.Add("header-value", new Pattern(@"(^(?:[^:]+):[ \t]*(?![ \t]))[\s\S]+", regexOptions: "i", lookbehind: true));
        headerInside.Add("header-name", new Pattern(@"^[^:]+", alias: "keyword"));
        headerInside.Add("punctuation", new Pattern(@"^:"));

        grammar.Add("header", new Pattern(@"^[\w-]+:.+(?:(?:\r\n?|\n)[ \t].+)*", regexOptions: "m", inside: headerInside));

        return grammar;
    }
}
