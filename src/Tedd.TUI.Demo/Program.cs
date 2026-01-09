using System;
using System.Collections.Generic;
using Tedd.TUI;
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

        // Header
        mainStack.AddChild(new Border
        {
            Child = new TextBlock { Text = "Tedd.TUI Demo Application (.NET 10)", Foreground = ConsoleColor.Cyan },
            BorderStyle = BorderStyle.Double
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

        // Submit Button (Moved down so it can access logBox)
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };
        var btnSubmit = new Button { Content = "Submit" };
        btnSubmit.Click += (s, e) => {
            logBox.Items.Add($"Form Submitted: {nameBox.Text} / {countryCombo.SelectedItem}");
            logBox.SelectedIndex = logBox.Items.Count - 1;
        };
        btnPanel.AddChild(btnSubmit);
        formStack.AddChild(btnPanel);

        tabs.AddItem(new TabItem { Header = "Form", Content = formStack });

        // --- Tab 2: Lists & Progress ---
        var listStack = new StackPanel { Orientation = Orientation.Vertical };

        listStack.AddChild(new TextBlock { Text = "Progress:" });
        var progressBar = new ProgressBar { Width = 40, Value = 35 };
        listStack.AddChild(progressBar);

        listStack.AddChild(new TextBlock { Text = "Items:" });
        var listBox = new ListBox { Width = 40, Height = 10 };
        for(int i=1; i<=20; i++) listBox.Items.Add($"Item {i}");
        listStack.AddChild(listBox);

        tabs.AddItem(new TabItem { Header = "Lists", Content = listStack });

        // Run App
        var app = new TuiApp(window);

        // Manual hook for logging for now since we don't have a global message bus
        // In a real app we'd bind or use events.

        System.Console.CancelKeyPress += (s, e) => {
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
