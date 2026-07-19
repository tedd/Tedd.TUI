using System;
using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests
{
    public class UniformGridTests
    {
        [Fact]
        public void MouseClick_ButtonsInSeparateCells_HitsOnlyTarget()
        {
            var first = new Button { Content = "First" };
            var second = new Button { Content = "Second" };
            var grid = new UniformGrid
            {
                Rows = 2,
                Columns = 2,
                Width = 20,
                Height = 8
            };
            grid.AddChild(first);
            grid.AddChild(new TextBlock { Text = "empty cell" });
            grid.AddChild(new TextBlock { Text = "surface" });
            grid.AddChild(second);

            var host = new ControlTestHost(new Border { Child = grid }, 24, 12);
            var firstClicks = 0;
            var secondClicks = 0;
            first.Click += (_, _) => firstClicks++;
            second.Click += (_, _) => secondClicks++;

            host.Click(first, 2, 1);
            host.Click(grid, 12, 1);
            host.Click(second, 2, 1);

            Assert.Equal(1, firstClicks);
            Assert.Equal(1, secondClicks);
            Assert.True(second.IsFocused);
        }

        [Theory]
        [InlineData(2, 2, 4)]
        [InlineData(0, 0, 9)] // should default to 3x3
        [InlineData(0, 2, 6)] // rows=0, cols=2 -> should be 3x2
        [InlineData(3, 0, 7)] // rows=3, cols=0 -> should be 3x3
        public void UniformGrid_MeasureOverride_ComputesCorrectGrid(int rows, int cols, int childCount)
        {
            var grid = new UniformGrid();
            grid.Rows = rows;
            grid.Columns = cols;

            for (int i = 0; i < childCount; i++)
            {
                var child = new Border { Width = 10, Height = 5 }; // fixed size
                grid.AddChild(child);
            }

            // Available size is large enough
            grid.Measure(new Size(100, 100));

            // Test computed size.
            int expectedCols = cols;
            int expectedRows = rows;
            if (rows == 0 && cols == 0)
            {
                expectedRows = (int)Math.Ceiling(Math.Sqrt(childCount));
                int diff = expectedRows * expectedRows - childCount;
                if (diff >= expectedRows) expectedCols = expectedRows - 1;
                else expectedCols = expectedRows;
            }
            else if (rows == 0)
            {
                expectedRows = (childCount + cols - 1) / cols;
            }
            else if (cols == 0)
            {
                expectedCols = (childCount + rows - 1) / rows;
            }

            Assert.Equal(expectedCols * 10, grid.DesiredSize.Width);
            Assert.Equal(expectedRows * 5, grid.DesiredSize.Height);
        }

        [Theory]
        [InlineData(1, 4)]
        [InlineData(2, 3)]
        public void UniformGrid_ArrangeOverride_LaysOutChildrenCorrectly(int firstCol, int childCount)
        {
            var grid = new UniformGrid();
            grid.Rows = 2;
            grid.Columns = 2;
            grid.FirstColumn = firstCol;

            for (int i = 0; i < childCount; i++)
            {
                grid.AddChild(new Border());
            }

            grid.Measure(new Size(20, 20)); // Child max desired is 0, so desired is 0
            grid.Arrange(new Rect(0, 0, 20, 20)); // Cell size = 10x10

            // Before arrangement: Measure computes values. FirstCol is clamped.
            // If firstCol >= 2 (the computed columns), the internal logic sets it to 0.
            // But wait! If FirstColumn = 2, it is set to 0.
            int col = (firstCol >= 2) ? 0 : firstCol;
            int row = 0;

            for (int i = 0; i < childCount; i++)
            {
                var child = grid.GetVisualChild(i);
                Assert.Equal(col * 10, child.RenderSize.X);
                // Child RenderSize.Y was failing with expected 0 actual 20?
                // Ah! When childCount = 3, and FirstColumn = 2 (which becomes 0).
                // col starts at 0, row = 0.
                // 1st child: (0,0). col=1, row=0.
                // 2nd child: (10,0). col=2 -> col=0, row=1.
                // 3rd child: (0,10). col=1, row=1.
                Assert.Equal(row * 10, child.RenderSize.Y);

                col++;
                if (col >= 2)
                {
                    col = 0;
                    row++;
                }
            }
        }
    }
}
