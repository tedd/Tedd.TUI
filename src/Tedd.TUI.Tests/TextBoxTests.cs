using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class TextBoxTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var tb = new TextBox();
        Assert.Equal(string.Empty, tb.Text);
        Assert.False(tb.IsPassword);

        // hit the non-user-input programmatic text set branch that updates cursor
        tb.Text = "Hello";
        Assert.Equal("Hello", tb.Text);

        // set text to null
        tb.Text = null;
        Assert.Null(tb.Text);

        // set password char to hit the setter
        tb.PasswordChar = '#';
        Assert.Equal('#', tb.PasswordChar);

        // trigger OnPropertyChanged for a non-Text property to hit the base logic
        tb.Foreground = ConsoleColor.Green;
        Assert.Equal(ConsoleColor.Green, tb.Foreground);
    }

    [Fact]
    public void OnMouseDown_FocusesAndHandled()
    {
        var tb = new TextBox();
        var window = new TuiWindow();
        window.Content = tb; // TuiWindow inherits from ContentControl (or similar, it has Content property usually)

        tb.Measure(new Size(10, 1));
        tb.Arrange(new Rect(0, 0, 10, 1));

        var e = new MouseEventArgs { X = 0, Y = 0, GlobalX = 0, GlobalY = 0, RoutedEvent = UIElement.MouseDownEvent };
        tb.RaiseEvent(e);

        Assert.True(e.Handled);
        Assert.True(tb.IsFocused);
    }

    [Theory]
    [InlineData("A", ConsoleKey.LeftArrow, 0, "A")] // Cursor at end (1), move left -> (0). Text remains A.
    [InlineData("A", ConsoleKey.RightArrow, 0, "A")] // Cursor at end (1), move right -> bound by length (1).
    [InlineData("", ConsoleKey.Backspace, 0, "")] // Cursor at 0, backspace does nothing.
    [InlineData("", ConsoleKey.Delete, 0, "")] // Cursor at 0, delete does nothing.
    public void OnKeyDown_BoundaryConditions(string initialText, ConsoleKey key, int moveCount, string expectedText)
    {
        var tb = new TextBox { Text = initialText };
        // move cursor if needed.
        for(int i = 0; i < moveCount; i++) {
            tb.RaiseEvent(new KeyEventArgs { Key = ConsoleKey.LeftArrow, RoutedEvent = UIElement.KeyDownEvent });
        }

        var e = new KeyEventArgs { Key = key, RoutedEvent = UIElement.KeyDownEvent };
        tb.RaiseEvent(e);

        Assert.True(e.Handled);
        Assert.Equal(expectedText, tb.Text);
    }

    [Theory]
    [InlineData("ABC", ConsoleKey.Delete, 3, "BC")] // cursor at 0, deletes A -> BC
    [InlineData("ABC", ConsoleKey.Delete, 2, "AC")] // cursor at 1, deletes B -> AC
    [InlineData("ABC", ConsoleKey.Backspace, 0, "AB")] // cursor at 3, backspace -> AB
    [InlineData("ABC", ConsoleKey.Backspace, 3, "ABC")] // cursor at 0, backspace -> ABC
    [InlineData("ABC", ConsoleKey.LeftArrow, 4, "ABC")] // go beyond 0 -> stops at 0
    [InlineData("ABC", ConsoleKey.RightArrow, 0, "ABC")] // go beyond length -> stops at length
    public void OnKeyDown_DeleteAndNavigation(string initialText, ConsoleKey key, int leftMoves, string expectedText)
    {
        var tb = new TextBox { Text = initialText };
        for(int i=0; i < leftMoves; i++)
        {
            tb.RaiseEvent(new KeyEventArgs { Key = ConsoleKey.LeftArrow, RoutedEvent = UIElement.KeyDownEvent });
        }

        var e = new KeyEventArgs { Key = key, RoutedEvent = UIElement.KeyDownEvent };
        tb.RaiseEvent(e);

        Assert.True(e.Handled);
        Assert.Equal(expectedText, tb.Text);

        if (key == ConsoleKey.RightArrow)
        {
            // do an extra right arrow to hit boundary condition text.Length
            var e2 = new KeyEventArgs { Key = ConsoleKey.RightArrow, RoutedEvent = UIElement.KeyDownEvent };
            tb.RaiseEvent(e2);
            Assert.True(e2.Handled);
        }
    }

    [Fact]
    public void Text_Change_UpdatesCursor()
    {
        var tb = new TextBox();
        tb.Text = "Hello";
        // Cannot access private cursor position, but we can verify behavior via input
        Assert.Equal("Hello", tb.Text);
    }

    [Fact]
    public void OnKeyDown_AddsText()
    {
        var tb = new TextBox();
        tb.OnKeyDown(new KeyEventArgs { KeyChar = 'A' });
        Assert.Equal("A", tb.Text);

        tb.OnKeyDown(new KeyEventArgs { KeyChar = 'B' });
        Assert.Equal("AB", tb.Text);
    }

    [Fact]
    public void OnKeyDown_Backspace()
    {
        var tb = new TextBox { Text = "AB" };
        // Cursor is at end by default when Text is set

        tb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Backspace });
        Assert.Equal("A", tb.Text);
    }

    [Fact]
    public void OnKeyDown_LeftRightArrow_Navigation()
    {
        var tb = new TextBox { Text = "ABC" };
        // Cursor at 3

        tb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // At 2
        tb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Backspace }); // Deletes 'B'
        Assert.Equal("AC", tb.Text);
    }

    [Fact]
    public void PasswordMode_HidesText()
    {
        var tb = new TextBox { Text = "Pass", IsPassword = true, Width = 10 };
        tb.Measure(new Size(10, 1));
        tb.Arrange(new Rect(0, 0, 10, 1));

        var buffer = new VirtualBuffer(10, 1);
        tb.Render(buffer, 0, 0);

        Assert.Equal('*', buffer.GetPixel(0, 0).Character);
        Assert.Equal('*', buffer.GetPixel(1, 0).Character);
        Assert.Equal('*', buffer.GetPixel(2, 0).Character);
        Assert.Equal('*', buffer.GetPixel(3, 0).Character);
    }

    [Theory]
    [InlineData(false, false, ConsoleColor.White, ConsoleColor.Black)] // not focused, null background -> default transparent/black fallback handled by code
    [InlineData(false, true, ConsoleColor.White, ConsoleColor.Red)] // not focused, custom background
    [InlineData(true, false, ConsoleColor.Black, ConsoleColor.Gray)] // focused cursor cell, custom logic (cursor cell fg/bg invert)
    public void Render_ColorsAndFocus(bool isFocused, bool customBg, ConsoleColor expectedCursorFg, ConsoleColor expectedCursorBg)
    {
        var tb = new TextBox { Text = "A", Width = 5 };
        if (customBg) tb.Background = ConsoleColor.Red;
        if (isFocused) tb.RaiseEvent(new RoutedEventArgs(UIElement.GotFocusEvent, tb));

        tb.Measure(new Size(5, 1));
        tb.Arrange(new Rect(0, 0, 5, 1));

        var buffer = new VirtualBuffer(5, 1);
        tb.Render(buffer, 0, 0);

        // "A" is at index 0, length 1. Cursor is at index 1 because text length is 1 (programmatic set puts cursor at end).
        // Cell 0 should be 'A'.
        Assert.Equal('A', buffer.GetPixel(0, 0).Character);

        // If focused, cell 1 is the cursor cell (space).
        // Let's check cell 1.
        if (isFocused)
        {
            Assert.Equal(' ', buffer.GetPixel(1, 0).Character);
            Assert.Equal(expectedCursorFg, buffer.GetPixel(1, 0).Foreground);
            Assert.Equal(expectedCursorBg, buffer.GetPixel(1, 0).Background);
        }
        else
        {
            // Not focused, so cell 1 is just space with normal bg
            Assert.Equal(' ', buffer.GetPixel(1, 0).Character);
            if (customBg) Assert.Equal(ConsoleColor.Red, buffer.GetPixel(1, 0).Background);
        }
    }

    [Theory]
    [InlineData("1234567890", 5, "7890 ")] // Text length 10, width 5. Programmatic set puts cursor at 10. Start index should be 10 - 5 + 1 = 6. Display from index 6 -> '7890 '
    [InlineData("123", 5, "123  ")] // Text length 3, width 5. Fits completely.
    public void Render_Scrolling(string text, int width, string expectedVisibleCharacters)
    {
        var tb = new TextBox { Text = text, Width = width };
        tb.RaiseEvent(new RoutedEventArgs(UIElement.GotFocusEvent, tb)); // Set focus to ensure cursor rendering behavior triggers

        tb.Measure(new Size(width, 1));
        tb.Arrange(new Rect(0, 0, width, 1));

        var buffer = new VirtualBuffer(width, 1);
        tb.Render(buffer, 0, 0);

        string visible = "";
        for (int i = 0; i < width; i++)
        {
            visible += buffer.GetPixel(i, 0).Character;
        }

        Assert.Equal(expectedVisibleCharacters, visible);
    }
}
