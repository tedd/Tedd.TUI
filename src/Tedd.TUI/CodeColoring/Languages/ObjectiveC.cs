using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class ObjectiveCLanguage : ILanguage
{
    public string Id => "objectivec";
    public string[] Aliases => ["objc"];

    public Grammar GetGrammar()
    {
        var c = new CLanguage().GetGrammar();
        var grammar = Grammar.Extend(c, new Grammar());

        grammar["string"] = new List<Pattern>
        {
            new Pattern(@"@?""(?:\\(?:\r\n|[\s\S])|[^""\\\r\n])*""", greedy: true)
        };
        grammar["keyword"] = new List<Pattern>
        {
            new Pattern(@"\b(?:asm|auto|break|case|char|const|continue|default|do|double|else|enum|extern|float|for|goto|if|in|inline|int|long|register|return|self|short|signed|sizeof|static|struct|super|switch|typedef|typeof|union|unsigned|void|volatile|while)\b|(?:@interface|@end|@implementation|@protocol|@class|@public|@protected|@private|@property|@try|@catch|@finally|@throw|@synthesize|@dynamic|@selector)\b")
        };
        grammar["operator"] = new List<Pattern>
        {
            new Pattern(@"-[->]?|\+\+?|!=?|<<?=?|>>?=?|==?|&&?|\|\|?|[~^%?*\/@]")
        };

        grammar.Remove("class-name");

        return grammar;
    }
}
