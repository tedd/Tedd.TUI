using System;
using System.IO;
using System.Linq;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class FileDialogTests : IDisposable
{
    private readonly string _root;

    public FileDialogTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "TeddTuiFileDialogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "sub1"));
        Directory.CreateDirectory(Path.Combine(_root, "sub2"));
        File.WriteAllText(Path.Combine(_root, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_root, "b.txt"), "b");
        File.WriteAllText(Path.Combine(_root, "c.log"), "c");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Best effort cleanup of the temp tree.
        }
    }

    private static TuiWindow CreateHost(int width = 80, int height = 25)
    {
        var host = new TuiWindow();
        host.Measure(new Size(width, height));
        host.Arrange(new Rect(0, 0, width, height));
        return host;
    }

    private static ActivatableListBox GetList(FileDialog dialog) =>
        (ActivatableListBox)dialog.FindName("FileList");

    private static TextBox GetNameBox(FileDialog dialog) =>
        (TextBox)dialog.FindName("FileNameBox");

    private static void ActivateEntry(FileDialog dialog, string entryName)
    {
        var list = GetList(dialog);
        for (int i = 0; i < list.Items.Count; i++)
        {
            if (list.Items[i] is FileSystemEntry entry && entry.Name == entryName)
            {
                list.SelectedIndex = i;
                list.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Enter });
                return;
            }
        }
        Assert.Fail($"Entry '{entryName}' not found in file list.");
    }

    [Fact]
    public void ParseFilter_PairsAndMultiPatterns()
    {
        var filters = FileDialog.ParseFilter("Text files (*.txt)|*.txt|Images|*.png;*.jpg");
        Assert.Equal(2, filters.Count);
        Assert.Equal("Text files (*.txt)", filters[0].Description);
        Assert.Equal(new[] { "*.txt" }, filters[0].Patterns);
        Assert.Equal(new[] { "*.png", "*.jpg" }, filters[1].Patterns);
    }

    [Fact]
    public void ParseFilter_Empty_YieldsAllFiles()
    {
        var filters = FileDialog.ParseFilter("");
        Assert.Single(filters);
        Assert.Equal(new[] { "*" }, filters[0].Patterns);
    }

    [Fact]
    public void Show_ListsParentDirectoriesAndFilteredFiles()
    {
        var host = CreateHost();
        var dialog = new OpenFileDialog { InitialDirectory = _root, Filter = "Text files|*.txt" };
        dialog.ShowDialog(host);

        var names = GetList(dialog).Items.Cast<FileSystemEntry>().Select(e => e.Name).ToList();

        Assert.Equal("..", names[0]);
        Assert.Contains("sub1", names);
        Assert.Contains("sub2", names);
        Assert.Contains("a.txt", names);
        Assert.Contains("b.txt", names);
        Assert.DoesNotContain("c.log", names);

        // Directories come before files
        Assert.True(names.IndexOf("sub1") < names.IndexOf("a.txt"));
        Assert.Equal(_root, dialog.CurrentDirectory);
    }

    [Fact]
    public void ActivateDirectory_NavigatesIntoIt_AndParentGoesBack()
    {
        var host = CreateHost();
        var dialog = new OpenFileDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        ActivateEntry(dialog, "sub1");
        Assert.Equal(Path.Combine(_root, "sub1"), dialog.CurrentDirectory);

        ActivateEntry(dialog, "..");
        Assert.Equal(_root, dialog.CurrentDirectory);
    }

    [Fact]
    public void ActivateFile_OpenDialog_AcceptsWithFullPath()
    {
        var host = CreateHost();
        var dialog = new OpenFileDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        ActivateEntry(dialog, "a.txt");

        Assert.True(dialog.DialogResult);
        Assert.Equal(Path.Combine(_root, "a.txt"), dialog.FileName);
        Assert.Null(host.Overlay);
    }

    [Fact]
    public void SelectingFile_UpdatesNameBox()
    {
        var host = CreateHost();
        var dialog = new OpenFileDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        var list = GetList(dialog);
        int fileIndex = -1;
        for (int i = 0; i < list.Items.Count; i++)
        {
            if (list.Items[i] is FileSystemEntry { Name: "b.txt" }) { fileIndex = i; break; }
        }
        list.SelectedIndex = fileIndex;

        Assert.Equal("b.txt", GetNameBox(dialog).Text);
    }

    [Fact]
    public void Accept_MissingFile_WithCheckFileExists_StaysOpen()
    {
        var host = CreateHost();
        var dialog = new OpenFileDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        GetNameBox(dialog).Text = "does-not-exist.txt";
        dialog.Accept();

        Assert.Null(dialog.DialogResult);
        Assert.Equal(dialog, host.Overlay);
    }

    [Fact]
    public void Accept_TypedDirectoryName_NavigatesInstead()
    {
        var host = CreateHost();
        var dialog = new OpenFileDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        GetNameBox(dialog).Text = "sub2";
        dialog.Accept();

        Assert.Null(dialog.DialogResult);
        Assert.Equal(Path.Combine(_root, "sub2"), dialog.CurrentDirectory);
    }

    [Fact]
    public void PathBox_Enter_NavigatesToTypedPath()
    {
        var host = CreateHost();
        var dialog = new OpenFileDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        var pathBox = (TextBox)dialog.FindName("PathBox");
        pathBox.Text = Path.Combine(_root, "sub1");
        host.SetFocus(pathBox);
        host.ProcessKey(new KeyEventArgs(UIElement.KeyDownEvent, pathBox) { Key = ConsoleKey.Enter });

        Assert.Equal(Path.Combine(_root, "sub1"), dialog.CurrentDirectory);
    }

    [Fact]
    public void FilterChange_RefreshesFileList()
    {
        var host = CreateHost();
        var dialog = new OpenFileDialog
        {
            InitialDirectory = _root,
            Filter = "Text files|*.txt|Log files|*.log"
        };
        dialog.ShowDialog(host);

        var combo = (ComboBox)dialog.FindName("FilterCombo");
        combo.SelectedIndex = 1;

        var names = GetList(dialog).Items.Cast<FileSystemEntry>().Select(e => e.Name).ToList();
        Assert.Contains("c.log", names);
        Assert.DoesNotContain("a.txt", names);
        Assert.Equal(1, dialog.FilterIndex);
    }

    [Fact]
    public void CancelButton_ClosesWithFalse()
    {
        var host = CreateHost();
        var dialog = new OpenFileDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        var cancel = (Button)dialog.FindName("CancelButton");
        cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, cancel));

        Assert.False(dialog.DialogResult);
        Assert.Null(host.Overlay);
    }

    [Fact]
    public void SaveDialog_AppendsDefaultExtension()
    {
        var host = CreateHost();
        var dialog = new SaveFileDialog { InitialDirectory = _root, DefaultExt = "txt" };
        dialog.ShowDialog(host);

        GetNameBox(dialog).Text = "newfile";
        dialog.Accept();

        Assert.True(dialog.DialogResult);
        Assert.Equal(Path.Combine(_root, "newfile.txt"), dialog.FileName);
    }

    [Fact]
    public void SaveDialog_ActivatingFile_TakesNameWithoutClosing()
    {
        var host = CreateHost();
        var dialog = new SaveFileDialog { InitialDirectory = _root, OverwritePrompt = false };
        dialog.ShowDialog(host);

        ActivateEntry(dialog, "a.txt");

        Assert.Null(dialog.DialogResult);
        Assert.Equal("a.txt", GetNameBox(dialog).Text);
        Assert.Equal(dialog, host.Overlay);
    }

    [Fact]
    public void SaveDialog_OverwritePrompt_YesAccepts()
    {
        var host = CreateHost();
        var dialog = new SaveFileDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        GetNameBox(dialog).Text = "a.txt";
        dialog.Accept();

        // Confirmation dialog is now the top overlay.
        var confirm = Assert.IsType<MessageDialog>(host.Overlay);
        var yes = (Button)confirm.FindName("YesButton");
        yes.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, yes));

        Assert.True(dialog.DialogResult);
        Assert.Equal(Path.Combine(_root, "a.txt"), dialog.FileName);
        Assert.Null(host.Overlay);
    }

    [Fact]
    public void SaveDialog_OverwritePrompt_NoKeepsDialogOpen()
    {
        var host = CreateHost();
        var dialog = new SaveFileDialog { InitialDirectory = _root };
        dialog.ShowDialog(host);

        GetNameBox(dialog).Text = "a.txt";
        dialog.Accept();

        var confirm = Assert.IsType<MessageDialog>(host.Overlay);
        var no = (Button)confirm.FindName("NoButton");
        no.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, no));

        Assert.Null(dialog.DialogResult);
        Assert.Equal(dialog, host.Overlay);
    }

    [Fact]
    public void SaveDialog_NoOverwritePrompt_AcceptsDirectly()
    {
        var host = CreateHost();
        var dialog = new SaveFileDialog { InitialDirectory = _root, OverwritePrompt = false };
        dialog.ShowDialog(host);

        GetNameBox(dialog).Text = "a.txt";
        dialog.Accept();

        Assert.True(dialog.DialogResult);
        Assert.Equal(Path.Combine(_root, "a.txt"), dialog.FileName);
    }

    [Fact]
    public void PresetFileName_PrefillsNameBox()
    {
        var host = CreateHost();
        var dialog = new SaveFileDialog { InitialDirectory = _root, FileName = Path.Combine("somewhere", "preset.txt") };
        dialog.ShowDialog(host);

        Assert.Equal("preset.txt", GetNameBox(dialog).Text);
    }

    [Fact]
    public void ActivatableListBox_DoubleClick_Activates()
    {
        var list = new ActivatableListBox { Width = 20, Height = 5 };
        list.Items.Add("one");
        list.Items.Add("two");
        list.Measure(new Size(20, 5));
        list.Arrange(new Rect(0, 0, 20, 5));

        int activated = 0;
        list.ItemActivated += (s, e) => activated++;

        // Two clicks on the same row within the double-click window
        list.OnMouseDown(new MouseEventArgs { X = 2, Y = 1 });
        Assert.Equal(0, activated);
        list.OnMouseDown(new MouseEventArgs { X = 2, Y = 1 });
        Assert.Equal(1, activated);
    }
}
