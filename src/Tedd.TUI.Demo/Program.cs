using Tedd.TUI.Platform.Console;
using Tedd.TUI.CodeColoring;
using Tedd.TUI.Markdown;
using System.IO;
using System.Linq;

namespace Tedd.TUI.Demo;

class Program
{
    static void Main(string[] args)
    {
        bool useXaml = false;
        if (args.Length == 0)
        {
            System.Console.WriteLine("Select Demo Mode:");
            System.Console.WriteLine("1. Programmatic (Code)");
            System.Console.WriteLine("2. XAML");
            System.Console.Write("Enter choice (1/2): ");
            var key = System.Console.ReadKey().KeyChar;
            System.Console.WriteLine();

            if (key == '2') useXaml = true;
        }
        else if (args.Contains("--xaml"))
        {
            useXaml = true;
        }

        if (useXaml)
        {
            RunXamlDemo();
        }
        else
        {
            RunCodeDemo();
        }
    }

    static void RunXamlDemo()
    {
        // Load XAML
        string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demo.xaml");
        if (!File.Exists(xamlPath))
        {
            System.Console.WriteLine($"Error: demo.xaml not found at {xamlPath}");
            return;
        }
        string xaml = File.ReadAllText(xamlPath);

        var controller = new DemoController();
        // Load returns the Window defined in XAML
        var loadedWindow = (TuiWindow)XamlLoader.Load(xaml, controller);

        var app = new TuiApp(loadedWindow);

        // Initialize controller
        controller.Initialize(app, loadedWindow);

        System.Console.CancelKeyPress += (s, e) =>
        {
            app.Stop();
            e.Cancel = true;
        };

        try
        {
            app.Run();
        }
        finally
        {
            app.Stop();
        }
    }

