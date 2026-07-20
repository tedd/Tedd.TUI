using System;
using System.Collections.Generic;
using System.IO;

namespace Tedd.TUI;

/// <summary>
/// An entry shown in the list of a <see cref="FileDialog"/> or
/// <see cref="FolderBrowserDialog"/>.
/// </summary>
public sealed class FileSystemEntry
{
    public string Name { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public bool IsDirectory { get; init; }
    /// <summary>True for the ".." parent-directory entry.</summary>
    public bool IsParent { get; init; }

    public override string ToString() =>
        IsParent ? ".." : IsDirectory ? Name + Path.DirectorySeparatorChar : Name;
}

/// <summary>
/// A <see cref="ListBox"/> that raises <see cref="ItemActivated"/> when an item
/// is chosen with Enter or a double-click, used by the file/folder dialogs.
/// </summary>
public class ActivatableListBox : ListBox
{
    /// <summary>Raised when the selected item is activated (Enter / double-click).</summary>
    public event EventHandler? ItemActivated;

    /// <summary>Maximum time between two clicks on the same item to count as a double-click.</summary>
    public TimeSpan DoubleClickTime { get; set; } = TimeSpan.FromMilliseconds(500);

    private int _lastClickIndex = -1;
    private DateTime _lastClickTime;

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == ConsoleKey.Enter)
        {
            if (SelectedIndex >= 0)
            {
                ItemActivated?.Invoke(this, EventArgs.Empty);
            }
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        int index = SelectedIndex;
        var now = DateTime.UtcNow;
        if (index >= 0 && index == _lastClickIndex && now - _lastClickTime <= DoubleClickTime)
        {
            _lastClickIndex = -1;
            ItemActivated?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _lastClickIndex = index;
            _lastClickTime = now;
        }
    }
}

/// <summary>
/// Base class for <see cref="OpenFileDialog"/> and <see cref="SaveFileDialog"/>:
/// a modal dialog with an editable path box, a directory/file list (Enter or
/// double-click navigates into folders), a file name box, a filter selector and
/// accept/cancel buttons. On accept the chosen full path is in <see cref="FileName"/>
/// and <see cref="Dialog.DialogResult"/> is true.
/// </summary>
public abstract class FileDialog : Dialog
{
    /// <summary>Directory shown when the dialog opens. Empty = current directory.</summary>
    public string InitialDirectory { get; set; } = string.Empty;

    /// <summary>The directory currently being browsed.</summary>
    public string CurrentDirectory { get; private set; } = string.Empty;

    /// <summary>
    /// The chosen file. Before showing, its file-name part pre-fills the name box;
    /// after accepting it holds the full path.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File type filter in the classic form
    /// "Text files (*.txt)|*.txt|All files (*.*)|*.*" (patterns may be separated
    /// by ';'). Empty shows all files.
    /// </summary>
    public string Filter { get; set; } = string.Empty;

    /// <summary>Index of the active filter pair (0-based).</summary>
    public int FilterIndex { get; set; }

    /// <summary>When false (default) hidden files/directories are not listed.</summary>
    public bool ShowHiddenFiles { get; set; }

    protected TextBox PathBox { get; private set; } = null!;
    protected ActivatableListBox FileList { get; private set; } = null!;
    protected TextBox FileNameBox { get; private set; } = null!;
    protected ComboBox FilterCombo { get; private set; } = null!;
    protected Button OkButton { get; private set; } = null!;
    protected Button CancelButton { get; private set; } = null!;

    /// <summary>Text on the accept button ("Open" / "Save").</summary>
    protected abstract string AcceptButtonText { get; }

    private List<(string Description, string[] Patterns)> _filters = new();

    protected FileDialog()
    {
        Width = 62;
        Height = 18;
        MinWidth = 30;
        MinHeight = 10;
    }

