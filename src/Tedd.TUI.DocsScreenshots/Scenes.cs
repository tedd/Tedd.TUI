using Tedd.TUI;
using Tedd.TUI.CodeColoring;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.DocsScreenshots;

/// <summary>Builds the six sample <see cref="TuiWindow"/> scenes used as documentation screenshots.</summary>
internal static class Scenes
{
    private static readonly TuiColor PageBackground = TuiColor.FromHex("#0c0e12");

    private static TextBlock Label(string text, TuiColor? foreground = null)
    {
        var block = new TextBlock { Text = text };
        if (foreground.HasValue) block.Foreground = foreground.Value;
        return block;
    }

    public static TuiWindow BuildHero()
    {
        var stack = new StackPanel { Orientation = Orientation.Vertical };
        stack.AddChild(Label("Tedd.TUI — one UI, every surface", TuiColor.Cyan));
        stack.AddChild(Label("Terminal · Blazor · WPF · Avalonia · WinUI · MAUI", TuiColor.Green));
        stack.AddChild(Label(" "));

        var submit = new Button { Content = "Submit", BoxStyle = BoxStyle.Double };
        var cancel = new Button { Content = "Cancel", Margin = new Thickness(2, 0, 0, 0) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.AddChild(submit);
        buttons.AddChild(cancel);
        stack.AddChild(buttons);

        stack.AddChild(new ProgressBar
        {
            Width = 46,
            Value = 65,
            LabelMode = ProgressBarLabelMode.Percent,
            Margin = new Thickness(0, 1, 0, 0)
        });

        stack.AddChild(new CheckBox
        {
            Content = "Enable turbo mode",
            IsChecked = true,
            Margin = new Thickness(0, 1, 0, 0)
        });

        var box = new Border
        {
            BoxStyle = BoxStyle.Double,
            BorderColor = TuiColor.Cyan,
            Padding = new Thickness(2, 1, 2, 1),
            Margin = new Thickness(1),
            Child = stack
        };

        var window = new TuiWindow { Background = PageBackground, Content = box };
        submit.Focus();
        return window;
    }

    public static TuiWindow BuildMarkdown()
    {
        const string markdown = """
            # Markdown in the terminal

            Live-parsed into **styled cells** — headings, bullet lists, quotes, tables,
            `inline code` and [hyperlinks](https://tedd.github.io/Tedd.TUI/).

            - Fenced code blocks use the syntax highlighter
            - Inline images render as pixels or half-blocks

            > One document — identical on every host.

            | Control | Renders |
            |---|---|
            | MarkdownView | rich text |
            | CodeDocument | highlighted code |
            """;

        var view = new MarkdownView { Text = markdown, Width = 62 };

        var box = new Border
        {
            BoxStyle = BoxStyle.Single,
            BorderColor = TuiColor.Cyan,
            Title = Label("MarkdownView", TuiColor.Cyan),
            Padding = new Thickness(1),
            Margin = new Thickness(1),
            Child = view
        };

        return new TuiWindow { Background = PageBackground, Content = box };
    }

    public static TuiWindow BuildCode()
    {
        const string csharp = """
            // PrismTokenizer + CodeDocument
            public sealed class Greeter
            {
                public int Count { get; } = 3;

                public void Greet(string who)
                {
                    for (var i = 0; i < Count; i++)
                        Log($"Hello {who}!");
                }
            }
            """;

        const string json = """
            {
              "name": "Tedd.TUI",
              "grammars": 27,
              "colors": "truecolor",
              "aliases": ["cs", "py"]
            }
            """;

        var csharpDoc = new CodeDocument();
        csharpDoc.SetCode(csharp, "csharp");
        var jsonDoc = new CodeDocument();
        jsonDoc.SetCode(json, "json");

        var csharpPane = new Border
        {
            BoxStyle = BoxStyle.Single,
            BorderColor = TuiColor.Gray,
            Title = Label(" demo.cs ", TuiColor.Gray),
            Padding = new Thickness(1),
            Child = csharpDoc
        };
        var jsonPane = new Border
        {
            BoxStyle = BoxStyle.Single,
            BorderColor = TuiColor.Gray,
            Title = Label(" demo.json ", TuiColor.Gray),
            Padding = new Thickness(1),
            Margin = new Thickness(2, 0, 0, 0),
            Child = jsonDoc
        };

        var panes = new StackPanel { Orientation = Orientation.Horizontal };
        panes.AddChild(csharpPane);
        panes.AddChild(jsonPane);

        var footer = Label(" c# · python · rust · sql · bash · powershell · json · yaml · xml …", TuiColor.Gray);
        footer.Margin = new Thickness(0, 1, 0, 0);

        var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(1) };
        stack.AddChild(panes);
        stack.AddChild(footer);

        return new TuiWindow { Background = PageBackground, Content = stack };
    }

