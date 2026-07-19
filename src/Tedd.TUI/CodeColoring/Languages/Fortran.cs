using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class FortranLanguage : ILanguage
{
    public string Id => "fortran";
    public string[] Aliases => ["f90"];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("quoted-number", new Pattern(@"[BOZ](['""])[A-F0-9]+\1", regexOptions: "i", alias: "number"));

        var stringInside = new Grammar();
        stringInside.Add("comment", new Pattern(@"(&(?:\r\n?|\n)\s*)!.*", lookbehind: true));
        grammar.Add("string", new Pattern(@"(?:\b\w+_)?(['""])(?:\1\1|&(?:\r\n?|\n)(?:[ \t]*!.*(?:\r\n?|\n)|(?![ \t]*!))|(?!\1).)*(?:\1|&)", inside: stringInside));

        grammar.Add("comment", new Pattern(@"!.*", greedy: true));
        grammar.Add("boolean", new Pattern(@"\.(?:FALSE|TRUE)\.(?:_\w+)?", regexOptions: "i"));
        grammar.Add("number", new Pattern(@"(?:\b\d+(?:\.\d*)?|\B\.\d+)(?:[ED][+-]?\d+)?(?:_\w+)?", regexOptions: "i"));
        grammar.Add("keyword", new List<Pattern>
        {
            new Pattern(@"\b(?:CHARACTER|COMPLEX|DOUBLE ?PRECISION|INTEGER|LOGICAL|REAL)\b", regexOptions: "i"),
            new Pattern(@"\b(?:END ?)?(?:BLOCK ?DATA|DO|FILE|FORALL|FUNCTION|IF|INTERFACE|MODULE(?! PROCEDURE)|PROGRAM|SELECT|SUBROUTINE|TYPE|WHERE)\b", regexOptions: "i"),
            new Pattern(@"\b(?:ALLOCATABLE|ALLOCATE|BACKSPACE|CALL|CASE|CLOSE|COMMON|CONTAINS|CONTINUE|CYCLE|DATA|DEALLOCATE|DIMENSION|DO|END|EQUIVALENCE|EXIT|EXTERNAL|FORMAT|GO ?TO|IMPLICIT(?: NONE)?|INQUIRE|INTENT|INTRINSIC|MODULE PROCEDURE|NAMELIST|NULLIFY|OPEN|OPTIONAL|PARAMETER|POINTER|PRINT|PRIVATE|PUBLIC|READ|RETURN|REWIND|SAVE|SELECT|STOP|TARGET|WHILE|WRITE)\b", regexOptions: "i"),
            new Pattern(@"\b(?:ASSIGNMENT|DEFAULT|ELEMENTAL|ELSE|ELSEIF|ELSEWHERE|ENTRY|IN|INCLUDE|INOUT|KIND|NULL|ONLY|OPERATOR|OUT|PURE|RECURSIVE|RESULT|SEQUENCE|STAT|THEN|USE)\b", regexOptions: "i")
        });
        grammar.Add("operator", new List<Pattern>
        {
            new Pattern(@"\*\*|\/\/|=>|[=\/]=|[<>]=?|::|[+\-*=%]|\.[A-Z]+\.", regexOptions: "i"),
            // Use lookbehind to prevent confusion with (/ /)
            new Pattern(@"(^|(?!\().)\/(?!\))", lookbehind: true)
        });
        grammar.Add("punctuation", new Pattern(@"\(\/|\/\)|[(),;:&]"));
        return grammar;
    }
}
