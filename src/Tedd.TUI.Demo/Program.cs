using Tedd.TUI.Platform.Console;

namespace Tedd.TUI.Demo;

class Program
{
    static void Main(string[] args)
    {
        var window = new TuiWindow();

        // Root Layout
        var mainStack = new StackPanel { Orientation = Orientation.Vertical };
        window.Content = mainStack;

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
        var logBox = new ListBox { Width = 80, Height = 5 };
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

        // Run App
        var app = new TuiApp(window);

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