    static void RunCodeDemo()
    {
        var window = new TuiWindow();
        var app = new TuiApp(window);

        // Root Layout
        var mainStack = new StackPanel { Orientation = Orientation.Vertical };
        window.Content = mainStack;

        // --- Log Output (Created early for menu actions) ---
        var logBox = new ListBox { Width = 80, Height = 5 };

        // Menu Bar
        var menuBar = new MenuBar();
        mainStack.AddChild(menuBar);

        // File Menu
        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File ", Foreground = ConsoleColor.Black } };
        fileMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "New ", Foreground = ConsoleColor.Black }, Command = () => logBox.Items.Add("New Clicked") });
        fileMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "Open ", Foreground = ConsoleColor.Black }, Command = () => logBox.Items.Add("Open Clicked") });
        fileMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "Exit ", Foreground = ConsoleColor.Black }, Command = () => app.Stop() });
        menuBar.AddChild(fileMenu);

        // Edit Menu
        var editMenu = new MenuItem { Header = new TextBlock { Text = "Edit ", Foreground = ConsoleColor.Black } };
        editMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "Cut ", Foreground = ConsoleColor.Black }, Command = () => logBox.Items.Add("Cut Clicked") });
        editMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "Copy ", Foreground = ConsoleColor.Black }, Command = () => logBox.Items.Add("Copy Clicked") });
        editMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "Paste ", Foreground = ConsoleColor.Black }, Command = () => logBox.Items.Add("Paste Clicked") });
        menuBar.AddChild(editMenu);

        // Help Menu
        var helpMenu = new MenuItem { Header = new TextBlock { Text = "Help ", Foreground = ConsoleColor.Black } };
        helpMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "About ", Foreground = ConsoleColor.Black }, Command = () => logBox.Items.Add("About Clicked") });
        menuBar.AddChild(helpMenu);

        // Header (double-line box)
        mainStack.AddChild(new Border
        {
            Child = new TextBlock { Text = "Tedd.TUI Demo Application (.NET 10)", Foreground = ConsoleColor.Cyan },
            BoxStyle = BoxStyle.Double
        });


        // --- Bifurcated Architecture ---
        var dockPanel = new DockPanel { LastChildFill = true };
        mainStack.AddChild(dockPanel);

        // Navigation Pane (Left)
        var navExpander = new Expander { Header = "Navigation", IsExpanded = true, Width = 20 };
        DockPanel.SetDock(navExpander, Dock.Left);

        var navTree = new TreeView();

        var rootNode = new TreeViewItem { Header = "Controls", IsExpanded = true };

        var nodeForm = new TreeViewItem { Header = "Form", IsSelected = true };
        var nodeLists = new TreeViewItem { Header = "Lists" };
        var nodeTable = new TreeViewItem { Header = "Table" };
        var nodeScroll = new TreeViewItem { Header = "Scroll" };
        var nodeCode = new TreeViewItem { Header = "Code" };
        var nodeMarkdown = new TreeViewItem { Header = "Markdown" };
        var nodeDataGrid = new TreeViewItem { Header = "DataGrid" };
        var nodeEditor = new TreeViewItem { Header = "Editor" };

        rootNode.Items.Add(nodeForm);
        rootNode.Items.Add(nodeLists);
        rootNode.Items.Add(nodeTable);
        rootNode.Items.Add(nodeScroll);
        rootNode.Items.Add(nodeCode);
        rootNode.Items.Add(nodeMarkdown);
        rootNode.Items.Add(nodeDataGrid);
        rootNode.Items.Add(nodeEditor);

        navTree.Items.Add(rootNode);
        navExpander.Content = navTree;
        dockPanel.Children.Add(navExpander);

        // Content Matrix (Center)
        var tabs = new TabControl { Height = 20 };
        // We do not add tabs to mainStack, we add it to dockPanel
        dockPanel.Children.Add(tabs);

        // Wire navigation
        navTree.SelectionChanged += (s, e) =>
        {
            var sel = navTree.SelectedItem as TreeViewItem;
            if (sel == nodeForm) tabs.SelectedIndex = 0;
            else if (sel == nodeLists) tabs.SelectedIndex = 1;
            else if (sel == nodeTable) tabs.SelectedIndex = 2;
            else if (sel == nodeScroll) tabs.SelectedIndex = 3;
            else if (sel == nodeCode) tabs.SelectedIndex = 4;
            else if (sel == nodeMarkdown) tabs.SelectedIndex = 5;
            else if (sel == nodeDataGrid) tabs.SelectedIndex = 6;
            else if (sel == nodeEditor) tabs.SelectedIndex = 7;
        };


        // --- Tab 1: Form Controls ---
        var formStack = new StackPanel { Orientation = Orientation.Vertical };

        // Name Input
        formStack.AddChild(new TextBlock { Text = "Name:" });
        var nameBox = new TextBox { Width = 30, Text = "John Doe" };
        formStack.AddChild(nameBox);

        // Password Input
        formStack.AddChild(new TextBlock { Text = "Password:" });
        var passBox = new PasswordBox { Width = 30, Password = "secret" };
        formStack.AddChild(passBox);

        // Slider
        formStack.AddChild(new TextBlock { Text = "Volume:" });
        var volumeSlider = new Slider { Width = 20, Minimum = 0, Maximum = 100, Value = 50 };
        formStack.AddChild(volumeSlider);

        // CheckBox
        var termsCheck = new CheckBox { Content = "I agree to Terms & Conditions" };
        formStack.AddChild(termsCheck);

        // Radio Buttons
        formStack.AddChild(new TextBlock { Text = "Gender:" });
        var radioGroup = new StackPanel { Orientation = Orientation.Horizontal };
        radioGroup.AddChild(new RadioButton { Content = "Male", GroupName = "Gender", IsChecked = true });
        radioGroup.AddChild(new RadioButton { Content = "Female", GroupName = "Gender" });
        radioGroup.AddChild(new RadioButton { Content = "Other", GroupName = "Gender" });
        formStack.AddChild(radioGroup);

        // ComboBox
        formStack.AddChild(new TextBlock { Text = "Country:" });
        var countryCombo = new ComboBox { Width = 20 };
        countryCombo.Items.Add("USA");
        countryCombo.Items.Add("Canada");
        countryCombo.Items.Add("UK");
        countryCombo.Items.Add("Germany");
        countryCombo.Items.Add("France");
        countryCombo.SelectedItem = "USA";
        formStack.AddChild(countryCombo);

        // --- Log Output ---
        mainStack.AddChild(new TextBlock { Text = "--- Log ---", Foreground = ConsoleColor.DarkGray });
        // logBox already created above
        mainStack.AddChild(logBox);

        // Submit Button (double-line style to showcase both)
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };
        var btnSubmit = new Button { Content = "Submit", BoxStyle = BoxStyle.Double };
        btnSubmit.Click += (s, e) =>
        {
            logBox.Items.Add($"Form Submitted: {nameBox.Text} / {countryCombo.SelectedItem}");
            logBox.SelectedIndex = logBox.Items.Count - 1;
        };
        btnPanel.AddChild(btnSubmit);

        // Dialog Box Demo
        var btnDialog = new Button { Content = "Show Dialog", BoxStyle = BoxStyle.Single };
        btnDialog.Click += (s, e) =>
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
            btnClose1.Click += (sender, args) => dialog.Hide();
            var btnClose2 = new Button { Content = "Close", BoxStyle = BoxStyle.Single };
            btnClose2.Click += (sender, args) => dialog.Hide();

            // Center the button a bit (simple filler for now)
            var btnContainer = new StackPanel { Orientation = Orientation.Horizontal };
            btnContainer.AddChild(new TextBlock { Text = "   " });
            btnContainer.AddChild(btnClose1);
            btnContainer.AddChild(btnClose2);

            dialogStack.AddChild(new TextBlock { Text = " " }); // Spacer
            dialogStack.AddChild(btnContainer);

            dialog.Content = dialogStack;

            window.PushOverlay(dialog);
            dialog.Show();
        };
        btnPanel.AddChild(new TextBlock { Text = "  " }); // Spacer
        btnPanel.AddChild(btnDialog);

        formStack.AddChild(btnPanel);

        tabs.Items.Add(new TabItem { Header = "Form", Content = formStack });

        // --- Tab 2: Lists & Progress ---
        var listStack = new StackPanel { Orientation = Orientation.Vertical };

        listStack.AddChild(new TextBlock { Text = "Progress:" });
        var progressBar1 = new ProgressBar { Width = 40, Value = 35, LabelMode = ProgressBarLabelMode.Percent, LabelPercentDecimals = 0 };
        listStack.AddChild(progressBar1);
        var progressBar2 = new ProgressBar { Width = 40, Value = 75, LabelMode = ProgressBarLabelMode.Percent, LabelPercentDecimals = 1, ProgressColor = ConsoleColor.Blue };
        listStack.AddChild(progressBar2);
        var progressBar3 = new ProgressBar { Width = 40, Value = 50, LabelMode = ProgressBarLabelMode.Text, LabelText = "Loading...", ProgressColor = ConsoleColor.Red, LabelFilledColor = ConsoleColor.Yellow };
        listStack.AddChild(progressBar3);

        listStack.AddChild(new TextBlock { Text = "Items:" });
        // listBox already created
        var listBoxList = new ListBox { Width = 40, Height = 10 };
        for (int i = 1; i <= 20; i++) listBoxList.Items.Add($"Item {i}");
        listStack.AddChild(listBoxList);

        tabs.Items.Add(new TabItem { Header = "Lists", Content = listStack });

        // --- Tab 3: Table ---
        var tableStack = new StackPanel { Orientation = Orientation.Vertical };
        tableStack.AddChild(new TextBlock { Text = "Table Control:" });

        var table = new Table { Width = 60, Height = 10, ShowHeader = true, PageSize = 5 }; // Enable Pagination

        // 1. Numeric Sort for ID
        var colId = new TableColumn { Header = "ID", Width = GridLength.Pixel(5) };
        colId.SortComparer = (a, b) =>
        {
            if (int.TryParse(a.ToString(), out int i1) && int.TryParse(b.ToString(), out int i2))
                return i1.CompareTo(i2);
            return 0;
        };
        table.Columns.Add(colId);

        // 2. Default String Sort for Name (no custom comparer needed)
        table.Columns.Add(new TableColumn { Header = "Name", Width = GridLength.Star });

        // 3. Numeric Sort for Age
        var colAge = new TableColumn { Header = "Age", Width = GridLength.Auto };
        colAge.SortComparer = (a, b) =>
        {
            if (int.TryParse(a.ToString(), out int i1) && int.TryParse(b.ToString(), out int i2))
                return i1.CompareTo(i2);
            return 0;
        };
        table.Columns.Add(colAge);

        // 4. Custom key selector example for Actions (sort by length of content?)
        table.Columns.Add(new TableColumn { Header = "Actions", Width = GridLength.Pixel(15) });

        // Helper to create Edit Button
        UIElement CreateEditBtn(string name)
        {
            var btn = new Button { Content = "Edit" };
            btn.Click += (s, e) => logBox.Items.Add($"Edit Clicked: {name}");
            return btn;
        }

        // Add Rows with unsorted IDs to demonstrate sorting
        table.AddRow("10", "Alice", "30", CreateEditBtn("Alice"));
        table.AddRow("2", "Bob", "25", CreateEditBtn("Bob"));
        table.AddRow("1", "Charlie", "35", CreateEditBtn("Charlie"));
        table.AddRow("20", "David", "40", new CheckBox { Content = "Active", IsChecked = true });
        table.AddRow("3", "Eve", "22", CreateEditBtn("Eve"));

        // Add more rows for pagination testing
        table.AddRow("4", "Frank", "28", CreateEditBtn("Frank"));
        table.AddRow("5", "Grace", "31", CreateEditBtn("Grace"));
        table.AddRow("6", "Heidi", "24", CreateEditBtn("Heidi"));
        table.AddRow("7", "Ivan", "45", CreateEditBtn("Ivan"));
        table.AddRow("8", "Judy", "33", CreateEditBtn("Judy"));
        table.AddRow("9", "Mallory", "29", CreateEditBtn("Mallory"));

        tableStack.AddChild(table);
        tabs.Items.Add(new TabItem { Header = "Table", Content = tableStack });

        // --- Tab 4: ScrollBar & ScrollViewer ---
        var scrollStack = new StackPanel { Orientation = Orientation.Vertical };
        scrollStack.AddChild(new TextBlock { Text = "ScrollBars Demo" });

        var scrollLabel = new TextBlock { Text = "H: 0, V: 0" };
        scrollStack.AddChild(scrollLabel);

        // Horizontal
        scrollStack.AddChild(new TextBlock { Text = "Horizontal:" });
        var hScroll = new ScrollBar
        {
            Orientation = Orientation.Horizontal,
            Width = 40,
            Minimum = 0,
            Maximum = 100,
            ViewportSize = 20,
            Value = 30
        };
        scrollStack.AddChild(hScroll);

        // Vertical
        scrollStack.AddChild(new TextBlock { Text = "Vertical:" });
        var vScroll = new ScrollBar
        {
            Orientation = Orientation.Vertical,
            Height = 10,
            Minimum = 0,
            Maximum = 50,
            ViewportSize = 10
        };
        scrollStack.AddChild(vScroll);

        scrollStack.AddChild(new TextBlock { Text = "ScrollViewer:" });
        var sv = new ScrollViewer { Width = 40, Height = 10, HorizontalScrollBarVisibility = true, VerticalScrollBarVisibility = true };
        var largeStack = new StackPanel { Orientation = Orientation.Vertical };
        for (int i = 0; i < 20; i++)
        {
            largeStack.AddChild(new Button { Content = $"Button {i} (Wide Content for Scroll)" });
        }
        sv.Content = largeStack;
        scrollStack.AddChild(sv);

        // Event Handlers
        void UpdateLabel(object s, EventArgs e)
        {
            scrollLabel.Text = $"H: {hScroll.Value}, V: {vScroll.Value}";
        }

        hScroll.ValueChanged += UpdateLabel;
        vScroll.ValueChanged += UpdateLabel;

        // Initial update
        UpdateLabel(null, EventArgs.Empty);

        tabs.Items.Add(new TabItem { Header = "Scroll", Content = scrollStack });

        // --- Tab 5: Code Coloring ---
        var codeStack = new StackPanel { Orientation = Orientation.Vertical };
        codeStack.AddChild(new TextBlock { Text = "Code Coloring Demo (C#)" });

        var codeDoc = new CodeDocument();
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
        codeDoc.SetCode(sampleCode, "csharp");

        var scrollCode = new ScrollViewer { Width = 70, Height = 15, VerticalScrollBarVisibility = true, HorizontalScrollBarVisibility = true };
        scrollCode.Content = codeDoc;

        codeStack.AddChild(scrollCode);

        tabs.Items.Add(new TabItem { Header = "Code", Content = codeStack });

        // --- Tab 6: Markdown ---
        var mdStack = new StackPanel { Orientation = Orientation.Vertical };
        mdStack.AddChild(new TextBlock { Text = "Markdown View:" });

        var mdScrollViewer = new ScrollViewer { Width = 70, Height = 15, VerticalScrollBarVisibility = true };
        var mdView = new MarkdownView();
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
        mdView.Text = mdText;
        mdScrollViewer.Content = mdView;
        mdStack.AddChild(mdScrollViewer);

        tabs.Items.Add(new TabItem { Header = "Markdown", Content = mdStack });

        // --- Tab 7: DataGrid ---
        var dataGridStack = new StackPanel { Orientation = Orientation.Vertical };
        dataGridStack.AddChild(new TextBlock { Text = "DataGrid (Auto Generated Columns):" });

        var dataGrid = new DataGrid
        {
            Width = 60,
            Height = 10,
            ShowHeader = true,
            PageSize = 5,
            AutoGenerateColumns = true,
            ShowBorder = true,
            BorderStyle = BoxStyle.Single
        };

        var people = new List<Person>
        {
            new Person { Id = 1, Name = "Alice", Age = 30, Role = "Dev" },
            new Person { Id = 2, Name = "Bob", Age = 25, Role = "QA" },
            new Person { Id = 3, Name = "Charlie", Age = 35, Role = "Manager" },
            new Person { Id = 4, Name = "Dave", Age = 40, Role = "Dev" },
            new Person { Id = 5, Name = "Eve", Age = 22, Role = "Intern" },
            new Person { Id = 6, Name = "Frank", Age = 28, Role = "Dev" },
        };
        dataGrid.ItemsSource = people;
        dataGridStack.AddChild(dataGrid);

        tabs.Items.Add(new TabItem { Header = "DataGrid", Content = dataGridStack });

        // --- Tab 8: Editor ---
        var editorStack = new StackPanel { Orientation = Orientation.Vertical };
        editorStack.AddChild(new TextBlock { Text = "Text Editor:" });

        var editorBorder = new Border { BoxStyle = BoxStyle.Single, BorderColor = ConsoleColor.Cyan, Width = 70, Height = 15 };
        var editorBox = new TextEditor { Width = -1, Height = -1 };
        editorBorder.Child = editorBox;

        editorStack.AddChild(editorBorder);
        tabs.Items.Add(new TabItem { Header = "Editor", Content = editorStack });

        // Run App

        System.Console.CancelKeyPress += (s, e) =>
        {
            app.Stop();
            e.Cancel = true;
        };

        try
        {
            app.Run();
        }
        finally
        {
            app.Stop();
        }
    }
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public string Role { get; set; }
}
