using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiTable : TuiComponentBase
{
    private Table _table = new Table();
    public override UIElement Element => _table;

    // We need to support Columns and Rows.
    // Columns are added via TuiTableColumn component which calls AddColumn.
    // Rows are added via TuiTableRow component which calls AddChild (inherited/overridden).

    [Parameter] public bool ShowHeader { get; set; } = true;
    [Parameter] public int PageSize { get; set; } = 0;
    [Parameter] public int TotalRows { get; set; } = -1;
    [Parameter] public int CurrentPage { get; set; } = 0;
    [Parameter] public EventCallback<int> CurrentPageChanged { get; set; }
    [Parameter] public EventCallback PageChanged { get; set; } // Generic event if user just wants notification

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _table.PageChanged += (s, e) =>
        {
            if (CurrentPage != _table.CurrentPage)
            {
                CurrentPage = _table.CurrentPage;
                CurrentPageChanged.InvokeAsync(CurrentPage);
                PageChanged.InvokeAsync();
            }
        };
    }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _table.ShowHeader = ShowHeader;
        _table.PageSize = PageSize;
        _table.TotalRows = TotalRows;
        // Only push CurrentPage if it differs, to avoid loops?
        if (_table.CurrentPage != CurrentPage) _table.CurrentPage = CurrentPage;
    }

    public void AddColumn(TableColumn column)
    {
        _table.Columns.Add(column);
        _table.Invalidate();
    }

    public void RemoveColumn(TableColumn column)
    {
        _table.Columns.Remove(column);
        _table.Invalidate();
    }

    public override void AddChild(UIElement child)
    {
        if (child is TableRow row)
        {
            _table.AddRow(row);
        }
        else
        {
            // If user adds a non-row, we could wrap it?
            // Or throw?
            // "Table" usually expects Rows.
            // Let's wrap it in a row for convenience?
            // If so, a single cell row.
            var wrapperRow = new TableRow();
            wrapperRow.AddCell(child);
            _table.AddRow(wrapperRow);
        }
    }
}
