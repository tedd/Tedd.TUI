using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests
{
    public class VisualTreeHelperTests
    {
        [Fact]
        public void GetChildrenCount_NullReference_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => VisualTreeHelper.GetChildrenCount(null!));
        }

        [Fact]
        public void GetChild_NullReference_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => VisualTreeHelper.GetChild(null!, 0));
        }

        [Fact]
        public void GetParent_NullReference_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => VisualTreeHelper.GetParent(null!));
        }

        [Fact]
        public void GetChildrenCount_ReturnsVisualChildrenCount()
        {
            var panel = new StackPanel();
            panel.Children.Add(new Button());
            panel.Children.Add(new TextBlock());

            Assert.Equal(2, VisualTreeHelper.GetChildrenCount(panel));
        }

        [Fact]
        public void GetChild_ReturnsVisualChild()
        {
            var panel = new StackPanel();
            var button = new Button();
            var textBlock = new TextBlock();
            panel.Children.Add(button);
            panel.Children.Add(textBlock);

            Assert.Same(button, VisualTreeHelper.GetChild(panel, 0));
            Assert.Same(textBlock, VisualTreeHelper.GetChild(panel, 1));
        }

        [Fact]
        public void GetParent_ReturnsParent()
        {
            var panel = new StackPanel();
            var button = new Button();
            panel.Children.Add(button);

            Assert.Same(panel, VisualTreeHelper.GetParent(button));
        }
    }
}
