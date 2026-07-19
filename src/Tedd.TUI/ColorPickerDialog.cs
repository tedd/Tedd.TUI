using System;

namespace Tedd.TUI;

/// <summary>
/// A clickable color swatch used by <see cref="ColorPickerDialog"/>: renders a
/// solid block of its <see cref="Color"/> and raises <see cref="Picked"/> on
/// click, Enter or Space. Also usable standalone as a color preview
/// (set <see cref="UIElement.Focusable"/> to false).
/// </summary>
public class ColorSwatch : UIElement
{
    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register("Color", typeof(TuiColor), typeof(ColorSwatch), TuiColor.White);

    public TuiColor Color
    {
        get => (TuiColor)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>Raised when the swatch is activated (click / Enter / Space).</summary>
    public event EventHandler? Picked;

    public ColorSwatch()
    {
        Focusable = true;
        Width = 4;
        Height = 1;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(Width > 0 ? Width : 4, Height > 0 ? Height : 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (!Visibility) return;

        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        var color = Color;

        for (int row = 0; row < RenderSize.Height; row++)
        {
            for (int col = 0; col < RenderSize.Width; col++)
            {
                buffer.SetPixel(x + col, y + row, ' ', color, color);
            }
        }

        // Focus is indicated by corner brackets so the highlight survives any
        // swatch color.
        if (IsFocused && RenderSize.Width >= 2)
        {
            var marker = color.R + color.G + color.B > 382 ? TuiColor.Black : TuiColor.White;
            buffer.SetPixel(x, y, '[', marker, color);
            buffer.SetPixel(x + RenderSize.Width - 1, y + RenderSize.Height - 1, ']', marker, color);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Handled) return;
        e.Handled = true;
        Focus();
        Picked?.Invoke(this, EventArgs.Empty);
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && (e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.Spacebar))
        {
            e.Handled = true;
            Picked?.Invoke(this, EventArgs.Empty);
        }
    }
}

/// <summary>
/// Modal dialog for picking a color: a palette of the 16 standard console
/// colors, R/G/B numeric inputs, a hex field (#RRGGBB, Enter applies) and a
/// live preview. The chosen color is in <see cref="SelectedColor"/> when
/// <see cref="Dialog.DialogResult"/> is true.
/// </summary>
public class ColorPickerDialog : Dialog
{
    public static readonly DependencyProperty SelectedColorProperty =
        DependencyProperty.Register("SelectedColor", typeof(TuiColor), typeof(ColorPickerDialog), TuiColor.White);

