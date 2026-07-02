using System;
using Xunit;
using Tedd.TUI;
using System.Reflection;

namespace Tedd.TUI.Tests;

public class RadioButtonCoverageTests
{
    // Theory for DependencyProperties: TuiColor is a struct, which isn't allowed in InlineData
    [Theory]
    [InlineData((int)ConsoleColor.Blue, (int)ConsoleColor.Red, (int)ConsoleColor.Cyan, 'x', '-')]
    [InlineData((int)ConsoleColor.Green, (int)ConsoleColor.Yellow, (int)ConsoleColor.Gray, 'X', 'O')]
    public void RadioButton_DependencyProperties_GetSet(int focusedForegroundIndex, int checkColorIndex, int bracketColorIndex, char checkedChar, char uncheckedChar)
    {
        var focusedForeground = (TuiColor)(ConsoleColor)focusedForegroundIndex;
        var checkColor = (TuiColor)(ConsoleColor)checkColorIndex;
        var bracketColor = (TuiColor)(ConsoleColor)bracketColorIndex;

        var rb = new RadioButton();

        rb.FocusedForeground = focusedForeground;
        Assert.Equal(focusedForeground, rb.FocusedForeground);

        rb.CheckColor = checkColor;
        Assert.Equal(checkColor, rb.CheckColor);

        rb.BracketColor = bracketColor;
        Assert.Equal(bracketColor, rb.BracketColor);

        rb.CheckedChar = checkedChar;
        Assert.Equal(checkedChar, rb.CheckedChar);

        rb.UncheckedChar = uncheckedChar;
        Assert.Equal(uncheckedChar, rb.UncheckedChar);
    }

