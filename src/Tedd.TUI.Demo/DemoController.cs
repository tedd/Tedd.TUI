using Tedd.TUI.CodeColoring;
using Tedd.TUI.Markdown;
using Tedd.TUI.Platform.Console;

namespace Tedd.TUI.Demo;

public class DemoController
{
    private TuiApp _app;
    private TuiWindow _window;

    // Controls injected by XamlLoader via Name matching
    public ListBox LogBox;
    public TextBox NameBox;
    public TextBox PassBox;
    public CheckBox TermsCheck;
    public ComboBox CountryCombo;
    public ListBox SimpleList;
    public Table DemoTable;
    public ScrollBar HScroll;
    public ScrollBar VScroll;
    public TextBlock ScrollLabel;
    public StackPanel ScrollStackPanel;
    public CodeDocument CodeDoc;
    public MarkdownView MdView;
    public TextEditor EditorBox;

    public void Initialize(TuiApp app, TuiWindow window)
    {
        _app = app;
        _window = window;

        // Init Country Combo
        if (CountryCombo != null)
        {
            CountryCombo.Items.Add("USA");
            CountryCombo.Items.Add("Canada");
            CountryCombo.Items.Add("UK");
            CountryCombo.Items.Add("Germany");
            CountryCombo.Items.Add("France");
            CountryCombo.SelectedItem = "USA";
        }

        // Init Simple List
        if (SimpleList != null)
        {
            for (int i = 1; i <= 20; i++) SimpleList.Items.Add($"Item {i}");
        }

        // Init Table
        if (DemoTable != null)
        {
            // Add Comparers
            if (DemoTable.Columns.Count > 0)
            {
                DemoTable.Columns[0].SortComparer = (a, b) =>
                {
                    if (int.TryParse(a.ToString(), out int i1) && int.TryParse(b.ToString(), out int i2)) return i1.CompareTo(i2);
                    return 0;
                };
            }
            if (DemoTable.Columns.Count > 2)
            {
                DemoTable.Columns[2].SortComparer = (a, b) =>
                {
                    if (int.TryParse(a.ToString(), out int i1) && int.TryParse(b.ToString(), out int i2)) return i1.CompareTo(i2);
                    return 0;
                };
            }

            // Add Rows manually (clearing XAML rows to avoid duplicates or keeping them as example)
            // Let's keep XAML rows and append.
            // DemoTable.Rows.Clear();

            AddTableRow("10", "Alice", "30");
            AddTableRow("2", "Bob", "25");
            AddTableRow("1", "Charlie", "35");
            AddTableRow("20", "David", "40", true);
            AddTableRow("3", "Eve", "22");
            AddTableRow("4", "Frank", "28");
            AddTableRow("5", "Grace", "31");
            AddTableRow("6", "Heidi", "24");
            AddTableRow("7", "Ivan", "45");
            AddTableRow("8", "Judy", "33");
            AddTableRow("9", "Mallory", "29");
        }

        // Init ScrollViewer content
        if (ScrollStackPanel != null)
        {
            for (int i = 0; i < 20; i++)
            {
                ScrollStackPanel.AddChild(new Button { Content = $"Button {i} (Wide Content for Scroll)" });
            }
        }

        // Init CodeDoc
        if (CodeDoc != null)
        {
            string sampleCode = @"using System;

public class Test
{
    public void Method()
    {
        Console.WriteLine(""Hello World"");
        int x = 10;
        if (x > 5)
        {
            return;
        }
    }
}";
            CodeDoc.SetCode(sampleCode, "csharp");
        }

        // Init Markdown
        if (MdView != null)
        {
            string mdText = @"# Markdown Demo

This is a **bold** text and *italic* text.
Here is a [Link](http://example.com) and an ![Image](img.png).

## Lists

- Item 1
- Item 2 with **bold**
- Item 3

## Code

```csharp
public void Hello() {
    Console.WriteLine(""World"");
}
```

## Table

| ID | Name |
|---|---|
| 1 | Alice |
| 2 | Bob |

> This is a quote.
";
            MdView.Text = mdText;
        }

        // Init Scroll Events
        if (HScroll != null) HScroll.ValueChanged += OnScrollChanged;
        if (VScroll != null) VScroll.ValueChanged += OnScrollChanged;
        OnScrollChanged(null, null);

        // Init TextEditor
        if (EditorBox != null)
        {
            EditorBox.Text = "Welcome to the TextEditor!\nType here...";
        }
    }

    private void AddTableRow(string id, string name, string age, bool active = false)
    {
        var row = new TableRow { Tag = id };
        row.AddCell(id);
        row.AddCell(name);
        row.AddCell(age);
        if (active)
        {
            row.AddCell(new CheckBox { Content = "Active", IsChecked = true });
        }
        else
        {
            var btn = new Button { Content = "Edit" };
            btn.Click += (s, e) => LogBox?.Items.Add($"Edit Clicked: {name}");
            row.AddCell(btn);
        }
        DemoTable.AddRow(row);
    }

    // Event Handlers
    public void OnNewClick() => LogBox?.Items.Add("New Clicked");
    public void OnOpenClick() => LogBox?.Items.Add("Open Clicked");
    public void OnExitClick() => _app.Stop();
    public void OnCutClick() => LogBox?.Items.Add("Cut Clicked");
    public void OnCopyClick() => LogBox?.Items.Add("Copy Clicked");
    public void OnPasteClick() => LogBox?.Items.Add("Paste Clicked");
    public void OnAboutClick() => LogBox?.Items.Add("About Clicked");

    public void OnSubmit(object sender, RoutedEventArgs e)
    {
        if (LogBox != null && NameBox != null && CountryCombo != null)
        {
            LogBox.Items.Add($"Form Submitted: {NameBox.Text} / {CountryCombo.SelectedItem}");
            LogBox.SelectedIndex = LogBox.Items.Count - 1;
        }
    }

    public void OnShowDialog(object sender, RoutedEventArgs e)
    {
        var dialog = new DialogBox
        {
            Title = "Welcome",
            Width = 40,
            Height = 10,
            BoxStyle = BoxStyle.Double,
            BackgroundColor = ConsoleColor.DarkBlue,
            TitleColor = ConsoleColor.Yellow,
            BorderColor = ConsoleColor.White
        };

        var dialogStack = new StackPanel { Orientation = Orientation.Vertical };
        dialogStack.AddChild(new TextBlock { Text = "This is a modal dialog box.", Foreground = ConsoleColor.White });
        dialogStack.AddChild(new TextBlock { Text = "You can put any controls here.", Foreground = ConsoleColor.Gray });

        var btnClose1 = new Button { Content = "Close", BoxStyle = BoxStyle.Single };
        btnClose1.Click += (s, a) => dialog.Hide();
        var btnClose2 = new Button { Content = "Close", BoxStyle = BoxStyle.Single };
        btnClose2.Click += (s, a) => dialog.Hide();

        var btnContainer = new StackPanel { Orientation = Orientation.Horizontal };
        btnContainer.AddChild(new TextBlock { Text = "   " });
        btnContainer.AddChild(btnClose1);
        btnContainer.AddChild(btnClose2);

        dialogStack.AddChild(new TextBlock { Text = " " }); // Spacer
        dialogStack.AddChild(btnContainer);

        dialog.Content = dialogStack;

        _window.SetOverlay(dialog);
        dialog.Show();
    }

    // Table Edit Row - wired in XAML for rows defined there (if any)
    public void OnEditRow(object sender, RoutedEventArgs e)
    {
        if (sender is UIElement uie && uie.Parent is TableRow row)
        {
            if (row.Cells.Count > 1 && row.Cells[1] is TextBlock tb)
            {
                LogBox?.Items.Add($"Edit Clicked (XAML Row): {tb.Text}");
            }
        }
    }

    public void OnScrollChanged(object sender, EventArgs e)
    {
        if (ScrollLabel != null && HScroll != null && VScroll != null)
            ScrollLabel.Text = $"H: {HScroll.Value}, V: {VScroll.Value}";
    }
}