    public TuiColor SelectedColor
    {
        get => (TuiColor)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    private static readonly TuiColor[] Palette =
    [
        TuiColor.Black, TuiColor.DarkBlue, TuiColor.DarkGreen, TuiColor.DarkCyan,
        TuiColor.DarkRed, TuiColor.DarkMagenta, TuiColor.DarkYellow, TuiColor.Gray,
        TuiColor.DarkGray, TuiColor.Blue, TuiColor.Green, TuiColor.Cyan,
        TuiColor.Red, TuiColor.Magenta, TuiColor.Yellow, TuiColor.White
    ];

    protected NumericUpDown RedBox { get; private set; } = null!;
    protected NumericUpDown GreenBox { get; private set; } = null!;
    protected NumericUpDown BlueBox { get; private set; } = null!;
    protected TextBox HexBox { get; private set; } = null!;
    protected ColorSwatch Preview { get; private set; } = null!;

    private bool _syncing;

    public ColorPickerDialog()
    {
        Title = "Select Color";
        Width = 42;
        CanResize = false;
    }

    /// <summary>
    /// Creates and shows a color picker on <paramref name="host"/>.
    /// <paramref name="onClosed"/> receives the chosen color, or null when cancelled.
    /// </summary>
    public static ColorPickerDialog Show(TuiWindow host, TuiColor initialColor,
        string title = "Select Color", Action<TuiColor?>? onClosed = null)
    {
        var dialog = new ColorPickerDialog
        {
            SelectedColor = initialColor,
            Title = title
        };
        if (onClosed != null)
        {
            dialog.Closed += (s, e) => onClosed(dialog.DialogResult == true ? dialog.SelectedColor : null);
        }
        dialog.ShowDialog(host);
        return dialog;
    }

    /// <summary>Rebuilds the dialog UI. Called automatically by <see cref="Show()"/>.</summary>
    protected virtual void BuildContent()
    {
        var palette = new UniformGrid { Columns = 8, Margin = new Thickness(1, 0, 1, 0) };
        foreach (var color in Palette)
        {
            var swatch = new ColorSwatch { Color = color, Margin = new Thickness(0, 0, 1, 0) };
            swatch.Picked += (s, e) => SelectedColor = ((ColorSwatch)s!).Color;
            palette.Children.Add(swatch);
        }

        RedBox = CreateChannelBox("RedBox");
        GreenBox = CreateChannelBox("GreenBox");
        BlueBox = CreateChannelBox("BlueBox");

        var rgbRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(1, 1, 1, 0)
        };
        rgbRow.Children.Add(new TextBlock { Text = "R:" });
        rgbRow.Children.Add(RedBox);
        rgbRow.Children.Add(new TextBlock { Text = " G:" });
        rgbRow.Children.Add(GreenBox);
        rgbRow.Children.Add(new TextBlock { Text = " B:" });
        rgbRow.Children.Add(BlueBox);

        HexBox = new TextBox { Name = "HexBox", Width = 9 };
        Preview = new ColorSwatch { Name = "Preview", Focusable = false, Width = 8, Margin = new Thickness(2, 0, 0, 0) };

        var hexRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(1, 1, 1, 0)
        };
        hexRow.Children.Add(new TextBlock { Text = "Hex: " });
        hexRow.Children.Add(HexBox);
        hexRow.Children.Add(Preview);

        var okButton = new Button { Name = "OkButton", Content = "OK", Margin = new Thickness(1, 0, 1, 0) };
        okButton.Click += (s, e) => Close(true);
        var cancelButton = new Button { Name = "CancelButton", Content = "Cancel", Margin = new Thickness(1, 0, 1, 0) };
        cancelButton.Click += (s, e) => Close(false);

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 1, 0, 0)
        };
        buttonRow.Children.Add(okButton);
        buttonRow.Children.Add(cancelButton);

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        stack.Children.Add(palette);
        stack.Children.Add(rgbRow);
        stack.Children.Add(hexRow);
        stack.Children.Add(buttonRow);
        Content = stack;

        SyncFromColor();
    }

    private NumericUpDown CreateChannelBox(string name)
    {
        var box = new NumericUpDown { Name = name, Minimum = 0, Maximum = 255, Width = 9 };
        box.ValueChanged += (s, e) =>
        {
            if (_syncing) return;
            SelectedColor = TuiColor.FromRgb((byte)RedBox.Value, (byte)GreenBox.Value, (byte)BlueBox.Value);
        };
        return box;
    }

    /// <summary>Pushes <see cref="SelectedColor"/> into the RGB boxes, hex field and preview.</summary>
    private void SyncFromColor()
    {
        if (RedBox == null) return;

        _syncing = true;
        try
        {
            var color = SelectedColor;
            RedBox.Value = color.R;
            GreenBox.Value = color.G;
            BlueBox.Value = color.B;
            HexBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            Preview.Color = color;
        }
        finally
        {
            _syncing = false;
        }
        Invalidate();
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == SelectedColorProperty && !_syncing)
        {
            SyncFromColor();
        }
    }

    /// <summary>Applies the hex field ("#RRGGBB" or "RRGGBB") to <see cref="SelectedColor"/>.</summary>
    public void ApplyHex()
    {
        string text = HexBox.Text?.Trim() ?? string.Empty;
        if (text.Length == 0) return;

        try
        {
            SelectedColor = TuiColor.FromHex(text.StartsWith('#') ? text : "#" + text);
        }
        catch
        {
            // Invalid hex: restore the field from the current color.
            SyncFromColor();
        }
    }

    public override void Show()
    {
        BuildContent();
        base.Show();
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && e.Key == ConsoleKey.Enter && ReferenceEquals(e.Source, HexBox))
        {
            e.Handled = true;
            ApplyHex();
            return;
        }
        base.OnKeyDown(e);
    }
}
