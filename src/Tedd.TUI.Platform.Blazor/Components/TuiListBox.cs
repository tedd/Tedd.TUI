using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiListBox : TuiComponentBase
{
    private ListBox _listBox = new ListBox();
    public override UIElement Element => _listBox;

    [Parameter] public IEnumerable<string>? Items { get; set; }
    [Parameter] public int SelectedIndex { get; set; } = -1;
    [Parameter] public EventCallback<int> SelectedIndexChanged { get; set; }
    [Parameter] public bool ShowSelection { get; set; } = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _listBox.SelectionChanged += (s, e) =>
        {
            if (SelectedIndex != _listBox.SelectedIndex)
            {
                SelectedIndex = _listBox.SelectedIndex;
                InvokeAsync(async () => await SelectedIndexChanged.InvokeAsync(SelectedIndex));
            }
        };
    }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();

        // Sync Items
        // This is a bit inefficient for large lists or frequent updates, but fine for demo.
        // We clear and re-add if the list object changes or count differs?
        // Or just always rebuild to be safe?
        _listBox.Items.Clear();
        if (Items != null)
        {
            foreach (var item in Items)
            {
                _listBox.Items.Add(item);
            }
        }

        if (_listBox.SelectedIndex != SelectedIndex)
        {
            _listBox.SelectedIndex = SelectedIndex;
        }

        _listBox.ShowSelection = ShowSelection;
    }
}