    internal static List<(string Description, string[] Patterns)> ParseFilter(string? filter)
    {
        var result = new List<(string, string[])>();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var parts = filter.Split('|');
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                var patterns = parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (patterns.Length > 0)
                {
                    result.Add((parts[i].Trim(), patterns));
                }
            }
        }
        if (result.Count == 0)
        {
            result.Add(("All files (*.*)", new[] { "*" }));
        }
        return result;
    }

    /// <summary>Builds (or rebuilds) the dialog UI. Called automatically by <see cref="Show()"/>.</summary>
    protected virtual void BuildContent()
    {
        _filters = ParseFilter(Filter);
        if (FilterIndex < 0 || FilterIndex >= _filters.Count) FilterIndex = 0;

        PathBox = new TextBox { Name = "PathBox" };

        FileList = new ActivatableListBox { Name = "FileList", Margin = new Thickness(0, 1, 0, 1) };
        FileList.ItemActivated += (s, e) => ActivateEntry(FileList.SelectedItem as FileSystemEntry);
        FileList.SelectionChanged += (s, e) =>
        {
            if (FileList.SelectedItem is FileSystemEntry { IsDirectory: false, IsParent: false } file)
            {
                FileNameBox.Text = file.Name;
            }
        };

        FileNameBox = new TextBox { Name = "FileNameBox" };

        FilterCombo = new ComboBox { Name = "FilterCombo", Margin = new Thickness(0, 1, 1, 0) };
        foreach (var (description, _) in _filters)
        {
            FilterCombo.Items.Add(description);
        }
        FilterCombo.SelectedIndex = FilterIndex;
        FilterCombo.SelectionChanged += (s, e) =>
        {
            if (FilterCombo.SelectedIndex >= 0 && FilterCombo.SelectedIndex != FilterIndex)
            {
                FilterIndex = FilterCombo.SelectedIndex;
                RefreshEntries();
            }
        };

        OkButton = new Button { Name = "OkButton", Content = AcceptButtonText, Margin = new Thickness(1, 1, 0, 0) };
        OkButton.Click += (s, e) => Accept();
        CancelButton = new Button { Name = "CancelButton", Content = "Cancel", Margin = new Thickness(1, 1, 0, 0) };
        CancelButton.Click += (s, e) => Close(false);

        var nameRow = new Grid();
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        var nameLabel = new TextBlock { Text = "Name: " };
        Grid.SetColumn(nameLabel, 0);
        Grid.SetColumn(FileNameBox, 1);
        nameRow.Children.Add(nameLabel);
        nameRow.Children.Add(FileNameBox);

        var bottomRow = new Grid();
        bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(FilterCombo, 0);
        Grid.SetColumn(OkButton, 1);
        Grid.SetColumn(CancelButton, 2);
        bottomRow.Children.Add(FilterCombo);
        bottomRow.Children.Add(OkButton);
        bottomRow.Children.Add(CancelButton);

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(PathBox, 0);
        Grid.SetRow(FileList, 1);
        Grid.SetRow(nameRow, 2);
        Grid.SetRow(bottomRow, 3);
        grid.Children.Add(PathBox);
        grid.Children.Add(FileList);
        grid.Children.Add(nameRow);
        grid.Children.Add(bottomRow);

        Content = grid;
    }

    public override void Show()
    {
        BuildContent();

        string startDirectory = InitialDirectory;
        if (string.IsNullOrEmpty(startDirectory) || !Directory.Exists(startDirectory))
        {
            startDirectory = Directory.GetCurrentDirectory();
        }
        NavigateTo(startDirectory);

        if (!string.IsNullOrEmpty(FileName))
        {
            FileNameBox.Text = Path.GetFileName(FileName);
        }

        base.Show();

        // The list is the natural first target, not the path box.
        (GetRoot() as TuiWindow)?.SetFocus(FileList);
    }

    /// <summary>
    /// Navigates the dialog to the given directory and refreshes the list.
    /// Invalid or inaccessible paths are ignored.
    /// </summary>
    public void NavigateTo(string path)
    {
        string full;
        try
        {
            full = Path.GetFullPath(path);
            if (!Directory.Exists(full)) return;
        }
        catch
        {
            return;
        }

        CurrentDirectory = full;
        PathBox.Text = full;
        RefreshEntries();
    }

    /// <summary>Reloads the entries of <see cref="CurrentDirectory"/> using the active filter.</summary>
    protected void RefreshEntries()
    {
        var entries = new List<FileSystemEntry>();

        var parent = Directory.GetParent(CurrentDirectory);
        if (parent != null)
        {
            entries.Add(new FileSystemEntry { Name = "..", FullPath = parent.FullName, IsDirectory = true, IsParent = true });
        }

        try
        {
            var directories = new List<FileSystemEntry>();
            foreach (var dir in Directory.EnumerateDirectories(CurrentDirectory))
            {
                if (!ShowHiddenFiles && IsHidden(dir)) continue;
                directories.Add(new FileSystemEntry { Name = Path.GetFileName(dir), FullPath = dir, IsDirectory = true });
            }
            directories.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            entries.AddRange(directories);

            entries.AddRange(EnumerateMatchingFiles());
        }
        catch
        {
            // Inaccessible directory: show what we have (at least "..").
        }

        FileList.Items.Clear();
        foreach (var entry in entries)
        {
            FileList.Items.Add(entry);
        }
        FileList.SelectedIndex = entries.Count > 0 ? 0 : -1;
        Invalidate();
    }

    private List<FileSystemEntry> EnumerateMatchingFiles()
    {
        var files = new List<FileSystemEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] patterns = _filters[FilterIndex].Patterns;

        foreach (var pattern in patterns)
        {
            // "*.*" historically means "all files", but the .NET matcher would
            // require a dot in the name; normalize it.
            string effective = pattern is "*.*" or "" ? "*" : pattern;
            foreach (var file in Directory.EnumerateFiles(CurrentDirectory, effective))
            {
                if (!seen.Add(file)) continue;
                if (!ShowHiddenFiles && IsHidden(file)) continue;
                files.Add(new FileSystemEntry { Name = Path.GetFileName(file), FullPath = file, IsDirectory = false });
            }
        }

        files.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return files;
    }

    private static bool IsHidden(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.Hidden) != 0;
        }
        catch
        {
            return false;
        }
    }

    private void ActivateEntry(FileSystemEntry? entry)
    {
        if (entry == null) return;

        if (entry.IsDirectory)
        {
            NavigateTo(entry.FullPath);
        }
        else
        {
            FileNameBox.Text = entry.Name;
            OnFileActivated(entry);
        }
    }

    /// <summary>
    /// Called when a file entry is activated in the list. Open dialogs accept
    /// immediately; save dialogs only take over the name.
    /// </summary>
    protected virtual void OnFileActivated(FileSystemEntry entry) { }

    /// <summary>
    /// Attempts to accept the dialog with the current name-box content. A directory
    /// name navigates instead of accepting; subclass validation decides the rest.
    /// </summary>
    public void Accept()
    {
        string name = FileNameBox.Text?.Trim() ?? string.Empty;
        if (name.Length == 0) return;

        string fullPath;
        try
        {
            fullPath = Path.IsPathRooted(name) ? Path.GetFullPath(name) : Path.GetFullPath(Path.Combine(CurrentDirectory, name));
        }
        catch
        {
            return;
        }

        if (Directory.Exists(fullPath))
        {
            NavigateTo(fullPath);
            return;
        }

        CommitFile(fullPath);
    }

    /// <summary>
    /// Validates the chosen path and, when valid, sets <see cref="FileName"/> and
    /// closes the dialog with a true result.
    /// </summary>
    protected abstract void CommitFile(string fullPath);

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && e.Key == ConsoleKey.Enter)
        {
            if (ReferenceEquals(e.Source, PathBox))
            {
                e.Handled = true;
                NavigateTo(PathBox.Text ?? string.Empty);
                return;
            }
            if (ReferenceEquals(e.Source, FileNameBox))
            {
                e.Handled = true;
                Accept();
                return;
            }
        }
        base.OnKeyDown(e);
    }
}

