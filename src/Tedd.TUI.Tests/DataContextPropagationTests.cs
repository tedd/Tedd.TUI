using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests
{
    public class DataContextPropagationTests
    {
        [Fact]
        public void PropagationTest()
        {
            var window = new TuiWindow();
            var stackPanel = new StackPanel();
            var textBlock = new TextBlock();

            window.Content = stackPanel;
            stackPanel.AddChild(textBlock);

            var data = "Test Data";
            window.DataContext = data;

            Assert.Equal(data, window.DataContext);
            Assert.Equal(data, stackPanel.DataContext);
            Assert.Equal(data, textBlock.DataContext);
        }

        [Fact]
        public void LocalOverrideTest()
        {
            var window = new TuiWindow();
            var stackPanel = new StackPanel();
            var textBlock = new TextBlock();

            window.Content = stackPanel;
            stackPanel.AddChild(textBlock);

            var overrideData = "Override";
            var rootData = "Root";

            // Set local override
            textBlock.DataContext = overrideData;
            Assert.Equal(overrideData, textBlock.DataContext);

            // Set root data
            window.DataContext = rootData;

            Assert.Equal(rootData, window.DataContext);
            Assert.Equal(rootData, stackPanel.DataContext);

            // This checks if local value is respected.
            Assert.Equal(overrideData, textBlock.DataContext);
        }

        [Fact]
        public void NotificationTest()
        {
            var window = new TuiWindow();
            var stackPanel = new StackPanel();
            var textBlock = new TestTextBlock();

            window.Content = stackPanel;
            stackPanel.AddChild(textBlock);

            var data1 = "Data1";
            var data2 = "Data2";

            window.DataContext = data1;
            Assert.Equal(data1, textBlock.DataContext);
            Assert.True(textBlock.ChangedCount > 0, $"OnDataContextChanged not called. Count={textBlock.ChangedCount}");
            int countAfterFirst = textBlock.ChangedCount;

            window.DataContext = data2;
            Assert.Equal(data2, textBlock.DataContext);
            Assert.True(textBlock.ChangedCount > countAfterFirst, $"OnDataContextChanged not called on update. Count={textBlock.ChangedCount}");
        }

        class TestTextBlock : TextBlock
        {
            public int ChangedCount = 0;
            protected override void OnDataContextChanged(object newValue)
            {
                base.OnDataContextChanged(newValue);
                ChangedCount++;
            }
        }
    }
}
