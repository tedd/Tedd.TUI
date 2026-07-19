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
        yield return ["toml", "# config\n[server.web]\nhost = \"localhost\"\nport = 8080\nenabled = true\ndate = 2024-01-15"];
        yield return ["ini", "; comment\n[section name]\nkey = \"value\"\nother=42"];
        yield return ["graphql", "# comment\nquery GetUser($id: ID!) {\n  user(id: $id) { name email }\n}"];
        yield return ["docker", "# build stage\nFROM node:20 AS build\nRUN --mount=type=cache npm ci\nENV FOO=\"bar\""];
        yield return ["makefile", "# rules\n.PHONY: all\nall: main.o\n\t$(CC) -o $@ $^\nCFLAGS := -Wall"];
        yield return ["git", "$ git status\n# On branch main\ncommit a11a14ef7e26f2ca62d4b35eac455ce636d0dc09\n@@ -1 +1,2 @@\n-old line\n+new line"];
        yield return ["nginx", "# comment\nserver {\n    listen 80;\n    server_name example.com;\n    root \"/var/www\";\n}"];
        yield return ["cmake", "# build\ncmake_minimum_required(VERSION 3.20)\nproject(Demo)\nset(SRC \"${CMAKE_SOURCE_DIR}/main.cpp\")"];
        yield return ["hcl", "# tf\nresource \"aws_instance\" \"web\" {\n  ami = \"ami-123\"\n  count = 2\n  tag = \"${var.name}\"\n}"];
        yield return ["http", "GET /api/users HTTP/1.1\nHost: example.com\nContent-Type: application/json\n\n{\"id\": 1}"];
        yield return ["protobuf", "syntax = \"proto3\";\nmessage User {\n  string name = 1;\n  map<string, int32> tags = 2;\n}"];
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
