using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Tedd.TUI.Controls.Primitives;

public abstract class Selector : ItemsControl
{
    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(Selector), -1, bindsTwoWayByDefault: true);

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(Selector), null, bindsTwoWayByDefault: true);

    public static readonly DependencyProperty SelectionModeProperty =
        DependencyProperty.Register(nameof(SelectionMode), typeof(SelectionMode), typeof(Selector), SelectionMode.Single);

    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(Selector), null, bindsTwoWayByDefault: true);

    // Guards the SelectedIndex <-> SelectedItem cross-sync in OnPropertyChanged so a
    // change to one side updates the other exactly once without recursing.
    private bool _syncingSelection;

    // Set while ApplySelection drives SelectedIndex/SelectedItem, so OnPropertyChanged
    // does not mistake its own writes for an external "replace the selection" write.
    private bool _updatingSelectionSet;

    // Set while the SelectedItems collection is being rewritten from _selectedIndices,
    // so the CollectionChanged handler does not feed the change straight back.
    private bool _syncingSelectedItems;

    // The authoritative multi-selection state. Indices, not items, because a list may
    // legitimately hold duplicate or equal items and each row selects independently.
    private readonly SortedSet<int> _selectedIndices = [];

    // Where a Shift-extended range starts: the item of the last plain click or toggle.
    private int _anchorIndex = -1;

    // The keyboard cursor: the row the user is standing on. A Shift-extended range moves
    // this end while _anchorIndex stays put, and it survives its row being deselected.
    private int _currentIndex = -1;

    private INotifyCollectionChanged? _attachedSelectedItems;

    protected Selector()
    {
        // Per-instance collection: a DP default value is shared by every instance, so a
        // mutable default would leak one control's selection into all the others.
        SetValue(SelectedItemsProperty, new ObservableCollection<object?>());
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty)!;
        set
        {
            // Out-of-range writes are rejected, preserving the current selection.
            if (value < -1 || value >= Items.Count) return;
            SetValue(SelectedIndexProperty, value);
        }
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Whether the user can select one item or many, and which gestures do it.
    /// Defaults to <see cref="Controls.SelectionMode.Single"/>.
    /// </summary>
    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty)!;
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>
    /// Every selected item, in item order. The control keeps this collection in sync with
    /// the selection, so an <c>ObservableCollection</c> bound here (or the default one it
    /// starts with) can be observed to track selection changes. Mutating it directly also
    /// works: the control adopts whatever items it then contains.
    /// </summary>
    public IList SelectedItems
    {
        get => (IList)GetValue(SelectedItemsProperty)!;
        set => SetValue(SelectedItemsProperty, value);
    }

    /// <summary>The indices of every selected item, ascending.</summary>
    public IReadOnlyList<int> SelectedIndices => _selectedIndices.ToArray();

    /// <summary>
    /// The item the user last acted on: the keyboard cursor and the anchor a Shift-extended
    /// range grows from. It outlives the selection — deselecting the current item with
    /// Control+click or Space leaves the cursor on that row, as list boxes elsewhere do.
    /// </summary>
    public int CurrentIndex =>
        _currentIndex >= 0 && _currentIndex < Items.Count ? _currentIndex : SelectedIndex;

    public event EventHandler? SelectionChanged;

    /// <summary>Whether the item at <paramref name="index"/> is part of the selection.</summary>
    public bool IsIndexSelected(int index) => _selectedIndices.Contains(index);

    /// <summary>Selects every item. Not valid while <see cref="SelectionMode"/> is Single.</summary>
    public void SelectAll()
    {
        if (SelectionMode == SelectionMode.Single)
            throw new NotSupportedException("SelectAll requires SelectionMode.Multiple or SelectionMode.Extended.");

        var all = new SortedSet<int>(Enumerable.Range(0, Items.Count));
        ApplySelection(all, SelectedIndex);
    }

    /// <summary>Clears the selection.</summary>
    public void UnselectAll()
    {
        _anchorIndex = -1;
        _currentIndex = -1;
        ApplySelection([], -1);
    }

    /// <summary>
    /// Applies the platform-standard click gesture for <paramref name="index"/>: plain
    /// selects, Shift extends the range from the anchor, Control (or Alt) toggles.
    /// Controls call this from their mouse and keyboard handling.
    /// </summary>
    protected internal void ApplySelectionGesture(int index, ConsoleModifiers modifiers)
    {
        if (index < 0 || index >= Items.Count) return;

        bool shift = (modifiers & ConsoleModifiers.Shift) != 0;
        // Control is the standard toggle modifier. Alt is accepted as well because many
        // terminals intercept Control-click before the application ever sees it.
        bool toggle = (modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) != 0;

        switch (SelectionMode)
        {
            case SelectionMode.Multiple:
                // No modifier needed in this mode: plain clicks toggle.
                if (shift) ExtendSelectionTo(index, union: true);
                else ToggleSelection(index);
                break;

            case SelectionMode.Extended:
                if (shift) ExtendSelectionTo(index, union: toggle);
                else if (toggle) ToggleSelection(index);
                else SelectSingle(index);
                break;

            default:
                SelectSingle(index);
                break;
        }
    }

    /// <summary>Replaces the selection with the single item at <paramref name="index"/>.</summary>
    public void SelectSingle(int index)
    {
        if (index < 0 || index >= Items.Count) return;
        _anchorIndex = index;
        _currentIndex = index;
        ApplySelection([index], index);
    }

    /// <summary>Adds the item at <paramref name="index"/> to the selection, or removes it if already selected.</summary>
    public void ToggleSelection(int index)
    {
        if (index < 0 || index >= Items.Count) return;
        if (SelectionMode == SelectionMode.Single)
        {
            SelectSingle(index);
            return;
        }

        var next = new SortedSet<int>(_selectedIndices);
        if (!next.Remove(index)) next.Add(index);
        _anchorIndex = index;
        _currentIndex = index;
        ApplySelection(next, index);
    }

    /// <summary>
    /// Selects the range between the anchor (the last plain click) and <paramref name="index"/>.
    /// With <paramref name="union"/> the range is added to the existing selection; otherwise it
    /// replaces it. The anchor stays put so dragging the Shift end back and forth re-picks the
    /// range rather than walking it.
    /// </summary>
    public void ExtendSelectionTo(int index, bool union = false)
    {
        if (index < 0 || index >= Items.Count) return;
        if (SelectionMode == SelectionMode.Single)
        {
            SelectSingle(index);
            return;
        }

        int anchor = _anchorIndex;
        if (anchor < 0 || anchor >= Items.Count) anchor = _anchorIndex = index;

        var next = union ? new SortedSet<int>(_selectedIndices) : [];
        for (int i = Math.Min(anchor, index); i <= Math.Max(anchor, index); i++)
            next.Add(i);

        // Only the moving end of the range advances; the anchor stays where the range began.
        _currentIndex = index;
        ApplySelection(next, index);
    }

    /// <summary>
    /// Makes <paramref name="indices"/> the selection, syncing SelectedItems and the primary
    /// SelectedIndex/SelectedItem, and raising SelectionChanged with the delta if it changed.
    /// </summary>
    private void ApplySelection(SortedSet<int> indices, int primaryIndex)
    {
        var added = new List<object?>();
        var removed = new List<object?>();

        foreach (int i in indices)
        {
            if (!_selectedIndices.Contains(i) && i >= 0 && i < Items.Count)
                added.Add(Items[i]);
        }
        foreach (int i in _selectedIndices)
        {
            if (!indices.Contains(i) && i >= 0 && i < Items.Count)
                removed.Add(Items[i]);
        }

        _selectedIndices.Clear();
        foreach (int i in indices)
        {
            if (i >= 0 && i < Items.Count) _selectedIndices.Add(i);
        }

        SyncSelectedItemsCollection();

        // The primary item is the one the user just acted on when it survived the change,
        // otherwise the first still-selected item — matching what a single-select consumer
        // reading SelectedItem expects to see.
        int primary = primaryIndex >= 0 && _selectedIndices.Contains(primaryIndex)
            ? primaryIndex
            : (_selectedIndices.Count > 0 ? _selectedIndices.Min : -1);

        bool primaryChanged = primary != SelectedIndex;
        SetPrimarySelection(primary);

        if (added.Count > 0 || removed.Count > 0 || primaryChanged)
            OnSelectionChanged(new SelectionChangedEventArgs(added, removed));
    }

    /// <summary>Writes SelectedIndex/SelectedItem without re-entering the selection-set logic.</summary>
    private void SetPrimarySelection(int index)
    {
        _updatingSelectionSet = true;
        _syncingSelection = true;
        try
        {
            SetValue(SelectedIndexProperty, index);
            SetValue(SelectedItemProperty, index >= 0 && index < Items.Count ? Items[index] : null);
        }
        finally
        {
            _syncingSelection = false;
            _updatingSelectionSet = false;
        }
    }

    /// <summary>Rewrites the public SelectedItems collection to match <see cref="_selectedIndices"/>.</summary>
    private void SyncSelectedItemsCollection()
    {
        if (GetValue(SelectedItemsProperty) is not IList list) return;

        _syncingSelectedItems = true;
        try
        {
            list.Clear();
            foreach (int i in _selectedIndices)
            {
                if (i >= 0 && i < Items.Count) list.Add(Items[i]);
            }
        }
        finally
        {
            _syncingSelectedItems = false;
        }
    }

    /// <summary>Rebuilds the index set from whatever the SelectedItems collection now holds.</summary>
    private void AdoptSelectedItemsCollection()
    {
        if (GetValue(SelectedItemsProperty) is not IList list) return;

        var indices = new SortedSet<int>();
        foreach (var item in list)
        {
            int i = Items.IndexOf(item);
            if (i >= 0) indices.Add(i);
        }
        ApplySelection(indices, SelectedIndex);
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);

        if (dp == SelectedItemsProperty)
        {
            if (_attachedSelectedItems != null)
                _attachedSelectedItems.CollectionChanged -= OnSelectedItemsCollectionChanged;

            _attachedSelectedItems = GetValue(SelectedItemsProperty) as INotifyCollectionChanged;
            if (_attachedSelectedItems != null)
                _attachedSelectedItems.CollectionChanged += OnSelectedItemsCollectionChanged;

            // A freshly bound collection describes the selection the consumer wants; an
            // empty one (the constructor's default) simply leaves the selection empty.
            AdoptSelectedItemsCollection();
            return;
        }

        if (_syncingSelection) return;

        if (dp == SelectedIndexProperty)
        {
            _syncingSelection = true;
            try
            {
                int index = SelectedIndex;
                if (index < -1 || index >= Items.Count)
                {
                    // Values written directly through SetValue (e.g. by a binding)
                    // bypass the CLR wrapper's range check; normalize to "no selection".
                    index = -1;
                    SetValue(SelectedIndexProperty, -1);
                }
                SetValue(SelectedItemProperty, index >= 0 ? Items[index] : null);
            }
            finally
            {
                _syncingSelection = false;
            }
            ReplaceSelectionWithPrimary();
            OnSelectionChanged();
        }
        else if (dp == SelectedItemProperty)
        {
            _syncingSelection = true;
            try
            {
                int index = Items.IndexOf(SelectedItem);
                if (index >= 0)
                {
                    SetValue(SelectedIndexProperty, index);
                }
                else
                {
                    // Unknown item clears the selection entirely.
                    SetValue(SelectedIndexProperty, -1);
                    SetValue(SelectedItemProperty, null);
                }
            }
            finally
            {
                _syncingSelection = false;
            }
            ReplaceSelectionWithPrimary();
            OnSelectionChanged();
        }
    }

    /// <summary>
    /// Setting SelectedIndex/SelectedItem from outside means "this is the selection now",
    /// as in WPF: any additional multi-selection is dropped.
    /// </summary>
    private void ReplaceSelectionWithPrimary()
    {
        if (_updatingSelectionSet) return;

        int index = SelectedIndex;
        _anchorIndex = index;
        _currentIndex = index;

        if (_selectedIndices.Count == 1 && _selectedIndices.Contains(index)) return;
        if (_selectedIndices.Count == 0 && index < 0) return;

        _selectedIndices.Clear();
        if (index >= 0) _selectedIndices.Add(index);
        SyncSelectedItemsCollection();
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_syncingSelectedItems) return;
        AdoptSelectedItemsCollection();
    }

    protected void OnSelectionChanged() => OnSelectionChanged(SelectionChangedEventArgs.Empty);

    protected virtual void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        SelectionChanged?.Invoke(this, e);
        Invalidate();
    }

    protected override void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsCollectionChanged(sender, e);

        // Re-validate selection state
        object? selectedItem = SelectedItem;
        int selectedIndex = SelectedIndex;

        if (selectedItem != null)
        {
            int index = Items.IndexOf(selectedItem);
            if (index >= 0)
            {
                // The item is still present; silently re-sync the index (no
                // SelectionChanged, the logical selection did not change).
                _syncingSelection = true;
                try
                {
                    SetValue(SelectedIndexProperty, index);
                }
                finally
                {
                    _syncingSelection = false;
                }
                RemapSelectedIndices();
            }
            else
            {
                _syncingSelection = true;
                try
                {
                    SetValue(SelectedIndexProperty, -1);
                    SetValue(SelectedItemProperty, null);
                }
                finally
                {
                    _syncingSelection = false;
                }
                RemapSelectedIndices();
                // Notify that selection is lost
                SelectionChanged?.Invoke(this, SelectionChangedEventArgs.Empty);
                Invalidate();
            }
        }
        else if (selectedIndex >= 0)
        {
            // Re-sync if SelectedIndex points to valid item
            if (selectedIndex < Items.Count)
            {
                _syncingSelection = true;
                try
                {
                    SetValue(SelectedItemProperty, Items[selectedIndex]);
                }
                finally
                {
                    _syncingSelection = false;
                }
                RemapSelectedIndices();
                SelectionChanged?.Invoke(this, SelectionChangedEventArgs.Empty);
                Invalidate();
            }
            else
            {
                _syncingSelection = true;
                try
                {
                    SetValue(SelectedIndexProperty, -1);
                }
                finally
                {
                    _syncingSelection = false;
                }
                RemapSelectedIndices();
                // No change event needed if both were effectively null/invalid
            }
        }
        else
        {
            RemapSelectedIndices();
        }
    }

    /// <summary>
    /// Follows the selection across an Items edit. The selected items are looked up again
    /// by identity, so an insert or removal above them shifts their indices instead of
    /// silently reselecting whatever moved into their old slot.
    /// </summary>
    private void RemapSelectedIndices()
    {
        if (_selectedIndices.Count == 0)
        {
            SyncSelectedItemsCollection();
            return;
        }

        // Snapshot the items before touching the index set, then map them back.
        var previous = GetValue(SelectedItemsProperty) is IList list && list.Count > 0
            ? list.Cast<object?>().ToList()
            : _selectedIndices.Where(i => i >= 0 && i < Items.Count).Select(i => Items[i]).ToList();

        _selectedIndices.Clear();
        foreach (var item in previous)
        {
            int i = Items.IndexOf(item);
            if (i >= 0) _selectedIndices.Add(i);
        }

        int primary = SelectedIndex;
        if (primary >= 0 && !_selectedIndices.Contains(primary)) _selectedIndices.Add(primary);

        SyncSelectedItemsCollection();
    }
}
