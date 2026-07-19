using System;
using System.IO;
using System.Linq;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class FolderBrowserDialogTests : IDisposable
{
    private readonly string _root;

    public FolderBrowserDialogTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "TeddTuiFolderDialogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "alpha"));
        Directory.CreateDirectory(Path.Combine(_root, "beta"));
        File.WriteAllText(Path.Combine(_root, "file.txt"), "x");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    private static TuiWindow CreateHost()
    {
        var host = new TuiWindow();
        host.Measure(new Size(80, 25));
        host.Arrange(new Rect(0, 0, 80, 25));
        return host;
    }

    private static ActivatableListBox GetList(FolderBrowserDialog dialog) =>
        (ActivatableListBox)dialog.FindName("FolderList");

    private static void SelectEntry(FolderBrowserDialog dialog, string name)
    {
        var list = GetList(dialog);
        for (int i = 0; i < list.Items.Count; i++)
        {
            if (list.Items[i] is FileSystemEntry entry && entry.Name == name)
            {
                list.SelectedIndex = i;
                return;
            }
        }
        Assert.Fail($"Entry '{name}' not found in folder list.");
    }

    [Fact]
    public void Show_ListsOnlyDirectories()
    {
        var host = CreateHost();
        var dialog = new FolderBrowserDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        var names = GetList(dialog).Items.Cast<FileSystemEntry>().Select(e => e.Name).ToList();

        Assert.Equal("..", names[0]);
        Assert.Contains("alpha", names);
        Assert.Contains("beta", names);
        Assert.DoesNotContain("file.txt", names);
    }

    [Fact]
    public void ActivateFolder_NavigatesIntoIt()
    {
        var host = CreateHost();
        var dialog = new FolderBrowserDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        SelectEntry(dialog, "alpha");
        GetList(dialog).OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Enter });

        Assert.Equal(Path.Combine(_root, "alpha"), dialog.CurrentDirectory);
    }

    [Fact]
    public void Accept_HighlightedFolder_BecomesSelectedPath()
    {
        var host = CreateHost();
        var dialog = new FolderBrowserDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        SelectEntry(dialog, "beta");
        dialog.Accept();

        Assert.True(dialog.DialogResult);
        Assert.Equal(Path.Combine(_root, "beta"), dialog.SelectedPath);
        Assert.Null(host.Overlay);
    }

    [Fact]
    public void Accept_ParentEntryHighlighted_SelectsCurrentDirectory()
    {
        var host = CreateHost();
        var dialog = new FolderBrowserDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        SelectEntry(dialog, "..");
        dialog.Accept();

        Assert.Equal(_root, dialog.SelectedPath);
    }

    [Fact]
    public void PresetSelectedPath_OpensThere()
    {
        var host = CreateHost();
        var dialog = new FolderBrowserDialog { SelectedPath = Path.Combine(_root, "alpha") };
        dialog.ShowDialog(host);

        Assert.Equal(Path.Combine(_root, "alpha"), dialog.CurrentDirectory);
    }

    [Fact]
    public void NewFolderButton_HiddenWhenDisabled()
    {
        var host = CreateHost();
        var dialog = new FolderBrowserDialog { InitialDirectory = _root, ShowNewFolderButton = false };
        dialog.ShowDialog(host);

        Assert.Null(dialog.FindName("NewFolderButton"));
    }

    [Fact]
    public void NewFolder_PromptsAndCreatesDirectory()
    {
        var host = CreateHost();
        var dialog = new FolderBrowserDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        var newFolderButton = (Button)dialog.FindName("NewFolderButton");
        newFolderButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, newFolderButton));

        // The InputDialog is now the top overlay; type a name and accept.
        var prompt = Assert.IsType<InputDialog>(host.Overlay);
        var inputBox = (TextBox)prompt.FindName("InputBox");
        inputBox.Text = "gamma";
        prompt.Accept();

        Assert.True(Directory.Exists(Path.Combine(_root, "gamma")));
        // Back on the folder dialog with the new folder selected.
        Assert.Equal(dialog, host.Overlay);
        var selected = Assert.IsType<FileSystemEntry>(GetList(dialog).SelectedItem);
        Assert.Equal("gamma", selected.Name);
    }

    [Fact]
    public void NewFolder_CancelledPrompt_CreatesNothing()
    {
        var host = CreateHost();
        var dialog = new FolderBrowserDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        int before = Directory.GetDirectories(_root).Length;

        var newFolderButton = (Button)dialog.FindName("NewFolderButton");
        newFolderButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, newFolderButton));

        var prompt = Assert.IsType<InputDialog>(host.Overlay);
        prompt.Close(false);

        Assert.Equal(before, Directory.GetDirectories(_root).Length);
    }
}

public class InputDialogTests
{
    private static TuiWindow CreateHost()
    {
        var host = new TuiWindow();
        host.Measure(new Size(80, 25));
        host.Arrange(new Rect(0, 0, 80, 25));
        return host;
    }

    [Fact]
    public void Show_PrefillsInputAndFocusesBox()
    {
        var host = CreateHost();
        var dialog = InputDialog.Show(host, "Enter name:", "Prompt", "initial");

        var inputBox = (TextBox)dialog.FindName("InputBox");
        Assert.Equal("initial", inputBox.Text);
        Assert.True(inputBox.IsFocused);
    }

    [Fact]
    public void Accept_ReturnsTypedText()
    {
        var host = CreateHost();
        string? result = "unset";
        var dialog = InputDialog.Show(host, "Enter name:", onClosed: r => result = r);

        var inputBox = (TextBox)dialog.FindName("InputBox");
        inputBox.Text = "hello";
        dialog.Accept();

        Assert.Equal("hello", result);
        Assert.Equal("hello", dialog.Input);
        Assert.True(dialog.DialogResult);
    }

    [Fact]
    public void EnterInInputBox_Accepts()
    {
        var host = CreateHost();
        var dialog = InputDialog.Show(host, "Enter name:");

        var inputBox = (TextBox)dialog.FindName("InputBox");
        inputBox.Text = "typed";
        host.ProcessKey(new KeyEventArgs(UIElement.KeyDownEvent, inputBox) { Key = ConsoleKey.Enter });

        Assert.True(dialog.DialogResult);
        Assert.Equal("typed", dialog.Input);
    }

    [Fact]
    public void Cancel_ReportsNull()
    {
        var host = CreateHost();
        string? result = "unset";
        var dialog = InputDialog.Show(host, "Enter name:", onClosed: r => result = r);

        var cancel = (Button)dialog.FindName("CancelButton");
        cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, cancel));

        Assert.Null(result);
        Assert.False(dialog.DialogResult);
    }
}
