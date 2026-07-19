using System;
using System.Collections.Generic;
using System.IO;

namespace Tedd.TUI;

/// <summary>
/// Modal dialog for browsing and selecting a folder. Enter or double-click
/// navigates into a folder, the path box accepts a typed path, and the optional
/// New Folder button prompts (via <see cref="InputDialog"/>) and creates a
/// directory. On accept <see cref="SelectedPath"/> holds the highlighted folder,
/// or the current directory when none is highlighted.
/// </summary>
public class FolderBrowserDialog : Dialog
{
    /// <summary>Directory shown when the dialog opens. Empty = current directory.</summary>
    public string InitialDirectory { get; set; } = string.Empty;

    /// <summary>The chosen folder after the dialog is accepted.</summary>
    public string SelectedPath { get; set; } = string.Empty;

    /// <summary>When true (default) a "New Folder" button is shown.</summary>
    public bool ShowNewFolderButton { get; set; } = true;

    /// <summary>When false (default) hidden directories are not listed.</summary>
    public bool ShowHiddenFolders { get; set; }

    /// <summary>The directory currently being browsed.</summary>
    public string CurrentDirectory { get; private set; } = string.Empty;

    protected TextBox PathBox { get; private set; } = null!;
    protected ActivatableListBox FolderList { get; private set; } = null!;

    public FolderBrowserDialog()
    {
        Title = "Select Folder";
        Width = 52;
        Height = 16;
        MinWidth = 26;
        MinHeight = 8;
    }

    /// <summary>Builds (or rebuilds) the dialog UI. Called automatically by <see cref="Show()"/>.</summary>
    protected virtual void BuildContent()
    {
        PathBox = new TextBox { Name = "PathBox" };

        FolderList = new ActivatableListBox { Name = "FolderList", Margin = new Thickness(0, 1, 0, 1) };
        FolderList.ItemActivated += (s, e) =>
        {
            if (FolderList.SelectedItem is FileSystemEntry entry)
            {
                NavigateTo(entry.FullPath);
            }
        };

        var selectButton = new Button { Name = "SelectButton", Content = "Select", Margin = new Thickness(1, 0, 0, 0) };
        selectButton.Click += (s, e) => Accept();
        var cancelButton = new Button { Name = "CancelButton", Content = "Cancel", Margin = new Thickness(1, 0, 0, 0) };
        cancelButton.Click += (s, e) => Close(false);

        var buttonRow = new Grid();
        buttonRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        buttonRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        buttonRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        if (ShowNewFolderButton)
        {
            var newFolderButton = new Button { Name = "NewFolderButton", Content = "New Folder", HorizontalAlignment = HorizontalAlignment.Left };
            newFolderButton.Click += (s, e) => PromptNewFolder();
            Grid.SetColumn(newFolderButton, 0);
            buttonRow.Children.Add(newFolderButton);
        }

        Grid.SetColumn(selectButton, 1);
        Grid.SetColumn(cancelButton, 2);
        buttonRow.Children.Add(selectButton);
        buttonRow.Children.Add(cancelButton);

        var grid = new Grid { Margin = new Thickness(1, 0, 1, 0) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(PathBox, 0);
        Grid.SetRow(FolderList, 1);
        Grid.SetRow(buttonRow, 2);
        grid.Children.Add(PathBox);
        grid.Children.Add(FolderList);
        grid.Children.Add(buttonRow);

        Content = grid;
    }

    public override void Show()
    {
        BuildContent();

        string startDirectory = SelectedPath;
        if (string.IsNullOrEmpty(startDirectory) || !Directory.Exists(startDirectory))
        {
            startDirectory = InitialDirectory;
        }
        if (string.IsNullOrEmpty(startDirectory) || !Directory.Exists(startDirectory))
        {
            startDirectory = Directory.GetCurrentDirectory();
        }
        NavigateTo(startDirectory);

        base.Show();
        (GetRoot() as TuiWindow)?.SetFocus(FolderList);
    }

    /// <summary>
    /// Navigates the dialog to the given directory and refreshes the folder list.
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

    /// <summary>Reloads the subdirectories of <see cref="CurrentDirectory"/>.</summary>
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
                if (!ShowHiddenFolders && IsHidden(dir)) continue;
                directories.Add(new FileSystemEntry { Name = Path.GetFileName(dir), FullPath = dir, IsDirectory = true });
            }
            directories.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            entries.AddRange(directories);
        }
        catch
        {
            // Inaccessible directory: show what we have (at least "..").
        }

        FolderList.Items.Clear();
        foreach (var entry in entries)
        {
            FolderList.Items.Add(entry);
        }
        FolderList.SelectedIndex = entries.Count > 0 ? 0 : -1;
        Invalidate();
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

    /// <summary>
    /// Accepts the dialog: the highlighted folder (or the current directory when
    /// ".." or nothing is highlighted) becomes <see cref="SelectedPath"/>.
    /// </summary>
    public void Accept()
    {
        SelectedPath = FolderList.SelectedItem is FileSystemEntry { IsParent: false } entry
            ? entry.FullPath
            : CurrentDirectory;
        Close(true);
    }

    private void PromptNewFolder()
    {
        if (GetRoot() is not TuiWindow host) return;

        InputDialog.Show(host, "Folder name:", "New Folder", "New Folder", name =>
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            string newPath;
            try
            {
                newPath = Path.Combine(CurrentDirectory, name.Trim());
                Directory.CreateDirectory(newPath);
            }
            catch
            {
                return; // Invalid name or no permission: silently ignore.
            }

            RefreshEntries();
            for (int i = 0; i < FolderList.Items.Count; i++)
            {
                if (FolderList.Items[i] is FileSystemEntry e && string.Equals(e.FullPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    FolderList.SelectedIndex = i;
                    break;
                }
            }
        });
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && e.Key == ConsoleKey.Enter && ReferenceEquals(e.Source, PathBox))
        {
            e.Handled = true;
            NavigateTo(PathBox.Text ?? string.Empty);
            return;
        }
        base.OnKeyDown(e);
    }
}
