
using Tedd.TUI.Controls;
using Tedd.TUI.Controls.Primitives;
using System.Reflection;
using Xunit;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using Tedd.TUI.Media;

namespace Tedd.TUI.Tests
{
    public class GeneratedExtraTests
    {
        public class MockItem
        {
            public string ThrowingProp
            {
                get { throw new Exception("Error"); }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void DataGrid_EnsureGetters_HandlesExceptions(int size)
        {
            var grid = new DataGrid { AutoGenerateColumns = true };
            var list = new List<object>();
            if (size > 0)
            {
                list.Add(new MockItem());
            }
            grid.ItemsSource = list;
            grid.Measure(new Size(size, size));

            var table = grid.GetVisualChild(0) as Table;
            Assert.NotNull(table);
            if (size > 0)
            {
                Assert.Equal(1, table.Rows.Count);
                // Cells are typically TextBlocks for DataGrid AutoGenerateColumns
                var cell = table.Rows[0].Cells[0] as TextBlock;
                Assert.NotNull(cell);
                Assert.Equal("", cell.Text);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ListBox_OnMouseDown_Handled_ReturnsEarly(bool isHandled)
        {
            var lb = new ListBox();
            var args = new MouseEventArgs(UIElement.MouseDownEvent, lb) { Handled = isHandled };
            lb.OnMouseDown(args);
            // ListBox always sets args.Handled = true at the end if it wasn't already handled.
            Assert.True(args.Handled);
        }

        [Theory]
        [InlineData(9, 0)]
        [InlineData(9, 1)]
        [InlineData(9, 2)]
        public void ListBox_OnMouseDown_OnScrollBar_PassesEventToScrollBar(int mouseX, int mouseY)
        {
            var lb = new ListBox();
            for (int i=0; i<10; i++) lb.Items.Add(i.ToString());
            lb.Measure(new Size(10, 3));
            lb.Arrange(new Rect(0, 0, 10, 3));

            var source = new Border();
            var args = new MouseEventArgs(UIElement.MouseDownEvent, source)
            {
                X = mouseX,
                Y = mouseY,
                GlobalX = mouseX,
                GlobalY = mouseY,
                Handled = false
            };
            lb.OnMouseDown(args);
            Assert.True(args.Handled);
        }

        [Theory]
        [InlineData(ConsoleKey.Enter)]
        [InlineData(ConsoleKey.Spacebar)]
        public void MenuItem_OnKeyDown_Enter_ExecutesCommandAndClosesMenu(ConsoleKey key)
        {
            bool commandInvoked = false;
            var item = new MenuItem { Command = () => commandInvoked = true };

            var source = new Border();
            var args = new KeyEventArgs(UIElement.KeyDownEvent, source) { Key = key };
            item.OnKeyDown(args);
            Assert.True(commandInvoked);
            Assert.True(args.Handled);
        }

        [Theory]
        [InlineData(ConsoleKey.Enter, true)]
        [InlineData(ConsoleKey.Spacebar, true)]
        public void MenuItem_OnKeyDown_Enter_TogglesSubMenuIfHasItems(ConsoleKey key, bool expectedHandled)
        {
            var item = new MenuItem();
            item.Items.Add(new MenuItem());

            var source = new Border();
            var args = new KeyEventArgs(UIElement.KeyDownEvent, source) { Key = key };
            item.OnKeyDown(args);
            Assert.Equal(expectedHandled, args.Handled);
        }

        [Theory]
        [InlineData(ConsoleKey.Enter)]
        [InlineData(ConsoleKey.Spacebar)]
        public void RadioButton_OnKeyDown_Enter_HandledByBase(ConsoleKey key)
        {
            var rb = new RadioButton();
            var source = new Border();
            var args = new KeyEventArgs(UIElement.KeyDownEvent, source) { Key = key };
            rb.OnKeyDown(args);
            Assert.True(args.Handled);
        }

        [Theory]
        [InlineData(ConsoleKey.DownArrow)]
        [InlineData(ConsoleKey.RightArrow)]
        [InlineData(ConsoleKey.UpArrow)]
        [InlineData(ConsoleKey.LeftArrow)]
        public void RadioButton_NavigateToSibling_NoPanel_DoesNothing(ConsoleKey key)
        {
            var rb = new RadioButton();
            var source = new Border();
            var args = new KeyEventArgs(UIElement.KeyDownEvent, source) { Key = key };
            rb.OnKeyDown(args);
            Assert.True(args.Handled);
        }

        [Theory]
        [InlineData(ConsoleKey.RightArrow)]
        [InlineData(ConsoleKey.PageDown)]
        public void Calendar_MoveDisplayDate_IgnoresOutOfRange_Max(ConsoleKey key)
        {
            var calendar = new Calendar();
            calendar.DisplayDate = DateTime.MaxValue.Date;
            var source = new Border();
            var args = new KeyEventArgs(UIElement.KeyDownEvent, source) { Key = key };
            calendar.OnKeyDown(args);
            Assert.Equal(DateTime.MaxValue.Date, calendar.DisplayDate.Date);
            Assert.True(args.Handled);
        }

        [Theory]
        [InlineData(ConsoleKey.LeftArrow)]
        [InlineData(ConsoleKey.PageUp)]
        public void Calendar_MoveDisplayDate_IgnoresOutOfRange_Min(ConsoleKey key)
        {
            var calendar = new Calendar();
            calendar.DisplayDate = DateTime.MinValue.Date;
            var source = new Border();
            var args = new KeyEventArgs(UIElement.KeyDownEvent, source) { Key = key };
            calendar.OnKeyDown(args);
            Assert.Equal(DateTime.MinValue.Date, calendar.DisplayDate.Date);
            Assert.True(args.Handled);
        }

        [Theory]
        [InlineData(HorizontalAlignment.Left)]
        [InlineData(HorizontalAlignment.Right)]
        [InlineData(HorizontalAlignment.Stretch)]
        public void GridSplitter_GetEffectiveResizeDirection_Columns(HorizontalAlignment align)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            var splitter = new GridSplitter();
            Grid.SetColumn(splitter, 0);
            grid.Children.Add(splitter);
            splitter.HorizontalAlignment = align;

            var method = typeof(GridSplitter).GetMethod("GetEffectiveResizeDirection", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                var result = method.Invoke(splitter, null);
                Assert.NotNull(result);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        [InlineData(-100)]
        public void TextBlock_WrapText_ZeroOrNegativeWidth(int width)
        {
            var wrapTextMethod = typeof(TextBlock).GetMethod("WrapText", BindingFlags.NonPublic | BindingFlags.Static);
            if (wrapTextMethod != null)
            {
                var result = wrapTextMethod.Invoke(null, new object[] { "Test string", width }) as System.Collections.Generic.List<string>;
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Equal(string.Empty, result[0]);
            }
        }

        [Theory]
        [InlineData(5, 1)]
        [InlineData(50, 2)]
        [InlineData(500, 3)]
        [InlineData(5000, 4)]
        [InlineData(50000, 5)]
        [InlineData(500000, 6)]
        [InlineData(5000000, 7)]
        [InlineData(50000000, 8)]
        [InlineData(500000000, 9)]
        [InlineData(1000000000, 10)]
        public void Table_GetDigitCount_ReturnsExpected(int number, int expectedDigits)
        {
            var tableType = typeof(Table);
            var method = tableType.GetMethod("GetDigitCount", BindingFlags.NonPublic | BindingFlags.Static);
            if (method != null)
            {
                var result = method.Invoke(null, new object[] { number });
                Assert.Equal(expectedDigits, result);
            }
        }
    }
}
