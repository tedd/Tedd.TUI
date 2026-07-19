using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class HaskellLanguage : ILanguage
{
    public string Id => "haskell";
    public string[] Aliases => ["hs"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();

        grammar.Add("comment", new Pattern(@"(^|[^-!#$%*+=?&@|~.:<>^\\\/])(?:--(?:(?=.)[^-!#$%*+=?&@|~.:<>^\\\/].*|$)|\{-[\s\S]*?-\})", regexOptions: "m", lookbehind: true));
        grammar.Add("char", new Pattern(@"'(?:[^\\']|\\(?:[abfnrtv\\""'&]|\^[A-Z@[\]^_]|ACK|BEL|BS|CAN|CR|DC1|DC2|DC3|DC4|DEL|DLE|EM|ENQ|EOT|ESC|ETB|ETX|FF|FS|GS|HT|LF|NAK|NUL|RS|SI|SO|SOH|SP|STX|SUB|SYN|US|VT|\d+|o[0-7]+|x[0-9a-fA-F]+))'", alias: "string"));
        grammar.Add("string", new Pattern(@"""(?:[^\\""]|\\(?:\S|\s+\\))*""", greedy: true));
        grammar.Add("keyword", new Pattern(@"\b(?:case|class|data|deriving|do|else|if|in|infixl|infixr|instance|let|module|newtype|of|primitive|then|type|where)\b"));

        var importInside = new Grammar();
        importInside.Add("keyword", new Pattern(@"\b(?:as|hiding|import|qualified)\b"));
        importInside.Add("punctuation", new Pattern(@"\."));
        grammar.Add("import-statement", new Pattern(@"(^[\t ]*)import\s+(?:qualified\s+)?(?:[A-Z][\w']*)(?:\.[A-Z][\w']*)*(?:\s+as\s+(?:[A-Z][\w']*)(?:\.[A-Z][\w']*)*)?(?:\s+hiding\b)?", regexOptions: "m", lookbehind: true, inside: importInside));

        grammar.Add("builtin", new Pattern(@"\b(?:abs|acos|acosh|all|and|any|appendFile|approxRational|asTypeOf|asin|asinh|atan|atan2|atanh|basicIORun|break|catch|ceiling|chr|compare|concat|concatMap|const|cos|cosh|curry|cycle|decodeFloat|denominator|digitToInt|div|divMod|drop|dropWhile|either|elem|encodeFloat|enumFrom|enumFromThen|enumFromThenTo|enumFromTo|error|even|exp|exponent|fail|filter|flip|floatDigits|floatRadix|floatRange|floor|fmap|foldl|foldl1|foldr|foldr1|fromDouble|fromEnum|fromInt|fromInteger|fromIntegral|fromRational|fst|gcd|getChar|getContents|getLine|group|head|id|inRange|index|init|intToDigit|interact|ioError|isAlpha|isAlphaNum|isAscii|isControl|isDenormalized|isDigit|isHexDigit|isIEEE|isInfinite|isLower|isNaN|isNegativeZero|isOctDigit|isPrint|isSpace|isUpper|iterate|last|lcm|length|lex|lexDigits|lexLitChar|lines|log|logBase|lookup|map|mapM|mapM_|max|maxBound|maximum|maybe|min|minBound|minimum|mod|negate|not|notElem|null|numerator|odd|or|ord|otherwise|pack|pi|pred|primExitWith|print|product|properFraction|putChar|putStr|putStrLn|quot|quotRem|range|rangeSize|read|readDec|readFile|readFloat|readHex|readIO|readInt|readList|readLitChar|readLn|readOct|readParen|readSigned|reads|readsPrec|realToFrac|recip|rem|repeat|replicate|return|reverse|round|scaleFloat|scanl|scanl1|scanr|scanr1|seq|sequence|sequence_|show|showChar|showInt|showList|showLitChar|showParen|showSigned|showString|shows|showsPrec|significand|signum|sin|sinh|snd|sort|span|splitAt|sqrt|subtract|succ|sum|tail|take|takeWhile|tan|tanh|threadToIOResult|toEnum|toInt|toInteger|toLower|toRational|toUpper|truncate|uncurry|undefined|unlines|until|unwords|unzip|unzip3|userError|words|writeFile|zip|zip3|zipWith|zipWith3)\b"));

        grammar.Add("number", new Pattern(@"\b(?:\d+(?:\.\d+)?(?:e[+-]?\d+)?|0o[0-7]+|0x[0-9a-f]+)\b", regexOptions: "i"));

        grammar.Add("operator", new List<Pattern>
        {
            // infix operator
            new Pattern(@"`(?:[A-Z][\w']*\.)*[_a-z][\w']*`", greedy: true),
            // function composition
            new Pattern(@"(\s)\.(?=\s)", lookbehind: true),
            // ascii operators
            new Pattern(@"[-!#$%*+=?&@|~:<>^\\\/][-!#$%*+=?&@|~.:<>^\\\/]*|\.[-!#$%*+=?&@|~.:<>^\\\/]+")
        });

        var dotPunctuation = new Grammar();
        dotPunctuation.Add("punctuation", new Pattern(@"\."));

        // In Haskell, nearly everything is a variable, do not highlight these.
        grammar.Add("hvariable", new Pattern(@"\b(?:[A-Z][\w']*\.)*[_a-z][\w']*", inside: dotPunctuation));
        grammar.Add("constant", new Pattern(@"\b(?:[A-Z][\w']*\.)*[A-Z][\w']*", inside: dotPunctuation));
        grammar.Add("punctuation", new Pattern(@"[{}[\];(),.:]"));

        return grammar;
    }
}
