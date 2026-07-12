using System.Collections.Generic;
using Tedd.TUI.CodeColoring;
using Tedd.TUI.CodeColoring.Languages;
using static Tedd.TUI.CodeColoring.RegexUtils;

namespace Tedd.TUI.CodeColoring.Languages;

public class CSharpLanguage : ILanguage
{
    public string Id => "csharp";
    public string[] Aliases => ["cs"];

    public Grammar GetGrammar()
    {
        // We need Clike
        var clike = new CLikeLanguage().GetGrammar();

        // Helper regex patterns from Prism
        string keywordsToPattern(string words) => @"\b(?:" + words.Trim().Replace(" ", "|") + @")\b";

        string typeKeywords = "bool byte char decimal double dynamic float int long object sbyte short string uint ulong ushort var void";
        string typeDeclarationKeywords = "class enum interface record struct";
        string contextualKeywords = "add alias and ascending async await by descending from(?=\\s*(?:\\w|$)) get global group into init(?=\\s*;) join let nameof not notnull on or orderby partial remove select set unmanaged value when where with(?=\\s*{)";
        string otherKeywords = "abstract as base break case catch checked const continue default delegate do else event explicit extern finally fixed for foreach goto if implicit in internal is lock namespace new null operator out override params private protected public readonly ref return sealed sizeof stackalloc static switch this throw try typeof unchecked unsafe using virtual volatile while yield";

        string typeDeclarationPattern = keywordsToPattern(typeDeclarationKeywords);
        string keywordsPattern = keywordsToPattern(typeKeywords + " " + typeDeclarationKeywords + " " + contextualKeywords + " " + otherKeywords);
        string nonTypeKeywordsPattern = keywordsToPattern(typeDeclarationKeywords + " " + contextualKeywords + " " + otherKeywords);
        string nonContextualKeywordsPattern = keywordsToPattern(typeKeywords + " " + typeDeclarationKeywords + " " + otherKeywords);

        // types
        string generic = Nested(@"<(?:[^<>;=+\-*/%&|^]|<<self>>)*>", 2);
        string nestedRound = Nested(@"\((?:[^()]|<<self>>)*\)", 2);
        string name = @"@?\b[A-Za-z_]\w*\b";
        string genericName = Replace(@"<<0>>(?:\s*<<1>>)?", name, generic);
        string identifier = Replace(@"(?!<<0>>)<<1>>(?:\s*\.\s*<<1>>)*", nonTypeKeywordsPattern, genericName);
        string array = @"\[\s*(?:,\s*)*\]";
        string typeExpressionWithoutTuple = Replace(@"<<0>>(?:\s*(?:\?\s*)?<<1>>)*(?:\s*\?)?", identifier, array);
        string tupleElement = Replace(@"[^,()<>[\];=+\-*/%&|^]|<<0>>|<<1>>|<<2>>", generic, nestedRound, array);
        string tuple = Replace(@"\(<<0>>+(?:,<<0>>+)+\)", tupleElement);
        string typeExpression = Replace(@"(?:<<0>>|<<1>>)(?:\s*(?:\?\s*)?<<2>>)*(?:\s*\?)?", tuple, identifier, array);

        var typeInside = new Grammar();
        typeInside.Add("keyword", new Pattern(keywordsPattern));
        typeInside.Add("punctuation", new Pattern(@"[<>()?,.:[\]]"));

        // strings & characters
        string character = @"'(?:[^\r\n'\\]|\\.|\\[Uux][\da-fA-F]{1,8})'";
        string regularString = @"""(?:\\.|[^\\""\r\n])*""";
        string verbatimString = @"@""(?:""""|\\[\s\S]|[^\\""])*""(?!"")";

        // Extend Clike
        var grammar = Grammar.Extend(clike, new Grammar());

        grammar["string"] = new List<Pattern>
        {
            new Pattern(Replace(@"(^|[^$\\])<<0>>", verbatimString), lookbehind: true, greedy: true),
            new Pattern(Replace(@"(^|[^@$\\])<<0>>", regularString), lookbehind: true, greedy: true)
        };

        grammar["class-name"] = new List<Pattern>
        {
            new Pattern(Replace(@"(\busing\s+static\s+)<<0>>(?=\s*;)", identifier), lookbehind: true, inside: typeInside),
            new Pattern(Replace(@"(\busing\s+<<0>>\s*=\s*)<<1>>(?=\s*;)", name, typeExpression), lookbehind: true, inside: typeInside),
            new Pattern(Replace(@"(\busing\s+)<<0>>(?=\s*=)", name), lookbehind: true),
            new Pattern(Replace(@"(\b<<0>>\s+)<<1>>", typeDeclarationPattern, genericName), lookbehind: true, inside: typeInside),
            new Pattern(Replace(@"(\bcatch\s*\(\s*)<<0>>", identifier), lookbehind: true, inside: typeInside),
            new Pattern(Replace(@"(\bwhere\s+)<<0>>", name), lookbehind: true),
            new Pattern(Replace(@"(\b(?:is(?:\s+not)?|as)\s+)<<0>>", typeExpressionWithoutTuple), lookbehind: true, inside: typeInside),
            new Pattern(Replace(@"\b<<0>>(?=\s+(?!<<1>>|with\s*\{)<<2>>(?:\s*[=,;:{)\]]|\s+(?:in|when)\b))", typeExpression, nonContextualKeywordsPattern, name), inside: typeInside)
        };

        grammar["keyword"] = new List<Pattern> { new Pattern(keywordsPattern) };
        grammar["number"] = new List<Pattern> { new Pattern(@"(?:\b0(?:x[\da-f_]*[\da-f]|b[01_]*[01])|(?:\B\.\d+(?:_+\d+)*|\b\d+(?:_+\d+)*(?:\.\d+(?:_+\d+)*)?)(?:e[-+]?\d+(?:_+\d+)*)?)(?:[dflmu]|lu|ul)?\b", regexOptions: "i") };
        grammar["operator"] = new List<Pattern> { new Pattern(@">>=?|<<=?|[-=]>|([-+&|])\1|~|\?\?=?|[-+*/%&|^!=<>]=?") };
        grammar["punctuation"] = new List<Pattern> { new Pattern(@"\?\.?|::|[{}[\];(),.:]") };

        // Insert Before 'number'
        grammar.InsertBefore("number", new Grammar
        {
            { "range", new List<Pattern> { new Pattern(@"\.\.", alias: "operator") } }
        });

        // Insert Before 'punctuation'
        grammar.InsertBefore("punctuation", new Grammar
        {
            { "named-parameter", new List<Pattern> { new Pattern(Replace(@"([(,]\s*)<<0>>(?=\s*:)", name), lookbehind: true, alias: "punctuation") } }
        });

        // Insert Before 'class-name'
        var preprocessorInside = new Grammar();
        preprocessorInside.Add("directive", new Pattern(@"(#)\b(?:define|elif|else|endif|endregion|error|if|line|nullable|pragma|region|undef|warning)\b", lookbehind: true, alias: "keyword"));

        var genericMethodInside = new Grammar();
        genericMethodInside.Add("function", new Pattern(Replace(@"^<<0>>", name)));
        genericMethodInside.Add("generic", new Pattern(generic, alias: "class-name", inside: typeInside));

        var typeListInside = new Grammar();
        typeListInside.Add("record-arguments", new Pattern(Replace(@"(^(?!new\s*\()<<0>>\s*)<<1>>", genericName, nestedRound), lookbehind: true, greedy: true, inside: grammar)); // Recursive reference

        typeListInside.Add("keyword", new Pattern(keywordsPattern));
        typeListInside.Add("class-name", new Pattern(typeExpression, greedy: true, inside: typeInside));
        typeListInside.Add("punctuation", new Pattern(@"[,()]"));

        var insertBeforeClassName = new Grammar
        {
            { "namespace", new List<Pattern> { new Pattern(Replace(@"(\b(?:namespace|using)\s+)<<0>>(?:\s*\.\s*<<0>>)*(?=\s*[;{])", name), lookbehind: true, inside: new Grammar { { "punctuation", new List<Pattern> { new Pattern(@"\.") } } }) } },
            { "type-expression", new List<Pattern> { new Pattern(Replace(@"(\b(?:default|sizeof|typeof)\s*\(\s*(?!\s))(?:[^()\s]|\s(?!\s)|<<0>>)*(?=\s*\))", nestedRound), lookbehind: true, alias: "class-name", inside: typeInside) } },
            { "return-type", new List<Pattern> { new Pattern(Replace(@"<<0>>(?=\s+(?:<<1>>\s*(?:=>|[({]|\.\s*this\s*\[)|this\s*\[))", typeExpression, identifier), inside: typeInside, alias: "class-name") } },
            { "constructor-invocation", new List<Pattern> { new Pattern(Replace(@"(\bnew\s+)<<0>>(?=\s*[[({])", typeExpression), lookbehind: true, inside: typeInside, alias: "class-name") } },
            { "generic-method", new List<Pattern> { new Pattern(Replace(@"<<0>>\s*<<1>>(?=\s*\()", name, generic), inside: genericMethodInside) } },
            { "type-list", new List<Pattern> { new Pattern(Replace(@"\b((?:<<0>>\s+<<1>>|record\s+<<1>>\s*<<5>>|where\s+<<2>>)\s*:\s*)(?:<<3>>|<<4>>|<<1>>\s*<<5>>|<<6>>)(?:\s*,\s*(?:<<3>>|<<4>>|<<6>>))*(?=\s*(?:where|[{;]|=>|$))",
                typeDeclarationPattern, genericName, name, typeExpression, keywordsPattern, nestedRound, @"\bnew\s*\(\s*\)"), lookbehind: true, inside: typeListInside) } },
            { "preprocessor", new List<Pattern> { new Pattern(@"(^[\t ]*)#.*", regexOptions: "m", lookbehind: true, alias: "property", inside: preprocessorInside) } }
        };
        grammar.InsertBefore("class-name", insertBeforeClassName);

        // Attributes
        string regularStringOrCharacter = regularString + "|" + character;
        string regularStringCharacterOrComment = Replace(@"\/(?![*/])|\/\/[^\r\n]*[\r\n]|\/\*(?:[^*]|\*(?!\/))*\*\/|<<0>>", regularStringOrCharacter);
        string roundExpression = Nested(Replace(@"[^""'/()]|<<0>>|\(<<self>>*\)", regularStringCharacterOrComment), 2);
        string attrTarget = @"\b(?:assembly|event|field|method|module|param|property|return|type)\b";
        string attr = Replace(@"<<0>>(?:\s*\(<<1>>*\))?", identifier, roundExpression);

        var attributeInside = new Grammar();
        attributeInside.Add("target", new Pattern(Replace(@"^<<0>>(?=\s*:)", attrTarget), alias: "keyword"));
        attributeInside.Add("attribute-arguments", new Pattern(Replace(@"\(<<0>>*\)", roundExpression), inside: grammar)); // Recursive
        var attrClassNameInside = new Grammar();
        attrClassNameInside.Add("punctuation", new Pattern(@"\."));
        attributeInside.Add("class-name", new Pattern(identifier, inside: attrClassNameInside));
        attributeInside.Add("punctuation", new Pattern(@"[:,]"));

        grammar.InsertBefore("class-name", new Grammar
        {
            { "attribute", new List<Pattern> { new Pattern(Replace(@"((?:^|[^\s\w>)?])\s*\[\s*)(?:<<0>>\s*:\s*)?<<1>>(?:\s*,\s*<<1>>)*(?=\s*\])", attrTarget, attr), lookbehind: true, greedy: true, inside: attributeInside) } }
        });

        // String Interpolation
        string formatString = @":[^\}\r\n]+";
        string mInterpolationRound = Nested(Replace(@"[^""'/()]|<<0>>|\(<<self>>*\)", regularStringCharacterOrComment), 2);
        string mInterpolation = Replace(@"\{(?!\{)(?:(?![}:])<<0>>)*<<1>>?\}", mInterpolationRound, formatString);
        string sInterpolationRound = Nested(Replace(@"[^""'/()]|\/(?!\*)|\/\*(?:[^*]|\*(?!\/))*\*\/|<<0>>|\(<<self>>*\)", regularStringOrCharacter), 2);
        string sInterpolation = Replace(@"\{(?!\{)(?:(?![}:])<<0>>)*<<1>>?\}", sInterpolationRound, formatString);

        Grammar createInterpolationInside(string interpolation, string interpolationRound)
        {
            var inside = new Grammar();
            var formatStringInside = new Grammar();
            formatStringInside.Add("punctuation", new Pattern(@"^:"));
            inside.Add("format-string", new Pattern(Replace(@"(^\{(?:(?![}:])<<0>>)*)<<1>>(?=\}$)", interpolationRound, formatString), lookbehind: true, inside: formatStringInside));
            inside.Add("punctuation", new Pattern(@"^\{|\}$"));
            inside.Add("expression", new Pattern(@"[\s\S]+", alias: "language-csharp", inside: grammar)); // Recursive

            var interpolationGrammar = new Grammar();
            interpolationGrammar.Add("interpolation", new Pattern(Replace(@"((?:^|[^{])(?:\{\{)*)<<0>>", interpolation), lookbehind: true, inside: inside));
            interpolationGrammar.Add("string", new Pattern(@"[\s\S]+"));
            return interpolationGrammar;
        }

        grammar.InsertBefore("string", new Grammar
        {
            { "interpolation-string", new List<Pattern>
            {
                new Pattern(Replace(@"(^|[^\\])(?:\$@|@\$)(?:""""|\\[\s\S]|\{\{|<<0>>|[^\\{""\r\n])*""", mInterpolation), lookbehind: true, greedy: true, inside: createInterpolationInside(mInterpolation, mInterpolationRound)),
                new Pattern(Replace(@"(^|[^@\\])\$(?:\\.|\{\{|<<0>>|[^\\""{\r\n])*""", sInterpolation), lookbehind: true, greedy: true, inside: createInterpolationInside(sInterpolation, sInterpolationRound))
            }},
            { "char", new List<Pattern> { new Pattern(character, greedy: true) } }
        });

        return grammar;
    }
}