    [Theory]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Spacebar)]
    public void RadioButton_OnToggle_UncheckedToChecked(ConsoleKey keyToPress)
    {
        var rb = new RadioButton();

        rb.IsChecked = false;

        rb.OnKeyDown(new KeyEventArgs { Key = keyToPress });
        rb.OnKeyUp(new KeyEventArgs { Key = keyToPress });
        Assert.True(rb.IsChecked);
    }

    [Theory]
    [InlineData("Option", 10, 1)]
    [InlineData("Yes", 7, 1)]
    [InlineData(null, 4, 1)]
    public void RadioButton_MeasureOverride_CalculatesCorrectSize(string? content, int expectedWidth, int expectedHeight)
    {
        var rb = new RadioButton();
        rb.Content = content;

        rb.Measure(new Size(100, 100));

        Assert.Equal(expectedWidth, rb.DesiredSize.Width);
        Assert.Equal(expectedHeight, rb.DesiredSize.Height);
    }

    [Theory]
    [InlineData("Test", true, 'o', 't')]
    [InlineData("No", false, ' ', 'o')]
    public void RadioButton_Render_OutputsExpectedCharacters(string content, bool isChecked, char expectedCheckChar, char lastChar)
    {
        var rb = new RadioButton();
        rb.Content = content;
        rb.IsChecked = isChecked;
        rb.Measure(new Size(10, 1));
        rb.Arrange(new Rect(0, 0, 10, 1));

        var buffer = new VirtualBuffer(10, 1);
        rb.Render(buffer, 0, 0);

        Assert.Equal('(', buffer.GetPixel(0, 0).Character);
        Assert.Equal(expectedCheckChar, buffer.GetPixel(1, 0).Character);
        Assert.Equal(')', buffer.GetPixel(2, 0).Character);

        if (content.Length > 0)
        {
            Assert.Equal(content[0], buffer.GetPixel(4, 0).Character);
            int lastIndex = 4 + content.Length - 1;
            Assert.Equal(lastChar, buffer.GetPixel(lastIndex, 0).Character);
        }
    }

    [Theory]
    [InlineData((int)ConsoleColor.Blue)]
    [InlineData((int)ConsoleColor.Red)]
    public void RadioButton_Render_Focused_OutputsExpectedForeground(int expectedColorIndex)
    {
        var expectedColor = (TuiColor)(ConsoleColor)expectedColorIndex;
        var p = new StackPanel();
        var w = new TuiWindow();
        w.Content = p;

        var rb = new RadioButton();
        p.AddChild(rb);

        rb.Content = "T";
        rb.IsChecked = false;
        rb.FocusedForeground = expectedColor;

        w.Measure(new Size(100, 100));
        w.Arrange(new Rect(0, 0, 100, 100));

        rb.Focus();

        var buffer = new VirtualBuffer(10, 1);
        rb.Render(buffer, 0, 0);

        Assert.Equal(expectedColor, buffer.GetPixel(4, 0).Foreground);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.UpArrow)]
    public void RadioButton_NavigateToSibling_NotStackPanel_Returns(ConsoleKey keyToPress)
    {
        var grid = new Grid();
        var rb1 = new RadioButton { GroupName = "Group" };
        var rb2 = new RadioButton { GroupName = "Group" };
        grid.AddChild(rb1);
        grid.AddChild(rb2);

        rb1.Focus();
        rb1.OnKeyDown(new KeyEventArgs { Key = keyToPress });

        Assert.False(rb2.IsChecked);
        Assert.False(rb2.IsFocused);
    }

    private class MaliciousRadioButton : RadioButton
    {
        public Action? BeforeNavigate;
        public override void OnKeyDown(KeyEventArgs e)
        {
            BeforeNavigate?.Invoke();
            base.OnKeyDown(e);
        }
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.RightArrow)]
    public void RadioButton_NavigateToSibling_EmptyPanel_Returns(ConsoleKey keyToPress)
    {
        var panel = new StackPanel();
        var rb1 = new MaliciousRadioButton { GroupName = "Group" };

        panel.AddChild(rb1);

        rb1.BeforeNavigate = () => panel.Children.Clear();

        rb1.Focus();
        rb1.OnKeyDown(new KeyEventArgs { Key = keyToPress });

        Assert.False(rb1.IsChecked);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.RightArrow)]
    public void RadioButton_NavigateToSibling_NoStart_Returns(ConsoleKey keyToPress)
    {
        var panel = new StackPanel();
        var rb1 = new MaliciousRadioButton { GroupName = "Group" };
        var rb2 = new RadioButton { GroupName = "Group" };

        panel.AddChild(rb1);
        panel.AddChild(rb2);

        rb1.BeforeNavigate = () => {
            panel.Children.Clear();
            panel.AddChild(new Button());
        };

        rb1.Focus();
        rb1.OnKeyDown(new KeyEventArgs { Key = keyToPress });

        Assert.False(rb2.IsChecked);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.RightArrow)]
    public void RadioButton_NavigateToSibling_LoopAround_Returns(ConsoleKey keyToPress)
    {
        var panel = new StackPanel();
        var rb1 = new RadioButton { GroupName = "Group" };
        var other = new Button();
        panel.AddChild(rb1);
        panel.AddChild(other);

        rb1.Focus();
        rb1.OnKeyDown(new KeyEventArgs { Key = keyToPress });

        Assert.False(rb1.IsChecked);
    }

    [Theory]
    [InlineData(ConsoleKey.UpArrow)]
    [InlineData(ConsoleKey.LeftArrow)]
    public void RadioButton_NavigateToSibling_UpArrow_WrapsToLast(ConsoleKey keyToPress)
    {
        var w = new TuiWindow();
        var panel = new StackPanel();
        w.Content = panel;

        var rb1 = new RadioButton { GroupName = "Group" };
        var rb2 = new RadioButton { GroupName = "Group" };

        panel.AddChild(rb1);
        panel.AddChild(rb2);
        w.Measure(new Size(100, 100));
        w.Arrange(new Rect(0, 0, 100, 100));

        rb1.Focus();
        rb1.IsChecked = true;

        rb1.OnKeyDown(new KeyEventArgs { Key = keyToPress });

        Assert.True(rb2.IsChecked);
        Assert.True(rb2.IsFocused);
    }

    [Theory]
    [InlineData(ConsoleKey.A)]
    [InlineData(ConsoleKey.B)]
    public void RadioButton_OnKeyDown_UnhandledKey_CallsBase(ConsoleKey keyToPress)
    {
        var rb = new RadioButton();
        var args = new KeyEventArgs { Key = keyToPress };
        rb.OnKeyDown(args);

        Assert.False(args.Handled);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.RightArrow)]
    public void RadioButton_NavigateToSibling_FocusesAndChecksAlreadyUnchecked(ConsoleKey keyToPress)
    {
        var w = new TuiWindow();
        var panel = new StackPanel();
        w.Content = panel;
        var rb1 = new RadioButton { GroupName = "Group" };
        var rb2 = new RadioButton { GroupName = "Group" };

        panel.AddChild(rb1);
        panel.AddChild(rb2);
        w.Measure(new Size(100, 100));
        w.Arrange(new Rect(0, 0, 100, 100));

        rb2.IsChecked = false;

        rb1.Focus();
        rb1.IsChecked = true;

        rb1.OnKeyDown(new KeyEventArgs { Key = keyToPress });

        Assert.True(rb2.IsChecked);
        Assert.True(rb2.IsFocused);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.RightArrow)]
    public void RadioButton_NavigateToSibling_DifferentGroup_Ignored(ConsoleKey keyToPress)
    {
        var w = new TuiWindow();
        var panel = new StackPanel();
        w.Content = panel;
        var rb1 = new RadioButton { GroupName = "Group1" };
        var rb2 = new RadioButton { GroupName = "Group2" };
        var rb3 = new RadioButton { GroupName = "Group1" };

        panel.AddChild(rb1);
        panel.AddChild(rb2);
        panel.AddChild(rb3);
        w.Measure(new Size(100, 100));
        w.Arrange(new Rect(0, 0, 100, 100));

        rb1.Focus();
        rb1.IsChecked = true;

        rb1.OnKeyDown(new KeyEventArgs { Key = keyToPress });

        Assert.False(rb2.IsChecked);
        Assert.True(rb3.IsChecked);
        Assert.True(rb3.IsFocused);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.RightArrow)]
    public void RadioButton_NavigateToSibling_NotCheckedNotSelf_HitsContinue(ConsoleKey keyToPress)
    {
        var panel = new StackPanel();
        var rb1 = new RadioButton { GroupName = "Group" };
        var rb2 = new RadioButton { GroupName = "Group" };

        panel.AddChild(rb1);
        panel.AddChild(rb2);

        rb1.IsChecked = false;
        rb2.IsChecked = false;

        rb1.Focus();
        rb1.OnKeyDown(new KeyEventArgs { Key = keyToPress });

        Assert.True(rb2.IsChecked);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.RightArrow)]
    public void RadioButton_NavigateToSibling_EmptyPanel_Hits136(ConsoleKey keyToPress)
    {
        var panel = new StackPanel();
        var rb1 = new RadioButton { GroupName = "Group" };

        typeof(UIElement).GetProperty("Parent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(rb1, panel);

        rb1.Focus();
        rb1.OnKeyDown(new KeyEventArgs { Key = keyToPress });

        Assert.False(rb1.IsChecked);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.RightArrow)]
    public void RadioButton_NavigateToSibling_NoStart_Hits163(ConsoleKey keyToPress)
    {
        var panel = new StackPanel();
        var rb1 = new RadioButton { GroupName = "Group" };
        var btn = new Button();

        panel.AddChild(btn);

        typeof(UIElement).GetProperty("Parent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(rb1, panel);

        rb1.Focus();
        rb1.OnKeyDown(new KeyEventArgs { Key = keyToPress });

        Assert.False(rb1.IsChecked);
    }
}
