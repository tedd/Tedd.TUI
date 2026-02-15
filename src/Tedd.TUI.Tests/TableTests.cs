using System;
using System.Linq;
using Xunit;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.Tests
{
    public class TableTests
    {
        [Fact]
        public void Table_Default_Style_Is_Heavy()
        {
            var table = new Table();
            Assert.Equal(BoxStyle.Heavy, table.BorderStyle);
            Assert.False(table.ShowBorder); // Default is false unless Markdown sets it? No, in Table.cs ShowBorder=false default.
            // MarkdownTheme default is ShowBorder=true.
            // Let's check Table defaults.
            // In Table.cs: public bool ShowBorder { get; set; } = false;
        }

        [Fact]
        public void Table_Respects_ShowBorder_Property()
        {
            var table = new Table();
            table.ShowBorder = false;
            table.Columns.Add(new TableColumn { Header = "H", Width = GridLength.Pixel(1) });
            table.AddRow("R");

            // Measure/Arrange
            table.Measure(new Size(10, 5));
            table.Arrange(new Rect(0, 0, 10, 5));

            var buffer = new VirtualBuffer(10, 5);
            table.Render(buffer, 0, 0);

            // Without border, top-left (0,0) should be header text or space, not a corner char.
            // With ShowHeader=true default.
            // Header is at (0,0) if no border.
            // If Border=true, Header is at (1,1).

            var c = buffer.GetPixel(0, 0).Character;
            // Should be 'H' (if width fits) or ' ' background.
            // BoxDrawing Heavy TopLeft is U+250F (┏).
            Assert.NotEqual('\u250F', c);

            // Enable border
            table.ShowBorder = true;
            table.BorderStyle = BoxStyle.Heavy;
            table.Invalidate();
            // Re-render (VirtualBuffer clear first)
            buffer.Clear();
            table.Measure(new Size(10, 5)); // Re-measure needed for padding change
            table.Arrange(new Rect(0, 0, 10, 5));
            table.Render(buffer, 0, 0);

            c = buffer.GetPixel(0, 0).Character;
            Assert.Equal('\u250F', c);
        }

        [Fact]
        public void Table_Respects_ShowVerticalLines_Property()
        {
            var table = new Table();
            table.ShowBorder = true;
            table.ShowVerticalLines = true;
            table.BorderStyle = BoxStyle.Heavy;
            table.Columns.Add(new TableColumn { Header = "A", Width = GridLength.Pixel(1) });
            table.Columns.Add(new TableColumn { Header = "B", Width = GridLength.Pixel(1) });
            table.AddRow("1", "2");

            table.Measure(new Size(10, 5));
            table.Arrange(new Rect(0, 0, 10, 5));
            var buffer = new VirtualBuffer(10, 5);
            table.Render(buffer, 0, 0);

            // Header separator between A and B
            // A is width 1. B is width 1.
            // Border at x=0. A at x=1. Sep at x=2.

            var c = buffer.GetPixel(2, 1).Character; // Header row y=1 (Border y=0)
            // Should be vertical line. Heavy style uses Light Vertical (\u2502) for inner?
            // Wait, implementation uses `chars.HeaderInnerV`.
            // In Table.cs: c.HeaderInnerV = b.Vertical (which is Heavy Vertical \u2503 for Heavy style?)
            // Let's check BoxDrawingChars.Get(BoxStyle.Heavy).
            // Heavy: Vertical = \u2503.
            // So Header Inner V is \u2503.

            Assert.Equal('\u2503', c);

            // Disable Vertical Lines
            table.ShowVerticalLines = false;
            buffer.Clear();
            table.Render(buffer, 0, 0);
            c = buffer.GetPixel(2, 1).Character;
            Assert.Equal(' ', c); // Should be space (padding) or background
        }

        [Fact]
        public void Table_Renders_Mixed_Style_Separators()
        {
            // Verify that for Heavy style, we use Mixed junctions (Heavy Outer, Light Inner) where implemented.
            // Table.cs:
            // case BoxStyle.Heavy:
            // c.TDown = '\u2533'; (Heavy Down, Heavy Horizontal) -> Actually standard Heavy T Down is ┳ (\u2533).
            // c.TUp = '\u2537';   // ┷ (Heavy Horz, Light Up)? No.
            // \u2537 is ┷ (Heavy Up, Heavy Horizontal).
            // Wait, looking at Table.cs code I wrote:
            /*
                 case BoxStyle.Heavy:
                     c.TDown = '\u2533';
                     c.TUp = '\u2537';
                     c.TLeft = '\u2523';
                     c.TRight = '\u252B';
                     c.HeaderCross = '\u254B';
                     c.BodySepTLeft = '\u2520';
                     c.BodySepTRight = '\u2528';
            */
            // \u2533 is ┳ (Heavy Down/Horz).
            // \u2537 is ┷ (Heavy Up/Horz).
            // \u2523 is ┣ (Heavy Vert/Right).
            // \u252B is ┫ (Heavy Vert/Left).
            // \u254B is ╋ (Heavy Cross).

            // Wait, did I implement mixed style?
            // "If outer border is there, it needs to match whatever inner border there is correct characters when lines intercept. This means header will have thicker lines around it and separating it."
            // "Vertical lines between each colum, yes,"
            // "Horizontal lines should optionally be between every row, but under every row is a thinner version than the header"

            // So Header/Border = Heavy.
            // Inner Body Lines = Light.
            // Junctions must mix Heavy and Light.

            // Let's check Body Separator logic in Table.cs:
            // if (child is TableSeparator ...)
            // buffer.SetPixel(x, screenY, chars.BodySepTLeft, ...);
            // chars.BodySepTLeft for Heavy: '\u2520' (┠ - Heavy Vert, Light Right).
            // Correct.

            var table = new Table();
            table.ShowBorder = true;
            table.ShowHorizontalLines = true;
            table.BorderStyle = BoxStyle.Heavy;
            table.Columns.Add(new TableColumn { Header = "A", Width = GridLength.Pixel(1) });
            table.AddRow("1");
            table.AddRow("2"); // Separator between 1 and 2

            table.Measure(new Size(10, 10));
            table.Arrange(new Rect(0, 0, 10, 10));
            var buffer = new VirtualBuffer(10, 10);
            table.Render(buffer, 0, 0);

            // Header is y=1. Sep line is y=2.
            // Row 1 (R1) is y=3 (Height 1).
            // Separator between R1 and R2 is y=4.

            // Left Border at x=0.
            // Junction at (0, 4) should be ┠ (\u2520).

            var c = buffer.GetPixel(0, 4).Character;
            Assert.Equal('\u2520', c);
        }

        [Fact]
        public void MarkdownParser_Applies_Table_Style()
        {
            var theme = new MarkdownTheme();
            theme.Table.ShowBorder = true;
            theme.Table.ShowVerticalLines = false;
            theme.Table.ShowHorizontalLines = true;
            theme.Table.BorderStyle = BoxStyle.Double;

            var parser = new MarkdownParser(theme);
            string md = "| H |\n|---|\n| C |";
            var doc = parser.Parse(md);

            var table = doc.GetVisualChild(0) as Table;
            Assert.NotNull(table);
            Assert.True(table.ShowBorder);
            Assert.False(table.ShowVerticalLines);
            Assert.True(table.ShowHorizontalLines);
            Assert.Equal(BoxStyle.Double, table.BorderStyle);
        }
    }
}
