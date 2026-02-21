using Xunit;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Tests
{
    public class VirtualBufferTests
    {
        [Theory]
        [InlineData(0, 0, 'A', true)] // Valid
        [InlineData(-1, 0, 'B', false)] // Invalid X
        [InlineData(0, -1, 'B', false)] // Invalid Y
        [InlineData(10, 0, 'B', false)] // Max X (Width=10, 0..9)
        [InlineData(0, 10, 'B', false)] // Max Y (Height=10, 0..9)
        public void TestSetPixel_Bounds(int x, int y, char c, bool shouldSet)
        {
            var buffer = new VirtualBuffer(10, 10);

            // Set pixel
            buffer.SetPixel(x, y, c, ConsoleColor.White, ConsoleColor.Black);

            // If it should set, verify it did.
            if (shouldSet)
            {
                Assert.Equal(c, buffer.GetPixel(x, y).Character);
            }
            else
            {
                // If invalid, checking if it was set is tricky because GetPixel also returns default for OOB.
                // We mainly check that it didn't throw and maybe check a valid neighbor didn't change?
                // But the previous test logic was checking edges.

                // Let's just rely on no exception being thrown as the primary check for bounds safety,
                // and if x,y are within bounds (but rejected by logic? No, bounds logic is the only thing).

                // Actually, if it's OOB, GetPixel returns default ' '.
                // So we can assert GetPixel(x,y) returns ' ' if OOB?
                // Wait, GetPixel also has bounds check.

                var pixel = buffer.GetPixel(x, y);
                // If we set 'B' but it was invalid, we expect default ' '.
                Assert.NotEqual(c, pixel.Character);
                Assert.Equal(' ', pixel.Character);
            }
        }

        [Fact]
        public void TestClipping()
        {
            var buffer = new VirtualBuffer(20, 20);

            // Push clip 5,5 size 10x10. (Region 5,5 to 15,15)
            buffer.PushClip(new Rect(5, 5, 10, 10));

            // Inside clip
            buffer.SetPixel(5, 5, 'I', ConsoleColor.White, ConsoleColor.Black);
            Assert.Equal('I', buffer.GetPixel(5, 5).Character);

            // Outside clip
            buffer.SetPixel(4, 5, 'O', ConsoleColor.White, ConsoleColor.Black);
            Assert.Equal(' ', buffer.GetPixel(4, 5).Character); // Should be empty

            buffer.SetPixel(15, 15, 'O', ConsoleColor.White, ConsoleColor.Black); // 15 is limit (start 5 + width 10 = 15). So index 15 is outside. Indices are 5..14.
            Assert.Equal(' ', buffer.GetPixel(15, 15).Character);

            buffer.SetPixel(14, 14, 'I', ConsoleColor.White, ConsoleColor.Black);
            Assert.Equal('I', buffer.GetPixel(14, 14).Character);

            // Nested Clip
            // Current Clip: 5,5 10x10.
            // New Clip: 6,6 2x2. (Region 6,6 to 8,8)
            buffer.PushClip(new Rect(6, 6, 2, 2));

            buffer.SetPixel(5, 5, 'X', ConsoleColor.White, ConsoleColor.Black); // Was valid, now invalid
            Assert.Equal('I', buffer.GetPixel(5, 5).Character); // Should remain 'I' from before, not 'X'

            buffer.SetPixel(6, 6, 'N', ConsoleColor.White, ConsoleColor.Black);
            Assert.Equal('N', buffer.GetPixel(6, 6).Character);

            buffer.PopClip();

            // Should be back to 5,5 10x10
            buffer.SetPixel(5, 5, 'Y', ConsoleColor.White, ConsoleColor.Black);
            Assert.Equal('Y', buffer.GetPixel(5, 5).Character);

            buffer.PopClip();
            // No clip (full buffer)
            buffer.SetPixel(0, 0, 'Z', ConsoleColor.White, ConsoleColor.Black);
            Assert.Equal('Z', buffer.GetPixel(0, 0).Character);
        }

        [Fact]
        public void TestClear()
        {
            var buffer = new VirtualBuffer(10, 10);
            buffer.SetPixel(5, 5, 'A', ConsoleColor.Red, ConsoleColor.Blue);

            // Verify set
            var cell = buffer.GetPixel(5, 5);
            Assert.Equal('A', cell.Character);
            Assert.Equal(ConsoleColor.Red, cell.Foreground);
            Assert.Equal(ConsoleColor.Blue, cell.Background);

            buffer.Clear();

            // Verify cleared
            cell = buffer.GetPixel(5, 5);
            Assert.Equal(' ', cell.Character);
            Assert.Equal(ConsoleColor.White, cell.Foreground);
            Assert.Equal(ConsoleColor.Black, cell.Background);

            // Verify clip stack is cleared?
            // Push clip, clear, try set outside clip.
            buffer.PushClip(new Rect(0,0, 1,1));
            buffer.Clear(); // Should reset clip stack

            buffer.SetPixel(5, 5, 'B', ConsoleColor.White, ConsoleColor.Black);
            Assert.Equal('B', buffer.GetPixel(5, 5).Character);
        }

        [Fact]
        public void TestGetPixel_Bounds()
        {
            var buffer = new VirtualBuffer(10, 10);

            // Out of bounds get should return default empty cell
            var cell = buffer.GetPixel(-1, 0);
            Assert.Equal(' ', cell.Character);

            cell = buffer.GetPixel(100, 0);
            Assert.Equal(' ', cell.Character);
        }
    }
}