/// <summary>
/// Modal dialog for picking an existing file to open.
/// </summary>
public class OpenFileDialog : FileDialog
{
    /// <summary>When true (default) the chosen file must exist for the dialog to accept.</summary>
    public bool CheckFileExists { get; set; } = true;

    protected override string AcceptButtonText => "Open";

    public OpenFileDialog()
    {
        Title = "Open";
    }

    protected override void OnFileActivated(FileSystemEntry entry)
    {
        Accept();
    }

    protected override void CommitFile(string fullPath)
    {
        if (CheckFileExists && !File.Exists(fullPath)) return;
        FileName = fullPath;
        Close(true);
    }
}

/// <summary>
/// Modal dialog for choosing a location and name to save a file. When the target
/// exists and <see cref="OverwritePrompt"/> is set, a Yes/No confirmation is shown
/// before accepting.
/// </summary>
public class SaveFileDialog : FileDialog
{
    /// <summary>Extension (without dot) appended when the entered name has none.</summary>
    public string DefaultExt { get; set; } = string.Empty;

    /// <summary>When true (default) saving over an existing file asks for confirmation.</summary>
    public bool OverwritePrompt { get; set; } = true;

    protected override string AcceptButtonText => "Save";

    public SaveFileDialog()
    {
        Title = "Save As";
    }

    protected override void CommitFile(string fullPath)
    {
        if (!string.IsNullOrEmpty(DefaultExt) && !Path.HasExtension(fullPath))
        {
            fullPath += "." + DefaultExt.TrimStart('.');
        }

        if (OverwritePrompt && File.Exists(fullPath) && GetRoot() is TuiWindow host)
        {
            string name = Path.GetFileName(fullPath);
            MessageDialog.Show(host, $"{name} already exists.\nDo you want to replace it?", "Confirm Save As",
                MessageDialogButtons.YesNo, result =>
                {
                    if (result == MessageDialogResult.Yes)
                    {
                        FileName = fullPath;
                        Close(true);
                    }
                });
            return;
        }

        FileName = fullPath;
        Close(true);
    }
}
