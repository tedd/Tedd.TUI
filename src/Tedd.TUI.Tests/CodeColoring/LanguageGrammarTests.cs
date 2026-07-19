using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tedd.TUI.CodeColoring;
using Xunit;

namespace Tedd.TUI.Tests.CodeColoring;

// Smoke tests for the ported Prism grammars: every language must load, tokenize a
// representative snippet without throwing, produce at least one typed token, and
// the token stream must reassemble to exactly the input text.
public class LanguageGrammarTests
{
    public static IEnumerable<object[]> Samples()
    {
        yield return ["javascript", "const f = async (x) => { return `hi ${x.name}`; } // done"];
        yield return ["typescript", "interface Foo<T> { bar: T; }\nconst x: number = 42; @decorator\nclass A {}"];
        yield return ["c", "#include <stdio.h>\nint main(void) { /* hi */ printf(\"%d\\n\", 42); return 0; }"];
        yield return ["cpp", "#include <vector>\nclass Foo : public Bar { std::vector<int> v; };\nauto s = R\"(raw)\";"];
        yield return ["java", "import java.util.List;\npublic class Foo<T> { @Override int x = 0b1010; String s = \"hi\"; }"];
        yield return ["go", "package main\nfunc main() { s := `raw`\n\tfmt.Println(\"hi\", 0x1F, 'c') }"];
        yield return ["kotlin", "fun main() { val x = \"hello $name and ${1 + 2}\"\n@Anno class Foo }"];
        yield return ["swift", "func greet(name: String) -> String { return \"Hello \\(name)\" } // eol\nlet n = 0x1F"];
        yield return ["dart", "import 'dart:io';\nclass Point { final num x; Point(this.x); }\nvar s = 'a $b c';"];
        yield return ["ruby", "# comment\nclass Foo\n  def bar(x)\n    :sym\n    @var = \"hi #{x}\"\n  end\nend"];
        yield return ["php", "<?php\nnamespace App;\nclass Foo {\n  public function bar(int $x): string { return \"v=$x\"; }\n}\n?>"];
        yield return ["r", "f <- function(x) {\n  # comment\n  y <- x %in% c(1.5, TRUE)\n}"];
        yield return ["julia", "function f(x)\n  # comment\n  s = \"hi\"\n  return x^2 + 1.5\nend"];
        yield return ["groovy", "def greet(name) {\n  println \"Hello ${name}!\" // eol\n  @Override int x = 0\n}"];
        yield return ["scala", "object Main { def main(): Unit = { val s = s\"v=${1 + 2}\"\n println(\"hi\") } }"];
        yield return ["objectivec", "@interface Foo : NSObject\n- (void)bar:(NSString *)s;\n@end\nNSString *x = @\"hi\"; // c"];
        yield return ["vb", "Module M\n  ' comment\n  Sub Main()\n    Dim x As Integer = &H1F\n    Console.WriteLine(\"hi\")\n  End Sub\nEnd Module"];
        yield return ["fsharp", "module M =\n  (* block *)\n  let add x y = x + y\n  [<EntryPoint>]\n  let main _ = printfn \"hi\"; 0"];
        yield return ["haskell", "module Main where\n-- comment\nmain :: IO ()\nmain = putStrLn \"hi\" >> print 42"];
        yield return ["elixir", "defmodule Foo do\n  # comment\n  def bar(x), do: \"v=#{x}\"\nend"];
        yield return ["erlang", "-module(foo).\n%% comment\nbar(X) -> io:format(\"~p~n\", [X])."];
        yield return ["clojure", "(defn greet [name]\n  ; comment\n  (str \"Hello \" name 42))"];
        yield return ["lisp", "(defun greet (name)\n  ;; comment\n  (message \"Hello %s\" name))"];
        yield return ["scheme", "(define (square x)\n  ; comment\n  (* x x))\n(display \"hi\") #t 3.14"];
        yield return ["ocaml", "(* comment *)\nlet rec fact n = if n <= 1 then 1 else n * fact (n - 1)\nlet s = \"hi\""];
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void GrammarTokenizesRoundTrip(string language, string code)
    {
        var grammar = LanguageRegistry.GetGrammar(language);
        Assert.NotNull(grammar);

        var tokens = PrismTokenizer.Tokenize(code, grammar);

        Assert.Equal(code, Flatten(tokens));
        Assert.Contains(tokens, t => t.Type != "text");
    }

    private static string Flatten(List<Token> tokens)
    {
        var sb = new StringBuilder();
        foreach (var token in tokens)
        {
            Append(sb, token);
        }
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, Token token)
    {
        if (token.Content is string s)
        {
            sb.Append(s);
        }
        else if (token.Content is List<Token> nested)
        {
            foreach (var child in nested)
            {
                Append(sb, child);
            }
        }
    }
}
