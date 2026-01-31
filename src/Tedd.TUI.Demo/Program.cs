using Tedd.TUI.Platform.Console;

namespace Tedd.TUI.Demo;

class Program
{
    static void Main(string[] args)
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

        // Tab Control
        var tabs = new TabControl { Height = 20 };
        mainStack.AddChild(tabs);

        // --- Tab 1: Form Controls ---
        var formStack = new StackPanel { Orientation = Orientation.Vertical };

        // Name Input
        formStack.AddChild(new TextBlock { Text = "Name:" });
        var nameBox = new TextBox { Width = 30, Text = "John Doe" };
        formStack.AddChild(nameBox);

        // Password Input
        formStack.AddChild(new TextBlock { Text = "Password:" });
        var passBox = new TextBox { Width = 30, IsPassword = true, Text = "secret" };
        formStack.AddChild(passBox);

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

            // We need to add it to the window overlay. 
            // In a real app we might have a better way, but for now we manually use SetOverlay via a helper or direct access?
            // TuiWindow has SetOverlay but it's public. We need access to 'window' variable.
            // Since we are inside main, we have 'window'.

            // BUT wait, DialogBox.Show/Hide methods rely on Visibility property. 
            // TuiWindow's overlay mechanism is: SetOverlay(UIElement).
            // DialogBox.Show() just sets Visibility=true. It doesn't attach itself to the window if not already there.
            // The current DialogBox implementation expects to be placed somewhere. 
            // If we want it to be a true modal overlay handled by TuiWindow, we need to pass it to window.SetOverlay.

            // Let's adjust how we use it here.
            window.SetOverlay(dialog);
            dialog.Show();
        };
        btnPanel.AddChild(new TextBlock { Text = "  " }); // Spacer
        btnPanel.AddChild(btnDialog);

        formStack.AddChild(btnPanel);

        tabs.AddItem(new TabItem { Header = "Form", Content = formStack });

        // --- Tab 2: Lists & Progress ---
        var listStack = new StackPanel { Orientation = Orientation.Vertical };

        listStack.AddChild(new TextBlock { Text = "Progress:" });
        var progressBar = new ProgressBar { Width = 40, Value = 35 };
        listStack.AddChild(progressBar);

        listStack.AddChild(new TextBlock { Text = "Items:" });
        var listBox = new ListBox { Width = 40, Height = 10 };
        for (int i = 1; i <= 20; i++) listBox.Items.Add($"Item {i}");
        listStack.AddChild(listBox);

        tabs.AddItem(new TabItem { Header = "Lists", Content = listStack });

        // --- Tab 3: Table ---
        var tableStack = new StackPanel { Orientation = Orientation.Vertical };
        tableStack.AddChild(new TextBlock { Text = "Table Control:" });
        
        var table = new Table { Width = 60, Height = 10, ShowHeader = true };
        
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
        // Or just leave unsortable or default string.
        table.Columns.Add(new TableColumn { Header = "Actions", Width = GridLength.Pixel(10) });

        var btnRemove = new Button { Content = "Del" };
        
        // Add Rows with unsorted IDs to demonstrate sorting
        table.AddRow("10", "Alice", "30", "Edit");
        table.AddRow("2", "Bob", "25", "Edit");
        table.AddRow("1", "Charlie", "35", "Edit");
        table.AddRow("20", "David", "40", new CheckBox { Content = "Active", IsChecked = true });
        table.AddRow("3", "Eve", "22", "Edit");

        tableStack.AddChild(table);
        tabs.AddItem(new TabItem { Header = "Table", Content = tableStack });

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

        tabs.AddItem(new TabItem { Header = "Scroll", Content = scrollStack });

        // Run App
        
        // Manual hook for logging for now since we don't have a global message bus
        // In a real app we'd bind or use events.

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
