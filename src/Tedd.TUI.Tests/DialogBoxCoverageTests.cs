using System;
using System.Collections.Generic;
using Xunit;

namespace Tedd.TUI.Tests;

public class DialogBoxCoverageTests
{
    private class TestElement : UIElement
    {
        public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
        {
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // By default, UIElement return an empty size.
            // In tests where we check if available space gets propagated, we'll return it so the DesiredSize reflects the max allowed.
            return availableSize;
        }
    }

    [Fact]
    public void DialogBox_Properties_DependencyPropertySettersAndGetters()
    {
        var dialog = new DialogBox();

        // Title
        dialog.Title = "Test Title";
        Assert.Equal("Test Title", dialog.Title);
        Assert.Equal("Test Title", dialog.GetValue(DialogBox.TitleProperty));

        // BorderColor
        dialog.BorderColor = ConsoleColor.Red;
        Assert.Equal(TuiColor.Red, dialog.BorderColor);
        Assert.Equal(TuiColor.Red, dialog.GetValue(DialogBox.BorderColorProperty));

        // TitleColor
        dialog.TitleColor = ConsoleColor.Green;
        Assert.Equal(TuiColor.Green, dialog.TitleColor);
        Assert.Equal(TuiColor.Green, dialog.GetValue(DialogBox.TitleColorProperty));

        // BackgroundColor
        dialog.BackgroundColor = ConsoleColor.Blue;
        Assert.Equal(TuiColor.Blue, dialog.BackgroundColor);
        Assert.Equal(TuiColor.Blue, dialog.GetValue(DialogBox.BackgroundColorProperty));

        // BoxStyle
        dialog.BoxStyle = BoxStyle.Single;
        Assert.Equal(BoxStyle.Single, dialog.BoxStyle);
        Assert.Equal(BoxStyle.Single, dialog.GetValue(DialogBox.BoxStyleProperty));
    }

    [Fact]
    public void Content_Set_UpdatesParentAndDataContext()
    {
        var dialog = new DialogBox();
        var dataContext = new object();
        dialog.DataContext = dataContext;

        var content = new TestElement();
        dialog.Content = content;

        Assert.Equal(dialog, content.Parent);
        Assert.Equal(dataContext, content.DataContext);
    }

    [Fact]
    public void DataContextChanged_UpdatesContentDataContext()
    {
        var dialog = new DialogBox();
        var content = new TestElement();
        dialog.Content = content;

        var newContext = new object();
        dialog.DataContext = newContext;

        Assert.Equal(newContext, content.DataContext);
    }

    [Fact]
    public void VisualChildren_WithoutContent()
    {
        var dialog = new DialogBox();

        Assert.Equal(0, dialog.VisualChildrenCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => dialog.GetVisualChild(0));
    }