    public static TuiWindow BuildImages(string samplePhotoPath)
    {
        var bitmap = new Image
        {
            Source = samplePhotoPath,
            AltText = "sunset",
            RenderMode = ImageRenderMode.Graphic,
            MaxCellWidth = 24,
            MaxCellHeight = 12
        };
        var halfBlock = new Image
        {
            Source = samplePhotoPath,
            AltText = "sunset",
            RenderMode = ImageRenderMode.Ascii,
            MaxCellWidth = 24,
            MaxCellHeight = 12
        };

        var bitmapStack = new StackPanel { Orientation = Orientation.Vertical };
        bitmapStack.AddChild(bitmap);
        bitmapStack.AddChild(Label("Sixel · Kitty · iTerm2", TuiColor.Gray));

        var halfBlockStack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(2, 0, 0, 0) };
        halfBlockStack.AddChild(halfBlock);
        halfBlockStack.AddChild(Label("half-block fallback", TuiColor.Gray));

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.AddChild(bitmapStack);
        row.AddChild(halfBlockStack);

        var box = new Border
        {
            BoxStyle = BoxStyle.Single,
            BorderColor = TuiColor.Cyan,
            Title = Label("inline images", TuiColor.Cyan),
            Padding = new Thickness(1),
            Margin = new Thickness(1),
            Child = row
        };

        return new TuiWindow { Background = PageBackground, Content = box };
    }

    public static TuiWindow BuildForm()
    {
        var stack = new StackPanel { Orientation = Orientation.Vertical };

        stack.AddChild(Label("Name:"));
        var nameBox = new TextBox { Width = 30, Text = "John Doe" };
        stack.AddChild(nameBox);

        var passwordLabel = Label("Password:");
        passwordLabel.Margin = new Thickness(0, 1, 0, 0);
        stack.AddChild(passwordLabel);
        var passwordBox = new PasswordBox { Width = 30, Password = "secret" };
        stack.AddChild(passwordBox);

        var genderLabel = Label("Gender:");
        genderLabel.Margin = new Thickness(0, 1, 0, 0);
        stack.AddChild(genderLabel);
        var genderRow = new StackPanel { Orientation = Orientation.Horizontal };
        genderRow.AddChild(new RadioButton { Content = "Male", GroupName = "Gender", IsChecked = true });
        genderRow.AddChild(new RadioButton { Content = "Female", GroupName = "Gender", Margin = new Thickness(2, 0, 0, 0) });
        genderRow.AddChild(new RadioButton { Content = "Other", GroupName = "Gender", Margin = new Thickness(2, 0, 0, 0) });
        stack.AddChild(genderRow);

        var volumeLabel = Label("Volume:");
        volumeLabel.Margin = new Thickness(0, 1, 0, 0);
        stack.AddChild(volumeLabel);
        stack.AddChild(new Slider { Minimum = 0, Maximum = 100, Value = 65, Width = 30 });

        var submit = new Button { Content = "Submit", BoxStyle = BoxStyle.Double, Margin = new Thickness(0, 1, 0, 0) };
        var reset = new Button { Content = "Reset", Margin = new Thickness(2, 1, 0, 0) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.AddChild(submit);
        buttons.AddChild(reset);
        stack.AddChild(buttons);

        var box = new Border
        {
            BoxStyle = BoxStyle.Single,
            BorderColor = TuiColor.Green,
            Title = Label("User registration", TuiColor.Yellow),
            Padding = new Thickness(1),
            Margin = new Thickness(1),
            Child = stack
        };

        var window = new TuiWindow { Background = PageBackground, Content = box };
        nameBox.Focus();
        nameBox.SelectAll();
        return window;
    }

    public static TuiWindow BuildTable()
    {
        var table = new Table { Width = 50 };
        table.Columns.Add(new TableColumn { Header = "ID", Width = GridLength.Pixel(5) });
        table.Columns.Add(new TableColumn { Header = "Vessel", Width = GridLength.Star });
        table.Columns.Add(new TableColumn { Header = "Status", Width = GridLength.Pixel(10) });
        table.Columns.Add(new TableColumn { Header = "Crew", Width = GridLength.Pixel(4) });

        table.AddRow("101", "Aurora", "Docked", "24");
        table.AddRow("102", "Borealis", "En route", "31");
        table.AddRow("103", "Cassiopeia", "Docked", "18");
        table.AddRow("104", "Draugr", "Refit", "9");
        table.AddRow("105", "Eventide", "En route", "27");
        table.AddRow("106", "Fenrir", "Docked", "22");

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        stack.AddChild(table);
        var hint = Label("Enter: select   Tab: next   F10: menu", TuiColor.Gray);
        hint.Margin = new Thickness(0, 1, 0, 0);
        stack.AddChild(hint);

        var box = new Border
        {
            BoxStyle = BoxStyle.Double,
            BorderColor = TuiColor.Cyan,
            Title = Label("Fleet status", TuiColor.Cyan),
            Padding = new Thickness(1),
            Margin = new Thickness(1),
            Child = stack
        };

        return new TuiWindow { Background = PageBackground, Content = box };
    }
}
