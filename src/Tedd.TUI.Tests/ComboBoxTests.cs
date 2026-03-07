using System;
using System.Reflection;
using Xunit;

namespace Tedd.TUI.Tests;

public class ComboBoxTests
{
    [Fact]
    public void TestDropdownHeight_WithManyItems_FitsInSpace()
    {
        // Setup
        var window = new TuiWindow();
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        var comboBox = new ComboBox { VerticalAlignment = VerticalAlignment.Top };
        for (int i = 0; i < 10; i++)
        {
            comboBox.Items.Add($"Item {i}");
        }
        window.Content = comboBox;

        // Ensure layout
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Act - Open dropdown
        var method = typeof(ComboBox).GetMethod("OpenDropdown", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method.Invoke(comboBox, new object[] { window });

        // Assert - Check _popupListBox height
        var field = typeof(ComboBox).GetField("_popupListBox", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var popupListBox = (ListBox)field.GetValue(comboBox);

        // Should be 10 (number of items) as there is plenty of space (24 lines below)
        Assert.Equal(10, popupListBox.Height);
    }

    [Fact]
    public void TestDropdownHeight_WithFewItems()
    {
        // Setup
        var window = new TuiWindow();
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        var comboBox = new ComboBox { VerticalAlignment = VerticalAlignment.Top };
        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");
        window.Content = comboBox;

        // Ensure layout
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Act - Open dropdown
        var method = typeof(ComboBox).GetMethod("OpenDropdown", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method.Invoke(comboBox, new object[] { window });

        // Assert - Check _popupListBox height
        var field = typeof(ComboBox).GetField("_popupListBox", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var popupListBox = (ListBox)field.GetValue(comboBox);

        // Should be 2 (number of items)
        Assert.Equal(2, popupListBox.Height);
    }

    [Fact]
    public void TestDropdownHeight_ClampedBySpace()
    {
        // Setup small window
        var window = new TuiWindow();
        // 5 height total. ComboBox takes 1. Space below is 4.
        // Border takes 2. Content space is 2.
        window.Measure(new Size(80, 5));
        window.Arrange(new Rect(0, 0, 80, 5));

        var comboBox = new ComboBox { VerticalAlignment = VerticalAlignment.Top };
        for (int i = 0; i < 10; i++)
        {
            comboBox.Items.Add($"Item {i}");
        }
        window.Content = comboBox;

        // Ensure layout
        window.Measure(new Size(80, 5));
        window.Arrange(new Rect(0, 0, 80, 5));

        // Act - Open dropdown
        var method = typeof(ComboBox).GetMethod("OpenDropdown", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method.Invoke(comboBox, new object[] { window });

        // Assert
        var field = typeof(ComboBox).GetField("_popupListBox", BindingFlags.NonPublic | BindingFlags.Instance);
        var popupListBox = (ListBox)field.GetValue(comboBox);

        // Expected: Window H=5. ComboBox Y=0, H=1. Bottom=1. Space below=4.
        // Border H = ContentH + 2.
        // ContentH max = SpaceBelow - 2 = 2.
        Assert.Equal(2, popupListBox.Height);
    }

    [Fact]
    public void Properties_SetAndGet()
    {
        var cb = new ComboBox();

        cb.FocusedForeground = ConsoleColor.Red;
        Assert.Equal(ConsoleColor.Red, cb.FocusedForeground);

        cb.FocusedTextBackgroundColor = ConsoleColor.Blue;
        Assert.Equal(ConsoleColor.Blue, cb.FocusedTextBackgroundColor);

        cb.ArrowColor = ConsoleColor.Green;
        Assert.Equal(ConsoleColor.Green, cb.ArrowColor);

        cb.ArrowBackgroundColor = ConsoleColor.Yellow;
        Assert.Equal(ConsoleColor.Yellow, cb.ArrowBackgroundColor);

        cb.FocusedArrowColor = ConsoleColor.Cyan;
        Assert.Equal(ConsoleColor.Cyan, cb.FocusedArrowColor);

        cb.FocusedArrowBackgroundColor = ConsoleColor.Magenta;
        Assert.Equal(ConsoleColor.Magenta, cb.FocusedArrowBackgroundColor);

        cb.PopupBackground = ConsoleColor.DarkGray;
        Assert.Equal(ConsoleColor.DarkGray, cb.PopupBackground);

        cb.PopupBorderColor = ConsoleColor.DarkRed;
        Assert.Equal(ConsoleColor.DarkRed, cb.PopupBorderColor);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Render_FocusStateAndArrowFocus(bool isFocused, bool arrowFocused)
    {
        var cb = new ComboBox();
        cb.Items.Add("Test");
        cb.SelectedIndex = 0;
        cb.Measure(new Size(10, 1));
        cb.Arrange(new Rect(0, 0, 10, 1));

        if (isFocused) cb.Focus();

        if (arrowFocused)
        {
            // Shift to arrow focus
            cb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Tab });
        }

        var buffer = new VirtualBuffer(10, 1);
        cb.Render(buffer, 0, 0);

        // Focus affects text and arrow colors based on arrowFocused state
        var pixelText = buffer.GetPixel(0, 0);
        var pixelArrow = buffer.GetPixel(9, 0);

        // Note: ComboBox text color depends on IsFocused && !_arrowFocused ? FocusedForeground : Foreground
        // and Background ?? ConsoleColor.Black
        var expectedTextFg = (cb.IsFocused && !arrowFocused) ? cb.FocusedForeground : cb.Foreground;
        var expectedTextBg = (cb.IsFocused && !arrowFocused) ? cb.FocusedTextBackgroundColor : (cb.Background ?? ConsoleColor.Black);

        var expectedArrowFg = (cb.IsFocused && arrowFocused) ? cb.FocusedArrowColor : cb.ArrowColor;
        var expectedArrowBg = (cb.IsFocused && arrowFocused) ? cb.FocusedArrowBackgroundColor : cb.ArrowBackgroundColor;

        Assert.Equal(expectedTextFg, pixelText.Foreground);
        Assert.Equal(expectedTextBg, pixelText.Background);
        Assert.Equal(expectedArrowFg, pixelArrow.Foreground);
        Assert.Equal(expectedArrowBg, pixelArrow.Background);
    }

    [Fact]
    public void Focus_OnGotFocus_ResetsArrowFocus()
    {
        var cb = new ComboBox();
        cb.Focus();

        cb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Tab });
        // arrow is focused now

        cb.OnLostFocus();
        cb.OnGotFocus(); // arrow focus should be reset

        cb.Measure(new Size(10, 1));
        cb.Arrange(new Rect(0, 0, 10, 1));
        var buffer = new VirtualBuffer(10, 1);
        cb.Render(buffer, 0, 0);

        var pixelArrow = buffer.GetPixel(9, 0);
        Assert.Equal(cb.ArrowColor, pixelArrow.Foreground);
    }

    [Fact]
    public void ToggleDropdown_OnMouseDown()
    {
        var window = new TuiWindow();
        var cb = new ComboBox();
        cb.Items.Add("Item 1");
        window.Content = cb;

        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        // Should open dropdown
        cb.OnMouseDown(new MouseEventArgs { X = 0, Y = 0 });
        cb.Focus(); // Ensure it takes focus for testing rendering properties
        Assert.True(cb.IsFocused);

        // Check if overlay was added (Border -> ListBox)
        // TuiWindow private _overlays accessed implicitly via VisualChildrenCount or GetVisualChild
        // The dropdown adds an overlay to TuiWindow. TuiWindow returns overlays at the end of GetVisualChild
        Assert.Equal(2, window.VisualChildrenCount); // Content (ComboBox) + Overlay (Border)
        var overlay = window.GetVisualChild(1);
        Assert.IsType<Border>(overlay);

        // Should close dropdown
        cb.OnMouseDown(new MouseEventArgs { X = 0, Y = 0 });
        Assert.Equal(1, window.VisualChildrenCount);
    }

    [Theory]
    [InlineData(ConsoleKey.Spacebar)]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.UpArrow)]
    public void ToggleDropdown_OnKeyDown(ConsoleKey key)
    {
        var window = new TuiWindow();
        var cb = new ComboBox();
        cb.Items.Add("Item 1");
        window.Content = cb;

        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        // Should open dropdown
        cb.OnKeyDown(new KeyEventArgs { Key = key });
        Assert.Equal(2, window.VisualChildrenCount);

        // Should close dropdown
        cb.OnKeyDown(new KeyEventArgs { Key = key });
        Assert.Equal(1, window.VisualChildrenCount);
    }