    [Fact]
    public void VisualChildren_WithContent()
    {
        var dialog = new DialogBox();
        var content = new TestElement();
        dialog.Content = content;

        Assert.Equal(1, dialog.VisualChildrenCount);
        Assert.Equal(content, dialog.GetVisualChild(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => dialog.GetVisualChild(1));
    }

    [Fact]
    public void MeasureOverride_NoContent_ExplicitDimensions_ReturnsExplicit()
    {
        var dialog = new DialogBox();
        dialog.Width = 100;
        dialog.Height = 50;

        dialog.Measure(new Size(200, 200));

        Assert.Equal(100, dialog.DesiredSize.Width);
        Assert.Equal(50, dialog.DesiredSize.Height);
    }

    [Fact]
    public void MeasureOverride_NoContent_NoExplicitDimensions_ReturnsDefaults()
    {
        var dialog = new DialogBox();

        dialog.Measure(new Size(200, 200));

        // Default Width 40, Height 10
        Assert.Equal(40, dialog.DesiredSize.Width);
        Assert.Equal(10, dialog.DesiredSize.Height);
    }

    [Fact]
    public void MeasureOverride_WithContent_CalculatesDynamicDimensions()
    {
        var dialog = new DialogBox();
        dialog.Title = "TestTitle"; // Length 9

        var content = new TestElement();
        content.Width = 20;
        content.Height = 10;
        dialog.Content = content;

        dialog.Measure(new Size(100, 100));

        // content size 20 + 2 (border) = 22. Title "TestTitle" + 4 = 13. Max is 22.
        Assert.Equal(22, dialog.DesiredSize.Width);
        // content height 10 + 2 (border) = 12
        Assert.Equal(12, dialog.DesiredSize.Height);
    }

    [Fact]
    public void MeasureOverride_WithContent_LongTitle_CalculatesDynamicDimensions()
    {
        var dialog = new DialogBox();
        dialog.Title = "ThisIsAVeryLongTitle"; // Length 20

        var content = new TestElement();
        content.Width = 5;
        content.Height = 5;
        dialog.Content = content;

        dialog.Measure(new Size(100, 100));

        // content width 5 + 2 (border) = 7. Title "ThisIsAVeryLongTitle" + 4 = 24. Max is 24.
        Assert.Equal(24, dialog.DesiredSize.Width);
        Assert.Equal(7, dialog.DesiredSize.Height);
    }

    [Fact]
    public void MeasureOverride_WithContent_ExplicitDimensions()
    {
        var dialog = new DialogBox();
        dialog.Width = 50;
        dialog.Height = 50;

        var content = new TestElement();
        dialog.Content = content;

        dialog.Measure(new Size(100, 100));

        // It should use explicit dimensions
        Assert.Equal(50, dialog.DesiredSize.Width);
        Assert.Equal(50, dialog.DesiredSize.Height);

        // Ensure content gets measured with correct available space
        // With TestElement that has no intrinsic size, it accepts the constraint size minus borders
        Assert.Equal(48, content.DesiredSize.Width);
        Assert.Equal(48, content.DesiredSize.Height);
    }

    [Fact]
    public void ArrangeOverride_WithContent_PositionsContentCorrectly()
    {
        var dialog = new DialogBox();
        dialog.Width = 50;
        dialog.Height = 30;

        var content = new TestElement();
        dialog.Content = content;

        dialog.Measure(new Size(100, 100));
        dialog.Arrange(new Rect(0, 0, 50, 30));

        // Inside the border, X=1, Y=1, W=final.Width-2, H=final.Height-2
        Assert.Equal(1, content.RenderSize.X);
        Assert.Equal(1, content.RenderSize.Y);
        Assert.Equal(48, content.RenderSize.Width);
        Assert.Equal(28, content.RenderSize.Height);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(10, 1)]
    [InlineData(0, 0)]
    public void Render_TooSmall_ReturnsEarly(int width, int height)
    {
        var dialog = new DialogBox();
        dialog.Width = width;
        dialog.Height = height;

        dialog.Measure(new Size(100, 100));
        dialog.Arrange(new Rect(0, 0, width, height));

        var buffer = new VirtualBuffer(10, 10);
        // It shouldn't crash or write out of bounds, and should not modify the buffer
        dialog.Render(buffer, 0, 0);

        // Check buffer is unchanged (empty)
        Assert.Equal(' ', buffer.GetPixel(0, 0).Character);
    }

    [Fact]
    public void Render_NotVisible_ReturnsEarly()
    {
        var dialog = new DialogBox();
        dialog.Width = 10;
        dialog.Height = 10;
        dialog.Visibility = false;

        dialog.Measure(new Size(100, 100));
        dialog.Arrange(new Rect(0, 0, 10, 10));

        var buffer = new VirtualBuffer(10, 10);
        dialog.Render(buffer, 0, 0);

        // Check buffer is unchanged (empty)
        Assert.Equal(' ', buffer.GetPixel(0, 0).Character);
    }

    [Theory]
    [InlineData("Short", 20, " Short ")]
    [InlineData("LongTitleThatWillBeTruncated", 20, " LongTitleThatWil ")]
    [InlineData(null, 20, "")]
    [InlineData("", 20, "")]
    public void Render_Titles_CalculatesProperly(string? title, int width, string expectedFragment)
    {
        var dialog = new DialogBox();
        dialog.Title = title!;
        dialog.Width = width;
        dialog.Height = 10;

        dialog.Measure(new Size(100, 100));
        dialog.Arrange(new Rect(0, 0, width, 10));

        var buffer = new VirtualBuffer(width, 10);
        dialog.Render(buffer, 0, 0);

        // Extract the top border string to see if the title is correctly drawn
        var topBorder = new char[width];
        for (int i = 0; i < width; i++)
        {
            topBorder[i] = buffer.GetPixel(i, 0).Character;
        }
        var topBorderStr = new string(topBorder);

        if (!string.IsNullOrEmpty(expectedFragment))
        {
            Assert.Contains(expectedFragment, topBorderStr);
        }
    }

    [Fact]
    public void Render_WithContent_CallsContentRender()
    {
        var dialog = new DialogBox();
        dialog.Width = 10;
        dialog.Height = 10;

        var content = new MockContentElement();
        dialog.Content = content;

        dialog.Measure(new Size(100, 100));
        dialog.Arrange(new Rect(0, 0, 10, 10));

        var buffer = new VirtualBuffer(10, 10);
        dialog.Render(buffer, 0, 0);

        Assert.True(content.RenderCalled);
    }

    private class MockContentElement : UIElement
    {
        public bool RenderCalled { get; private set; }

        public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
        {
            RenderCalled = true;
        }
    }

    [Fact]
    public void Show_WithoutTuiWindow_SetsVisibilityOnly()
    {
        var dialog = new DialogBox();
        dialog.Visibility = false;

        dialog.Show();

        Assert.True(dialog.Visibility);
        // Ensure no exception is thrown when root is null
    }

    [Fact]
    public void Show_WithTuiWindow_CentersDialogAndSetsFocus()
    {
        var window = new TuiWindow();
        window.Measure(new Size(100, 100));
        window.Arrange(new Rect(0, 0, 100, 100));

        var dialog = new DialogBox();
        dialog.Width = 40;
        dialog.Height = 20;

        var focusableElement = new TestElement { Focusable = true };
        dialog.Content = focusableElement;

        // Add to logical tree of window
        window.PushOverlay(dialog);

        // Act
        dialog.Show();

        // Assert Centering: (100 - 40) / 2 = 30, (100 - 20) / 2 = 40
        Assert.True(dialog.Visibility);
        Assert.Equal(30, dialog.RenderSize.X);
        Assert.Equal(40, dialog.RenderSize.Y);
        Assert.Equal(40, dialog.RenderSize.Width);
        Assert.Equal(20, dialog.RenderSize.Height);

        // Assert Focus - DialogBox.Show should focus the first focusable element in the dialog
        Assert.True(focusableElement.IsFocused);
    }

    [Fact]
    public void Show_WithTuiWindow_ClampsNegativeCoordinates()
    {
        var window = new TuiWindow();
        window.Measure(new Size(20, 10)); // Smaller than dialog
        window.Arrange(new Rect(0, 0, 20, 10));

        var dialog = new DialogBox();
        dialog.Width = 40;
        dialog.Height = 20;

        window.PushOverlay(dialog);

        // Act
        dialog.Show();

        // Should clamp to 0,0 instead of going off-screen negative
        Assert.Equal(0, dialog.RenderSize.X);
        Assert.Equal(0, dialog.RenderSize.Y);
    }

    [Fact]
    public void Hide_WithoutTuiWindow_SetsVisibilityFalse()
    {
        var dialog = new DialogBox();
        dialog.Visibility = true;

        dialog.Hide();

        Assert.False(dialog.Visibility);
    }

    [Fact]
    public void Hide_WithTuiWindow_RemovesOverlayAndSetsVisibilityFalse()
    {
        var window = new TuiWindow();
        var dialog = new DialogBox();

        window.PushOverlay(dialog);
        dialog.Visibility = true;

        Assert.Equal(dialog, window.Overlay);

        // Act
        dialog.Hide();

        Assert.False(dialog.Visibility);
        Assert.Null(window.Overlay);
    }
}
