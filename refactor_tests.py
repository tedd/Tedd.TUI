import re

with open("src/Tedd.TUI.Tests/TextEditorTests.cs", "r") as f:
    text = f.read()

# Replace the OnKeyDown_Navigation_Bounds method
match = re.search(r'    \[Fact\]\s+public void TextEditor_OnKeyDown_Navigation_Bounds\(\)\s+\{.*?(?=    \[Fact\])', text, re.DOTALL)
if match:
    old_test = match.group(0)

    new_test = """    [Theory]
    [InlineData(ConsoleKey.LeftArrow, 1, "AB\\r\\nC", "AB\\r\\nXC", 'X')]
    [InlineData(ConsoleKey.LeftArrow, 2, "AB\\r\\nC", "ABY\\r\\nC", 'Y')]
    [InlineData(ConsoleKey.RightArrow, 2, "ABY\\r\\nC", "ABY\\r\\nZC", 'Z')]
    [InlineData(ConsoleKey.UpArrow, 1, "ABY\\r\\nZC", "WABY\\r\\nZC", 'W')]
    [InlineData(ConsoleKey.DownArrow, 1, "WABY\\r\\nZC", "WABY\\r\\nZVC", 'V')]
    [InlineData(ConsoleKey.Home, 1, "WABY\\r\\nZVC", "WABY\\r\\nHZVC", 'H')]
    [InlineData(ConsoleKey.End, 1, "WABY\\r\\nHZVC", "WABY\\r\\nHZVCE", 'E')]
    public void TextEditor_OnKeyDown_Navigation_Bounds(ConsoleKey key, int repeatKey, string startText, string expectedText, char keyChar)
    {
        var editor = new TextEditor();
        editor.Text = startText;
        editor.OnGotFocus();
        editor.Width = 10;
        editor.Height = 10;
        editor.Measure(new Size(10, 10));
        editor.Arrange(new Rect(0, 0, 10, 10));

        // State setup simulation (simplified from original sequential test)
        if (key == ConsoleKey.LeftArrow && repeatKey == 2)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Col 0
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Line 0, End
        }
        else if (key == ConsoleKey.RightArrow && repeatKey == 2)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Col 0
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Line 0, End
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.RightArrow }); // Line 1, Col 0
        }
        else if (key == ConsoleKey.UpArrow && repeatKey == 1)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Col 0
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Line 0, End
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Col 2
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Col 1
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Col 0
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.UpArrow }); // Bound Up
        }
        else if (key == ConsoleKey.DownArrow && repeatKey == 1)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Col 0
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Line 0, End
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Col 2
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Col 1
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Col 0
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.DownArrow }); // Line 1, Col 0
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.DownArrow }); // Bound Down
        }
        else if (key == ConsoleKey.Home && repeatKey == 1)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Home }); // Line 1, Col 0
        }
        else if (key == ConsoleKey.End && repeatKey == 1)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.End }); // Line 1, End
        }
        else
        {
            // Default 1 LeftArrow
            for (int i = 0; i < repeatKey; i++)
            {
                editor.OnKeyDown(new KeyEventArgs { Key = key });
            }
        }

        editor.OnKeyDown(new KeyEventArgs { KeyChar = keyChar, Key = (ConsoleKey)char.ToUpper(keyChar) });
        Assert.Equal(expectedText, editor.Text);
    }
"""
    text = text.replace(old_test, new_test)

match2 = re.search(r'    \[Fact\]\s+public void TextEditor_OnKeyDown_Backspace_Delete\(\)\s+\{.*?(?=    \[Fact\])', text, re.DOTALL)
if match2:
    old_test2 = match2.group(0)

    new_test2 = """    [Theory]
    [InlineData("AB\\r\\nCD", ConsoleKey.Backspace, 0, "AB\\r\\nC")] // Delete char before cursor
    [InlineData("AB\\r\\nCD", ConsoleKey.Backspace, 1, "ABCD")] // Delete newline (from home)
    [InlineData("AB\\r\\nC", ConsoleKey.Delete, 2, "AB")] // Delete current char
    [InlineData("AB\\r\\nCD", ConsoleKey.Delete, 3, "ABCD")] // Delete newline from line end
    public void TextEditor_OnKeyDown_Backspace_Delete(string startText, ConsoleKey key, int setupMode, string expectedText)
    {
        var editor = new TextEditor();
        editor.Text = startText;
        editor.OnGotFocus();

        if (setupMode == 1)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Home }); // Start of line 1
        }
        else if (setupMode == 2)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Home }); // Start of line 1
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Backspace }); // Merged ABC
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // Moved back
            // Target is C
        }
        else if (setupMode == 3)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.UpArrow }); // Line 0
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.End }); // Line 0 end
        }

        editor.OnKeyDown(new KeyEventArgs { Key = key });
        Assert.Equal(expectedText, editor.Text);
    }
"""
    text = text.replace(old_test2, new_test2)

with open("src/Tedd.TUI.Tests/TextEditorTests.cs", "w") as f:
    f.write(text)
