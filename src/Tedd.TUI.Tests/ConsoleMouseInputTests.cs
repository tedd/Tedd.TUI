using Xunit;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests
{
    // Exercises ConsoleInputManager's SGR mouse parsing end-to-end against a real
    // window, matching what a terminal sends under tracking modes 1002/1006.
    public class ConsoleMouseInputTests
    {
        private static (ControlTestHost Host, ScrollBar ScrollBar) CreateScrollBarHost()
        {
            var scrollBar = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Height = 12,
                Width = 1,
                ViewportSize = 1
            };
            var host = new ControlTestHost(new Border { Child = scrollBar, Padding = new Thickness(0) }, 5, 14);
            return (host, scrollBar);
        }

        [Theory]
        [InlineData(32)] // plain left-button drag (mode 1002)
        [InlineData(48)] // ctrl + left-button drag (16 modifier + 32 motion)
        public void SgrDragMotion_UpdatesScrollBarDuringDrag_NotOnlyAtRelease(int dragButtonCode)
        {
            var (host, scrollBar) = CreateScrollBarHost();
            var input = new ConsoleInputManager(host.Window);
            try
            {
                // Thumb sits at local Y=1 when Value=0; SGR coordinates are 1-based.
                var thumb = scrollBar.PointToScreen(new Point(0, 1));
                int col = thumb.X + 1;
                int row = thumb.Y + 1;

                input.ParseMouseSGR($"[<0;{col};{row}M"); // press on thumb
                Assert.Same(scrollBar, host.Window.CapturedElement);
                Assert.Equal(0, scrollBar.Value);

                // Motion reports while the button is held must scroll immediately.
                // Inner track 10, thumb 1, slide 9: 3 cells -> round(3 * 100 / 9) = 33.
                input.ParseMouseSGR($"[<{dragButtonCode};{col};{row + 3}M");
                Assert.Equal(33, scrollBar.Value);

                input.ParseMouseSGR($"[<0;{col};{row + 3}m"); // release
                Assert.Null(host.Window.CapturedElement);
                Assert.Equal(33, scrollBar.Value);
            }
            finally
            {
                input.Stop();
            }
        }

        [Fact]
        public void SgrWheel_StillScrollsAfterMotionParsing()
        {
            var (host, scrollBar) = CreateScrollBarHost();
            var input = new ConsoleInputManager(host.Window);
            try
            {
                scrollBar.Value = 50;
                var pos = scrollBar.PointToScreen(new Point(0, 5));

                input.ParseMouseSGR($"[<65;{pos.X + 1};{pos.Y + 1}M"); // wheel down
                Assert.Equal(50 + scrollBar.SmallChange * ScrollViewer.WheelScrollLines, scrollBar.Value);
            }
            finally
            {
                input.Stop();
            }
        }
    }
}