    [Fact]
    public void KeyboardNavigation_TabAndShiftTab()
    {
        var cb = new ComboBox();
        cb.Focus();

        // Tab shifts to arrow
        var eTab = new KeyEventArgs { Key = ConsoleKey.Tab };
        cb.OnKeyDown(eTab);
        Assert.True(eTab.Handled);

        // Another Tab lets it pass through
        var eTab2 = new KeyEventArgs { Key = ConsoleKey.Tab };
        cb.OnKeyDown(eTab2);
        Assert.False(eTab2.Handled);

        // Shift+Tab shifts back to text
        var eShiftTab = new KeyEventArgs { Key = ConsoleKey.Tab, Modifiers = ConsoleModifiers.Shift };
        cb.OnKeyDown(eShiftTab);
        Assert.True(eShiftTab.Handled);

        // Another Shift+Tab lets it pass through
        var eShiftTab2 = new KeyEventArgs { Key = ConsoleKey.Tab, Modifiers = ConsoleModifiers.Shift };
        cb.OnKeyDown(eShiftTab2);
        Assert.False(eShiftTab2.Handled);
    }

    [Fact]
    public void Popup_SelectionChanged_ClosesDropdown()
    {
        var window = new TuiWindow();
        var cb = new ComboBox();
        cb.Items.Add("Item 1");
        cb.Items.Add("Item 2");
        window.Content = cb;

        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        cb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Enter });
        Assert.Equal(2, window.VisualChildrenCount); // Dropdown open

        var overlay = window.GetVisualChild(1) as Border;
        Assert.NotNull(overlay);
        var popupListBox = overlay.Child as ListBox;
        Assert.NotNull(popupListBox);

        // Trigger selection change in the popup listbox
        popupListBox.SelectedIndex = 1;

        // ComboBox dropdown should be closed
        Assert.Equal(1, window.VisualChildrenCount);
        Assert.Equal(1, cb.SelectedIndex);
    }

    [Fact]
    public void OpenDropdown_WithNoItems()
    {
        var window = new TuiWindow();
        var cb = new ComboBox();
        window.Content = cb;

        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        // Open dropdown
        cb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Enter });
        Assert.Equal(2, window.VisualChildrenCount);

        var overlay = window.GetVisualChild(1) as Border;
        Assert.NotNull(overlay);
        var popupListBox = overlay.Child as ListBox;
        Assert.NotNull(popupListBox);

        // ComboBox takes up the whole window height because it's the root content.
        // `MeasureOverride` returns (15, 1), but `Arrange` on TuiWindow expands to fill available size (80, 24).
        // `absY` = RenderSize.Y + RenderSize.Height = 0 + 24 = 24.
        // `spaceBelow` = 24 - 24 = 0. MaxContentHeight = 0.
        // Therefore, Height is 0.
        Assert.Equal(0, popupListBox.Height);
    }

    [Fact]
    public void OpenDropdown_CalculatesAvailableHeight()
    {
        var window = new TuiWindow();
        var cb = new ComboBox();
        for (int i = 0; i < 20; i++) cb.Items.Add($"Item {i}");

        // Place ComboBox near the bottom of the window
        var canvas = new Canvas();
        Canvas.SetTop(cb, 22);
        canvas.AddChild(cb);
        window.Content = canvas;

        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        // Open dropdown
        cb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Enter });

        var overlay = window.GetVisualChild(1) as Border;
        Assert.NotNull(overlay);

        // Height + 2 for border -> overlay height = 2
        Assert.Equal(2, overlay.Height);
    }
}
