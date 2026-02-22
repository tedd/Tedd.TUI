using System;
using Xunit;

namespace Tedd.TUI.Tests
{
    public class ControlPropertyTests
    {
        [Fact]
        public void Button_Defaults()
        {
            var btn = new Button();
            Assert.Equal(ConsoleColor.White, btn.Foreground);
            Assert.Equal(ConsoleColor.Gray, btn.BorderColor);
            Assert.Equal(ConsoleColor.Yellow, btn.FocusedForeground);
            Assert.Equal(ConsoleColor.Yellow, btn.FocusedBorderColor);
        }

        [Fact]
        public void Button_SetProperties()
        {
            var btn = new Button();
            btn.Foreground = ConsoleColor.Red;
            btn.BorderColor = ConsoleColor.Blue;
            btn.FocusedForeground = ConsoleColor.Green;
            btn.FocusedBorderColor = ConsoleColor.Cyan;

            Assert.Equal(ConsoleColor.Red, btn.Foreground);
            Assert.Equal(ConsoleColor.Blue, btn.BorderColor);
            Assert.Equal(ConsoleColor.Green, btn.FocusedForeground);
            Assert.Equal(ConsoleColor.Cyan, btn.FocusedBorderColor);
        }

        [Fact]
        public void CheckBox_Defaults()
        {
            var cb = new CheckBox();
            Assert.Equal(ConsoleColor.White, cb.Foreground);
            Assert.Equal(ConsoleColor.Yellow, cb.FocusedForeground);
            Assert.Equal(ConsoleColor.Green, cb.CheckColor);
            Assert.Equal(ConsoleColor.Gray, cb.BracketColor);
            Assert.Equal('√', cb.CheckedChar);
            Assert.Equal(' ', cb.UncheckedChar);
        }

        [Fact]
        public void CheckBox_SetProperties()
        {
            var cb = new CheckBox();
            cb.Foreground = ConsoleColor.Red;
            cb.CheckColor = ConsoleColor.Blue;
            cb.CheckedChar = 'X';

            Assert.Equal(ConsoleColor.Red, cb.Foreground);
            Assert.Equal(ConsoleColor.Blue, cb.CheckColor);
            Assert.Equal('X', cb.CheckedChar);
        }

        [Fact]
        public void RadioButton_Defaults()
        {
            var rb = new RadioButton();
            Assert.Equal(ConsoleColor.White, rb.Foreground);
            Assert.Equal(ConsoleColor.Yellow, rb.FocusedForeground);
            Assert.Equal(ConsoleColor.Green, rb.CheckColor);
            Assert.Equal(ConsoleColor.Gray, rb.BracketColor);
            Assert.Equal('o', rb.CheckedChar);
            Assert.Equal(' ', rb.UncheckedChar);
        }

        [Fact]
        public void ComboBox_Defaults()
        {
            var cb = new ComboBox();
            Assert.Equal(ConsoleColor.White, cb.Foreground);
            Assert.Equal(ConsoleColor.Yellow, cb.FocusedForeground);
            Assert.Equal(ConsoleColor.DarkGray, cb.FocusedTextBackgroundColor);
            Assert.Equal(ConsoleColor.Black, cb.ArrowColor);
            Assert.Equal(ConsoleColor.Gray, cb.ArrowBackgroundColor);
            Assert.Equal(ConsoleColor.Yellow, cb.FocusedArrowColor);
            Assert.Equal(ConsoleColor.DarkGray, cb.FocusedArrowBackgroundColor);
            Assert.Equal(ConsoleColor.Black, cb.PopupBackground);
            Assert.Equal(ConsoleColor.White, cb.PopupBorderColor);
        }

        [Fact]
        public void ListBox_Defaults()
        {
            var lb = new ListBox();
            Assert.Equal(ConsoleColor.Gray, lb.Foreground);
            Assert.Equal(ConsoleColor.Black, lb.SelectionForeground);
            Assert.Equal(ConsoleColor.White, lb.SelectionBackground);
            Assert.Equal(ConsoleColor.White, lb.FocusedSelectionForeground);
            Assert.Equal(ConsoleColor.Blue, lb.FocusedSelectionBackground);
        }
    }
}
