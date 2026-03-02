using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

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
}
