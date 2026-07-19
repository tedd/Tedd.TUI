using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ColorPickerDialogTests
{
    private static TuiWindow CreateHost()
    {
        var host = new TuiWindow();
        host.Measure(new Size(80, 25));
        host.Arrange(new Rect(0, 0, 80, 25));
        return host;
    }

    private static NumericUpDown GetChannel(ColorPickerDialog dialog, string name) =>
        (NumericUpDown)dialog.FindName(name);

    [Fact]
    public void Show_SyncsInitialColorToInputs()
    {
        var host = CreateHost();
        var dialog = ColorPickerDialog.Show(host, TuiColor.FromRgb(10, 20, 30));

        Assert.Equal(10, GetChannel(dialog, "RedBox").Value);
        Assert.Equal(20, GetChannel(dialog, "GreenBox").Value);
        Assert.Equal(30, GetChannel(dialog, "BlueBox").Value);
        Assert.Equal("#0A141E", ((TextBox)dialog.FindName("HexBox")).Text);
        Assert.Equal(TuiColor.FromRgb(10, 20, 30), ((ColorSwatch)dialog.FindName("Preview")).Color);
    }

    [Fact]
    public void ChangingChannel_UpdatesSelectedColorAndHex()
    {
        var host = CreateHost();
        var dialog = ColorPickerDialog.Show(host, TuiColor.FromRgb(0, 0, 0));

        GetChannel(dialog, "RedBox").Value = 255;

        Assert.Equal(TuiColor.FromRgb(255, 0, 0), dialog.SelectedColor);
        Assert.Equal("#FF0000", ((TextBox)dialog.FindName("HexBox")).Text);
    }

    [Fact]
    public void ApplyHex_SetsColorAndChannels()
    {
        var host = CreateHost();
        var dialog = ColorPickerDialog.Show(host, TuiColor.Black);

        var hexBox = (TextBox)dialog.FindName("HexBox");
        hexBox.Text = "#336699";
        dialog.ApplyHex();

        Assert.Equal(TuiColor.FromRgb(0x33, 0x66, 0x99), dialog.SelectedColor);
        Assert.Equal(0x33, GetChannel(dialog, "RedBox").Value);
        Assert.Equal(0x66, GetChannel(dialog, "GreenBox").Value);
        Assert.Equal(0x99, GetChannel(dialog, "BlueBox").Value);
    }

    [Fact]
    public void ApplyHex_WithoutHashPrefix_Works()
    {
        var host = CreateHost();
        var dialog = ColorPickerDialog.Show(host, TuiColor.Black);

        var hexBox = (TextBox)dialog.FindName("HexBox");
        hexBox.Text = "AABBCC";
        dialog.ApplyHex();

        Assert.Equal(TuiColor.FromRgb(0xAA, 0xBB, 0xCC), dialog.SelectedColor);
    }

    [Fact]
    public void ApplyHex_Invalid_RestoresCurrentColor()
    {
        var host = CreateHost();
        var dialog = ColorPickerDialog.Show(host, TuiColor.FromRgb(1, 2, 3));

        var hexBox = (TextBox)dialog.FindName("HexBox");
        hexBox.Text = "not-a-color";
        dialog.ApplyHex();

        Assert.Equal(TuiColor.FromRgb(1, 2, 3), dialog.SelectedColor);
        Assert.Equal("#010203", hexBox.Text);
    }

    [Fact]
    public void PaletteSwatch_Pick_SetsSelectedColor()
    {
        var host = CreateHost();
        var dialog = ColorPickerDialog.Show(host, TuiColor.Black);

        // Find a red swatch among the palette swatches.
        ColorSwatch? redSwatch = null;
        void FindSwatch(UIElement element)
        {
            if (element is ColorSwatch { Focusable: true } swatch && swatch.Color == TuiColor.Red)
            {
                redSwatch = swatch;
                return;
            }
            for (int i = 0; i < element.VisualChildrenCount; i++)
            {
                FindSwatch(element.GetVisualChild(i));
                if (redSwatch != null) return;
            }
        }
        FindSwatch(dialog);
        Assert.NotNull(redSwatch);

        redSwatch.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Enter });

        Assert.Equal(TuiColor.Red, dialog.SelectedColor);
    }

    [Fact]
    public void OkButton_AcceptsWithColor()
    {
        var host = CreateHost();
        TuiColor? result = null;
        var dialog = ColorPickerDialog.Show(host, TuiColor.Cyan, onClosed: c => result = c);

        var ok = (Button)dialog.FindName("OkButton");
        ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, ok));

        Assert.True(dialog.DialogResult);
        Assert.Equal(TuiColor.Cyan, result);
        Assert.Null(host.Overlay);
    }

    [Fact]
    public void Cancel_ReportsNullColor()
    {
        var host = CreateHost();
        TuiColor? result = TuiColor.White;
        var dialog = ColorPickerDialog.Show(host, TuiColor.Cyan, onClosed: c => result = c);

        var cancel = (Button)dialog.FindName("CancelButton");
        cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, cancel));

        Assert.False(dialog.DialogResult);
        Assert.Null(result);
    }

    [Fact]
    public void ColorSwatch_MouseDown_RaisesPicked()
    {
        var swatch = new ColorSwatch { Color = TuiColor.Green };
        swatch.Measure(new Size(4, 1));
        swatch.Arrange(new Rect(0, 0, 4, 1));

        int picked = 0;
        swatch.Picked += (s, e) => picked++;
        swatch.OnMouseDown(new MouseEventArgs { X = 1, Y = 0 });

        Assert.Equal(1, picked);
    }

    [Fact]
    public void ColorSwatch_RendersSolidColor()
    {
        var swatch = new ColorSwatch { Color = TuiColor.Magenta };
        swatch.Measure(new Size(4, 1));
        swatch.Arrange(new Rect(0, 0, 4, 1));

        var buffer = new VirtualBuffer(10, 3);
        swatch.Render(buffer, 0, 0);

        var pixel = buffer.GetPixel(1, 0);
        Assert.Equal(TuiColor.Magenta, pixel.Background);
    }
}
