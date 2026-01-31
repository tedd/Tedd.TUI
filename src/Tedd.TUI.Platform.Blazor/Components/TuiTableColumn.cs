using Microsoft.AspNetCore.Components;
using System;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiTableColumn : ComponentBase, IDisposable
{
    [CascadingParameter]
    public ITuiContainer Parent { get; set; }

    [Parameter]
    public string Header { get; set; }
    
    [Parameter]
    public string Width { get; set; } // "Auto", "*", "10", "2*"

    private TableColumn _column;
    private TuiTable _parentTable;

    protected override void OnInitialized()
    {
        if (Parent is TuiTable table)
        {
            _parentTable = table;
            _column = new TableColumn();
            UpdateColumn();
            _parentTable.AddColumn(_column);
        }
        else
        {
            // Warn? Or ignore.
            // Must be inside TuiTable.
        }
    }

    protected override void OnParametersSet()
    {
        if (_column != null)
        {
            UpdateColumn();
        }
    }

    private void UpdateColumn()
    {
        _column.Header = Header;
        _column.Width = ParseGridLength(Width);
    }
    
    private GridLength ParseGridLength(string width)
    {
        if (string.IsNullOrEmpty(width)) return GridLength.Star;
        width = width.Trim().ToLowerInvariant();
        
        if (width == "auto") return GridLength.Auto;
        if (width == "*") return GridLength.Star;
        if (width.EndsWith("*"))
        {
            if (width.Length == 1) return GridLength.Star;
            if (double.TryParse(width.Substring(0, width.Length - 1), out double val))
                return new GridLength(val, GridUnitType.Star);
        }
        if (double.TryParse(width, out double pixels))
        {
            return new GridLength(pixels, GridUnitType.Pixel);
        }
        
        return GridLength.Star; // Default
    }

    public void Dispose()
    {
        if (_parentTable != null && _column != null)
        {
            _parentTable.RemoveColumn(_column);
        }
    }
}
