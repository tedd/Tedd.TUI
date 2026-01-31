using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;
using System.Collections.Generic;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiComboBox : TuiComponentBase
{
    private ComboBox _comboBox = new ComboBox();
    public override UIElement Element => _comboBox;

    [Parameter] public List<string> Items { get; set; } = new List<string>();
    
    [Parameter] public string? SelectedItem { get; set; }
    [Parameter] public EventCallback<string?> SelectedItemChanged { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _comboBox.SelectionChanged += (s, e) =>
        {
            var newVal = _comboBox.SelectedItem?.ToString();
            if (SelectedItem != newVal)
            {
                SelectedItem = newVal;
                InvokeAsync(async () => await SelectedItemChanged.InvokeAsync(newVal));
            }
        };
    }

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        
        // Sync Items
        // Note: Full sync might be expensive if done every render, but for TUI scale it is fine.
        // We should clear and re-add if count differs or content?
        // Let's assume Items list reference changes or content is static-ish.
        // Simple approach: Clear and AddRange
        _comboBox.Items.Clear();
        foreach (var item in Items)
        {
            _comboBox.Items.Add(item);
        }

        if (SelectedItem != null)
        {
            _comboBox.SelectedItem = SelectedItem;
        }
    }
}
