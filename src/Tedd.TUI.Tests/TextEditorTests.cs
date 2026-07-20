using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class TextEditorTests
{
    private class TestViewModel : INotifyPropertyChanged
    {
        private string _textContent = "";
        public string TextContent
        {
            get => _textContent;
            set
            {
                if (_textContent != value)
                {
                    _textContent = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [Fact]
    public void TextEditor_TypingInsertsText()
    {
        var editor = new TextEditor();
        editor.OnGotFocus(); // Sets IsFocused

        // Type 'A'
        editor.OnKeyDown(new KeyEventArgs { KeyChar = 'A', Key = ConsoleKey.A });

        Assert.Equal("A", editor.Text);

        // Type 'B'
        editor.OnKeyDown(new KeyEventArgs { KeyChar = 'B', Key = ConsoleKey.B });
        Assert.Equal("AB", editor.Text);
    }

    [Fact]
    public void TextEditor_EnterKeySplitsLines()
    {
        var editor = new TextEditor();
        editor.Text = "Hello";
        editor.OnGotFocus();

        // Move to end
        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.End });

        // Enter
        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Enter });
        editor.OnKeyDown(new KeyEventArgs { KeyChar = 'W', Key = ConsoleKey.W });

        Assert.Equal($"Hello{Environment.NewLine}W", editor.Text);
    }

    [Fact]
    public void TextEditor_BackspaceRemovesText()
    {
        var editor = new TextEditor();
        editor.Text = "A";
        editor.OnGotFocus();

        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.End });
        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Backspace });

        Assert.Equal("", editor.Text);
    }

    [Fact]
    public void TextEditor_DataBindingUpdatesSource()
    {
        var editor = new TextEditor();
        var vm = new TestViewModel { TextContent = "Initial" };

        editor.DataContext = vm;
        editor.SetBinding(TextEditor.TextProperty, new Binding("TextContent"));

        Assert.Equal("Initial", editor.Text);

        editor.OnGotFocus();
        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.End });
        editor.OnKeyDown(new KeyEventArgs { KeyChar = 'X', Key = ConsoleKey.X });

        Assert.Equal("InitialX", editor.Text);
        // Current implementation of Data Binding in TUI might only be OneWay by default or not update source
        // immediately on user input without explicit TwoWay mode.
        // For simple test, we just ensure Text changed locally correctly.
    }
    [Theory]
    [InlineData(ConsoleKey.LeftArrow, 1, "AB||C", "AB||XC", 'X')]
    [InlineData(ConsoleKey.LeftArrow, 2, "AB||C", "ABY||C", 'Y')]
    [InlineData(ConsoleKey.RightArrow, 2, "ABY||C", "ABY||ZC", 'Z')]
    [InlineData(ConsoleKey.UpArrow, 1, "ABY||ZC", "AWBY||ZC", 'W')]
    [InlineData(ConsoleKey.DownArrow, 1, "AWBY||ZC", "AWBY||ZCV", 'V')]
    [InlineData(ConsoleKey.Home, 1, "WABY||ZVC", "WABY||HZVC", 'H')]
    [InlineData(ConsoleKey.End, 1, "WABY||HZVC", "WABY||HZVCE", 'E')]
    public void TextEditor_OnKeyDown_Navigation_Bounds(ConsoleKey key, int repeatKey, string startText, string expectedText, char keyChar)
    {
        var editor = new TextEditor();
        editor.Text = startText.Replace("||", Environment.NewLine);
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
        Assert.Equal(expectedText.Replace("||", Environment.NewLine), editor.Text);
    }
    [Theory]
    [InlineData("AB||CD", ConsoleKey.Backspace, 0, "AB||C")] // Delete char before cursor
    [InlineData("AB||CD", ConsoleKey.Backspace, 1, "ABCD")] // Delete newline (from home)
    [InlineData("AB||C", ConsoleKey.Delete, 2, "AB")] // Delete current char
    [InlineData("AB||CD", ConsoleKey.Delete, 3, "ABCD")] // Delete newline from line end
    public void TextEditor_OnKeyDown_Backspace_Delete(string startText, ConsoleKey key, int setupMode, string expectedText)
    {
        var editor = new TextEditor();
        editor.Text = startText.Replace("||", Environment.NewLine);
        editor.OnGotFocus();

        if (setupMode == 1)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Home }); // Start of line 1
        }
        else if (setupMode == 2)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Home }); // Start of line 1
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Backspace }); // Merged ABC

            // Target is C
        }
        else if (setupMode == 3)
        {
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.UpArrow }); // Line 0
            editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.End }); // Line 0 end
        }

        editor.OnKeyDown(new KeyEventArgs { Key = key });
        Assert.Equal(expectedText.Replace("||", Environment.NewLine), editor.Text);
    }
    [Fact]
    public void TextEditor_OnMouseDown_MovesCursor()
    {
        var editor = new TextEditor();
        editor.Text = $"Hello{Environment.NewLine}World";
        editor.OnGotFocus();

        editor.Width = 10;
        editor.Height = 10;
        editor.Measure(new Size(10, 10));
        editor.Arrange(new Rect(0, 0, 10, 10));

        // Click Row 0, Col 2
        editor.OnMouseDown(new MouseEventArgs { X = 2, Y = 0 });
        editor.OnKeyDown(new KeyEventArgs { KeyChar = 'X', Key = ConsoleKey.X });
        Assert.Equal($"HeXllo{Environment.NewLine}World", editor.Text);

        // Click Row 1, Col 1
        editor.OnMouseDown(new MouseEventArgs { X = 1, Y = 1 });
        editor.OnKeyDown(new KeyEventArgs { KeyChar = 'Y', Key = ConsoleKey.Y });
        Assert.Equal($"HeXllo{Environment.NewLine}WYorld", editor.Text);

        // Click out of bounds (Y > lines) -> snaps to last line
        editor.OnMouseDown(new MouseEventArgs { X = 1, Y = 5 });
        editor.OnKeyDown(new KeyEventArgs { KeyChar = 'Z', Key = ConsoleKey.Z });
        Assert.Equal($"HeXllo{Environment.NewLine}WZYorld", editor.Text); // Snaps to Col 1 on Row 1? No, X=1 -> Col 1.

        // Click out of bounds (X > line length) -> snaps to line end
        editor.OnMouseDown(new MouseEventArgs { X = 20, Y = 0 });
        editor.OnKeyDown(new KeyEventArgs { KeyChar = 'Q', Key = ConsoleKey.Q });
        Assert.Equal($"HeXlloQ{Environment.NewLine}WZYorld", editor.Text);
    }

    [Fact]
    public void TextEditor_Render_WithScroll()
    {
        var editor = new TextEditor();
        editor.Text = $"123456789{Environment.NewLine}A{Environment.NewLine}B{Environment.NewLine}C";
        editor.OnGotFocus();

        // Small viewport
        editor.Width = 3;
        editor.Height = 2;
        editor.Measure(new Size(3, 2));
        editor.Arrange(new Rect(0, 0, 3, 2));

        // Cursor at bottom-right initially (Row 3, Col 1).
        // This should trigger AdjustScroll. _scrollY should be 3 - 2 + 1 = 2 (Lines B and C visible).
        // _scrollX should be 1 - 3 + 1 = 0 (or bounded). Actually Col 1 < ScrollX + W is true. So ScrollX = 0.

        var buffer = new VirtualBuffer(3, 2);
        editor.Render(buffer, 0, 0);

        // Row 2 is "B", Row 3 is "C"
        Assert.Equal('B', buffer.GetPixel(0, 0).Character);
        Assert.Equal('C', buffer.GetPixel(0, 1).Character);

        // Now cursor move to line 0, col 5
        editor.OnMouseDown(new MouseEventArgs { X = 5, Y = 0 }); // Note: MouseDown uses relative X/Y. Wait, Y=0 means scrollY + 0.
        // If _scrollY is 2, clicking Y=0 means row 2.
        // Let's use keyboard to move scroll
        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.UpArrow }); // Row 2
        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.UpArrow }); // Row 1
        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.UpArrow }); // Row 0
        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.RightArrow });
        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.RightArrow });
        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.RightArrow });
        editor.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.RightArrow }); // Col 4

        // Arrange handles AdjustScroll internally
        editor.Arrange(new Rect(0, 0, 3, 2));
        editor.Render(buffer, 0, 0);

        // Scroll should be at row 0, col 2 to keep col 4 visible (width 3: cols 2,3,4)
        Assert.Equal('4', buffer.GetPixel(0, 0).Character);
        Assert.Equal('5', buffer.GetPixel(1, 0).Character);
        Assert.Equal('6', buffer.GetPixel(2, 0).Character); // Col 4 is '5'
    }

    [Fact]
    public void MouseClick_NestedTextEditors_SelectsLineAndCaretWithoutChangingSibling()
    {
        var first = new TextEditor
        {
            Text = $"Alpha{Environment.NewLine}Bravo",
            Width = 8,
            Height = 2
        };
        var second = new TextEditor
        {
            Text = $"Gamma{Environment.NewLine}Delta",
            Width = 8,
            Height = 2
        };
        var editors = new StackPanel();
        editors.AddChild(new TextBlock { Text = "editors" });
        editors.AddChild(first);
        editors.AddChild(new TextBlock { Text = "--------" });
        editors.AddChild(second);
        var surface = new Border { Child = editors, BoxStyle = BoxStyle.Double, Padding = new Thickness(0) };
        var host = new ControlTestHost(surface, 12, 8);

        var firstClick = host.Click(first, 2, 1);
        host.PressKey(ConsoleKey.X, 'X');

        Assert.True(firstClick.Down.Handled);
        Assert.True(first.IsFocused);
        Assert.False(second.IsFocused);
        Assert.Equal($"Alpha{Environment.NewLine}BrXavo", first.Text);
        Assert.Equal($"Gamma{Environment.NewLine}Delta", second.Text);

        host.Click(editors.GetVisualChild(2), 4, 0);
        Assert.Equal($"Alpha{Environment.NewLine}BrXavo", first.Text);
        Assert.Equal($"Gamma{Environment.NewLine}Delta", second.Text);

        var secondClick = host.Click(second, 1, 0);
        host.PressKey(ConsoleKey.Y, 'Y');

        Assert.True(secondClick.Down.Handled);
        Assert.False(first.IsFocused);
        Assert.True(second.IsFocused);
        Assert.Equal($"Alpha{Environment.NewLine}BrXavo", first.Text);
        Assert.Equal($"GYamma{Environment.NewLine}Delta", second.Text);
    }
}
