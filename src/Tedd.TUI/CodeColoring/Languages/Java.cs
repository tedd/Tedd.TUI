using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class JavaLanguage : ILanguage
{
    public string Id => "java";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var clike = new CLikeLanguage().GetGrammar();
        var grammar = Grammar.Extend(clike, new Grammar());

        string keywords = @"\b(?:abstract|assert|boolean|break|byte|case|catch|char|class|const|continue|default|do|double|else|enum|exports|extends|final|finally|float|for|goto|if|implements|import|instanceof|int|interface|long|module|native|new|non-sealed|null|open|opens|package|permits|private|protected|provides|public|record(?!\s*[(){}[\]<>=%~.:,;?+\-*/&|^])|requires|return|sealed|short|static|strictfp|super|switch|synchronized|this|throw|throws|to|transient|transitive|try|uses|var|void|volatile|while|with|yield)\b";

        // full package (optional) + parent classes (optional)
        string classNamePrefix = @"(?:[a-z]\w*\s*\.\s*)*(?:[A-Z]\w*\s*\.\s*)*";

        var namespaceInside = new Grammar();
        namespaceInside.Add("punctuation", new Pattern(@"\."));

        // based on the java naming conventions
        var classNameInside = new Grammar();
        classNameInside.Add("namespace", new Pattern(@"^[a-z]\w*(?:\s*\.\s*[a-z]\w*)*(?:\s*\.)?", inside: namespaceInside));
        classNameInside.Add("punctuation", new Pattern(@"\."));

        var className = new Pattern(@"(^|[^\w.])" + classNamePrefix + @"[A-Z](?:[\d_A-Z]*[a-z]\w*)?\b", lookbehind: true, inside: classNameInside);

        grammar["string"] = new List<Pattern>
        {
            new Pattern(@"(^|[^\\])""(?:\\.|[^""\\\r\n])*""", lookbehind: true, greedy: true)
        };

        grammar["class-name"] = new List<Pattern>
        {
            className,
            // variables, parameters, and constructor references
            new Pattern(@"(^|[^\w.])" + classNamePrefix + @"[A-Z]\w*(?=\s+\w+\s*[;,=()]|\s*(?:\[[\s,]*\]\s*)?::\s*new\b)", lookbehind: true, inside: classNameInside),
            // class names based on keyword
            new Pattern(@"(\b(?:class|enum|extends|implements|instanceof|interface|new|record|throws)\s+)" + classNamePrefix + @"[A-Z]\w*\b", lookbehind: true, inside: classNameInside)
        };

        grammar["keyword"] = new List<Pattern> { new Pattern(keywords) };

        grammar["function"] = new List<Pattern>
        {
            new Pattern(@"\b\w+(?=\()"),
            new Pattern(@"(::\s*)[a-z_]\w*", lookbehind: true)
        };

        grammar["number"] = new List<Pattern>
        {
            new Pattern(@"\b0b[01][01_]*L?\b|\b0x(?:\.[\da-f_p+-]+|[\da-f_]+(?:\.[\da-f_p+-]+)?)\b|(?:\b\d[\d_]*(?:\.[\d_]*)?|\B\.\d[\d_]*)(?:e[+-]?\d[\d_]*)?[dfl]?", regexOptions: "i")
        };

        grammar["operator"] = new List<Pattern>
        {
            new Pattern(@"(^|[^.])(?:<<=?|>>>?=?|->|--|\+\+|&&|\|\||::|[?:~]|[-+*/%&|^!=<>]=?)", regexOptions: "m", lookbehind: true)
        };

        grammar["constant"] = new List<Pattern> { new Pattern(@"\b[A-Z][A-Z_\d]+\b") };

        grammar.InsertBefore("comment", new Grammar
        {
            { "doc-comment", new List<Pattern>
                {
                    new Pattern(@"\/\*\*(?!\/)[\s\S]*?(?:\*\/|$)", greedy: true, alias: "comment")
                }
            }
        });

        grammar.InsertBefore("string", new Grammar
        {
            { "triple-quoted-string", new List<Pattern>
                {
                    // http://openjdk.java.net/jeps/355#Description
                    new Pattern(@"""""""[ \t]*[\r\n](?:(?:""|"""")?(?:\\.|[^""\\]))*""""""", greedy: true, alias: "string")
                }
            },
            { "char", new List<Pattern>
                {
                    new Pattern(@"'(?:\\.|[^'\\\r\n]){1,6}'", greedy: true)
                }
            }
        });

        var genericsInside = new Grammar();
        genericsInside.Add("class-name", className);
        genericsInside.Add("keyword", new Pattern(keywords));
        genericsInside.Add("punctuation", new Pattern(@"[<>(),.:]"));
        genericsInside.Add("operator", new Pattern(@"[?&|]"));

        var importInside = new Grammar();
        importInside.Add("namespace", classNameInside["namespace"]);
        importInside.Add("punctuation", new Pattern(@"\."));
        importInside.Add("operator", new Pattern(@"\*"));
        importInside.Add("class-name", new Pattern(@"\w+"));

        var importStaticInside = new Grammar();
        importStaticInside.Add("namespace", classNameInside["namespace"]);
        importStaticInside.Add("static", new Pattern(@"\b\w+$"));
        importStaticInside.Add("punctuation", new Pattern(@"\."));
        importStaticInside.Add("operator", new Pattern(@"\*"));
        importStaticInside.Add("class-name", new Pattern(@"\w+"));

        grammar.InsertBefore("class-name", new Grammar
        {
            { "annotation", new List<Pattern>
                {
                    new Pattern(@"(^|[^.])@\w+(?:\s*\.\s*\w+)*", lookbehind: true, alias: "punctuation")
                }
            },
            { "generics", new List<Pattern>
                {
                    new Pattern(@"<(?:[\w\s,.?]|&(?!&)|<(?:[\w\s,.?]|&(?!&)|<(?:[\w\s,.?]|&(?!&)|<(?:[\w\s,.?]|&(?!&))*>)*>)*>)*>", inside: genericsInside)
                }
            },
            { "import", new List<Pattern>
                {
                    new Pattern(@"(\bimport\s+)" + classNamePrefix + @"(?:[A-Z]\w*|\*)(?=\s*;)", lookbehind: true, inside: importInside),
                    new Pattern(@"(\bimport\s+static\s+)" + classNamePrefix + @"(?:\w+|\*)(?=\s*;)", lookbehind: true, alias: "static", inside: importStaticInside)
                }
            },
            { "namespace", new List<Pattern>
                {
                    new Pattern(@"(\b(?:exports|import(?:\s+static)?|module|open|opens|package|provides|requires|to|transitive|uses|with)\s+)(?!" + keywords + @")[a-z]\w*(?:\.[a-z]\w*)*\.?", lookbehind: true, inside: namespaceInside)
                }
            }
        });

        return grammar;
    }
}
