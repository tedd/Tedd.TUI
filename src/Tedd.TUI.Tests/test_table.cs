using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests
{
    public class TableValidatorTests
    {
        [Fact]
        public void Table_NestedInCanvas_NegativeConstraints_ValidatesDimensionalBehavior()
        {
            var table = new Table
            {
                ShowBorder = true,
                BorderStyle = BoxStyle.Heavy,
                ShowHeader = true,
                ShowVerticalLines = true
            };
            table.Columns.Add(new TableColumn { Header = "Id", Width = new GridLength(5, GridUnitType.Pixel) });
            table.Columns.Add(new TableColumn { Header = "Name", Width = new GridLength(10, GridUnitType.Pixel) });

            table.AddRow("1", "Alice");
            table.AddRow("2", "Bob");

            var canvas = new Canvas();
            canvas.Children.Add(table);
            Canvas.SetLeft(table, 5);
            Canvas.SetTop(table, 5);

            canvas.Measure(new Size(100, 100));
            canvas.Arrange(new Rect(0, 0, 100, 100));

            // force table explicit size so we know the test expectations
            // width requested was enough for columns: Col1=5, Col2=10, plus border spaces = 1+5+1+10+1 = 18 total.
            // If table gets W=18, top right is at x = 5 + 18 - 1 = 22.
            table.Measure(new Size(18, 10));
            table.Arrange(new Rect(5, 5, 18, 10));

            var buffer = new VirtualBuffer(100, 100);
            canvas.Render(buffer, 0, 0);

            // Assert exact boundary characters (Heavy borders)
            Assert.Equal('\u250F', buffer.GetPixel(5, 5).Character); // Top Left
            Assert.Equal('\u2513', buffer.GetPixel(5 + 18 - 1, 5).Character); // Top Right

            // Assert junction characters
            Assert.Equal('\u2533', buffer.GetPixel(5 + 6, 5).Character); // TDown between Id and Name

            // Test resize to negative/zero constraint
            table.Measure(new Size(0, 0));
            table.Arrange(new Rect(0, 0, 0, 0));
            var zeroBuffer = new VirtualBuffer(100, 100);
            table.Render(zeroBuffer, 0, 0);

            // Should not throw and nothing should render
            Assert.Equal(' ', zeroBuffer.GetPixel(0, 0).Character);
        }
    }
}
