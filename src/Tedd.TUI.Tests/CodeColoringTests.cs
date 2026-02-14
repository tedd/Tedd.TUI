using System;
using System.Collections.Generic;
using System.Linq;
using Tedd.TUI.CodeColoring;
using Xunit;

namespace Tedd.TUI.Tests;

public class CodeColoringTests
{
    [Fact]
    public void BasicTokenizerTest()
    {
        var grammar = new Grammar();
        grammar.Add("keyword", new Pattern(@"\b(if|else)\b"));
        grammar.Add("number", new Pattern(@"\d+"));

        var tokens = PrismTokenizer.Tokenize("if 123 else", grammar);

        Assert.Equal(5, tokens.Count);
        Assert.Equal("keyword", tokens[0].Type);
        Assert.Equal("if", tokens[0].TextContent);
        Assert.Equal("number", tokens[2].Type);
        Assert.Equal("123", tokens[2].TextContent);
    }

    [Fact]
    public void CSharpTest()
    {
        var code = "class Foo { int x; }";
        var grammar = LanguageRegistry.GetGrammar("csharp");
        Assert.NotNull(grammar);

        var tokens = PrismTokenizer.Tokenize(code, grammar);

        // Tokens:
        // class (keyword)
        // space
        // Foo (class-name? No, standard C# grammar needs context for class name declaration. 'class Foo' -> Foo should be class-name)
        // space
        // { (punctuation)
        // space
        // int (keyword/type)
        // space
        // x (text/identifier)
        // ; (punctuation)
        // space
        // } (punctuation)

        var types = tokens.Select(t => t.Type).ToList();
        Assert.Contains("keyword", types); // class
        Assert.Contains("punctuation", types); // {

        // Verify 'class' is keyword
        var keywordToken = tokens.FirstOrDefault(t => t.TextContent == "class");
        Assert.NotNull(keywordToken);
        Assert.Equal("keyword", keywordToken.Type);
    }

    [Fact]
    public void JsonTest()
    {
        var code = "{ \"key\": 123 }";
        var grammar = LanguageRegistry.GetGrammar("json");
        Assert.NotNull(grammar);

        var tokens = PrismTokenizer.Tokenize(code, grammar);

        // { (punctuation)
        // space
        // "key" (property)
        // : (operator)
        // space
        // 123 (number)
        // space
        // } (punctuation)

        var propToken = tokens.FirstOrDefault(t => t.TextContent == "\"key\"");
        Assert.NotNull(propToken);
        Assert.Equal("property", propToken.Type);

        var numberToken = tokens.FirstOrDefault(t => t.TextContent == "123");
        Assert.NotNull(numberToken);
        Assert.Equal("number", numberToken.Type);
    }

    [Fact]
    public void XmlTest()
    {
        var code = "<tag attr=\"val\">";
        var grammar = LanguageRegistry.GetGrammar("xml");
        Assert.NotNull(grammar);

        var tokens = PrismTokenizer.Tokenize(code, grammar);

        // <tag attr="val"> is matched by 'tag' pattern.
        // It returns a token of type 'tag' with nested content.

        Assert.Single(tokens);
        var tagToken = tokens[0];
        Assert.Equal("tag", tagToken.Type);

        var inner = tagToken.StreamContent;
        Assert.NotNull(inner);
        // <tag (tag)
        // attr (attr-name)
        // ="val" (attr-value)
        // > (punctuation)

        // "tag" inside "tag": pattern /^<\/?(?!\d)[^\s>\/=$<%]+/ captures <tag
        var innerTag = inner.FirstOrDefault(t => t.Type == "tag");
        Assert.NotNull(innerTag);
        // innerTag has nested content (< and tag)
        Assert.NotNull(innerTag.StreamContent);

        // attr-value: ="val"
        var attrVal = inner.FirstOrDefault(t => t.Type == "attr-value");
        Assert.NotNull(attrVal);
    }

    [Fact]
    public void BashTest()
    {
        var code = "echo \"hello\"";
        var grammar = LanguageRegistry.GetGrammar("bash");
        Assert.NotNull(grammar);

        var tokens = PrismTokenizer.Tokenize(code, grammar);

        // echo (builtin or function)
        // space
        // "hello" (string)

        var echo = tokens.FirstOrDefault(t => t.TextContent == "echo");
        Assert.NotNull(echo);
        Assert.True(echo.Type == "builtin" || echo.Type == "function");

        // "hello" matches string, but might have inner content
        var str = tokens.FirstOrDefault(t => t.Type == "string");
        Assert.NotNull(str);
        // Content might be nested or string depending on grammar logic
    }

    [Fact]
    public void BasicTest()
    {
        var code = "PRINT \"Hello\"";
        var grammar = LanguageRegistry.GetGrammar("basic");
        Assert.NotNull(grammar);

        var tokens = PrismTokenizer.Tokenize(code, grammar);

        var print = tokens.FirstOrDefault(t => t.TextContent == "PRINT");
        Assert.NotNull(print);
        Assert.Equal("function", print.Type); // Or keyword? In Basic definition it is under function? No wait.
        // Check Basic.cs:
        // 'keyword' has PRINT? No.
        // 'function' has PRINT? Yes.
        // Actually PRINT is a command/statement, often keyword, but Prism puts it in function or keyword?
        // Basic.cs: grammar.Add("function", ... PRINT ...)
        // Wait, checking Basic.cs...
        // grammar.Add("function", ... PRINT ...)
        // grammar.Add("keyword", ... PRINT ...) ? No.
        // Actually checking Basic.cs content:
        // keyword: AS|BEEP|...|PRINT|... ?
        // function: ABS|...|PRINT|... ?
        // Let's check my Basic.cs file content or logic.
        // It seems PRINT is in 'function'.

        Assert.Equal("function", print.Type);
    }

    [Fact]
    public void BatchTest()
    {
        var code = "ECHO Hello";
        var grammar = LanguageRegistry.GetGrammar("batch");
        Assert.NotNull(grammar);

        var tokens = PrismTokenizer.Tokenize(code, grammar);

        // ECHO matched by command (other commands)
        // Hello matched as string or parameter?
        // Or text.

        // Command regex: ((?:^|[&(])[ \t]*@?)\w+\b...
        // Matches "ECHO Hello"

        Assert.Single(tokens);
        var cmd = tokens[0];
        Assert.Equal("command", cmd.Type);

        // Inside: keyword (ECHO), text ( Hello)
        var inner = cmd.StreamContent;
        var echo = inner.FirstOrDefault(t => t.TextContent == "ECHO");
        Assert.NotNull(echo);
        Assert.Equal("keyword", echo.Type);
    }

    [Fact]
    public void PowerShellTest()
    {
        var code = "Write-Host \"Hello\"";
        var grammar = LanguageRegistry.GetGrammar("powershell");
        Assert.NotNull(grammar);

        var tokens = PrismTokenizer.Tokenize(code, grammar);

        var writeHost = tokens.FirstOrDefault(t => t.TextContent == "Write-Host");
        Assert.NotNull(writeHost);
        Assert.Equal("function", writeHost.Type);
    }

    [Fact]
    public void RegexTest()
    {
        var code = "[a-z]+";
        var grammar = LanguageRegistry.GetGrammar("regex");
        Assert.NotNull(grammar);

        var tokens = PrismTokenizer.Tokenize(code, grammar);

        // [a-z] (char-class)
        // + (quantifier)

        var charClass = tokens.FirstOrDefault(t => t.Type == "char-class");
        Assert.NotNull(charClass);

        var quant = tokens.FirstOrDefault(t => t.TextContent == "+");
        Assert.NotNull(quant);
        Assert.Equal("quantifier", quant.Type); // or number/operator alias
    }

    [Fact]
    public void YamlTest()
    {
        var code = "key: value";
        var grammar = LanguageRegistry.GetGrammar("yaml");
        Assert.NotNull(grammar);

        var tokens = PrismTokenizer.Tokenize(code, grammar);

        // key: (key)
        // space
        // value (string/text)

        // Regex for key: matches "key:" ?
        // JS: `(\s*(?:^|[:\-?])[ \t]*)(?:...)(?=\s*:)`
        // It uses lookahead `(?=\s*:)`. So it matches "key".
        // Colon remains?
        // Wait, punctuation pattern matches colon.

        var key = tokens.FirstOrDefault(t => t.TextContent.Trim() == "key");
        Assert.NotNull(key);
        Assert.Equal("key", key.Type); // Type is key, Alias is atrule
        Assert.Equal("atrule", key.Alias);

        var colon = tokens.FirstOrDefault(t => t.TextContent == ":");
        Assert.NotNull(colon);
        Assert.Equal("punctuation", colon.Type);
    }

    [Fact]
    public void CodeDocumentTest()
    {
        var doc = new CodeDocument();
        doc.SetCode("line1\nline2", "text");

        // Check children
        Assert.Equal(2, doc.Children.Count);

        var line1 = doc.Children[0] as StackPanel;
        Assert.NotNull(line1);
        Assert.Equal(Orientation.Horizontal, line1.Orientation);

        var text1 = line1.Children[0] as TextBlock;
        Assert.Equal("line1", text1.Text);
    }
}
