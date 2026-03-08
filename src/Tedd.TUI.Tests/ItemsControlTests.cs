using System;
using System.Collections.ObjectModel;
using Xunit;

namespace Tedd.TUI.Tests;

public class ItemsControlTests
{
    private class TestItemsControl : ItemsControl
    {
    }

    [Fact]
    public void ItemsSource_Populates_Items()
    {
        var control = new TestItemsControl();
        var source = new[] { "A", "B", "C" };

        control.ItemsSource = source;

        Assert.Equal(3, control.Items.Count);
        Assert.Equal("A", control.Items[0]);
        Assert.Equal("B", control.Items[1]);
        Assert.Equal("C", control.Items[2]);
    }

    [Fact]
    public void ItemsSource_CollectionChanged_Updates_Items()
    {
        var control = new TestItemsControl();
        var source = new ObservableCollection<string>();

        control.ItemsSource = source;
        Assert.Empty(control.Items);

        source.Add("A");
        Assert.Single(control.Items);
        Assert.Equal("A", control.Items[0]);

        source.Add("B");
        Assert.Equal(2, control.Items.Count);

        source.Remove("A");
        Assert.Single(control.Items);
        Assert.Equal("B", control.Items[0]);

        source.Clear();
        Assert.Empty(control.Items);
    }

    [Fact]
    public void ItemsSource_Insert_Updates_Correct_Index()
    {
        var control = new TestItemsControl();
        var source = new ObservableCollection<string> { "A", "C" };
        control.ItemsSource = source;

        source.Insert(1, "B");

        Assert.Equal(3, control.Items.Count);
        Assert.Equal("A", control.Items[0]);
        Assert.Equal("B", control.Items[1]);
        Assert.Equal("C", control.Items[2]);
    }

    [Fact]
    public void ItemsSource_RemoveAt_Updates_Correct_Index()
    {
        var control = new TestItemsControl();
        var source = new ObservableCollection<string> { "A", "B", "C" };
        control.ItemsSource = source;

        source.RemoveAt(1);

        Assert.Equal(2, control.Items.Count);
        Assert.Equal("A", control.Items[0]);
        Assert.Equal("C", control.Items[1]);
    }

    [Fact]
    public void ItemsSource_Replace_Updates_Correct_Index()
    {
        var control = new TestItemsControl();
        var source = new ObservableCollection<string> { "A", "B", "C" };
        control.ItemsSource = source;

        source[1] = "D";

        Assert.Equal(3, control.Items.Count);
        Assert.Equal("A", control.Items[0]);
        Assert.Equal("D", control.Items[1]);
        Assert.Equal("C", control.Items[2]);
    }

    [Fact]
    public void ItemsSource_Move_Updates_Items()
    {
        var control = new TestItemsControl();
        var source = new ObservableCollection<string> { "A", "B", "C" };
        control.ItemsSource = source;

        source.Move(0, 2); // Move A to end

        Assert.Equal(3, control.Items.Count);
        Assert.Equal("B", control.Items[0]);
        Assert.Equal("C", control.Items[1]);
        Assert.Equal("A", control.Items[2]);
    }

    [Fact]
    public void Items_IsReadOnly_When_ItemsSource_Set()
    {
        var control = new TestItemsControl();
        control.ItemsSource = new[] { "A" };

        Assert.Throws<InvalidOperationException>(() => control.Items.Add("B"));
        Assert.Throws<InvalidOperationException>(() => control.Items.RemoveAt(0));
        Assert.Throws<InvalidOperationException>(() => control.Items.Clear());

        control.ItemsSource = null;
        control.Items.Add("B"); // Should work now
        Assert.Single(control.Items);
    }

    [Fact]
    public void GetItemText_Uses_DisplayMemberPath()
    {
        var control = new TestItemsControl();
        var item = new { Name = "TestItem", Value = 123 };
        control.DisplayMemberPath = "Name";

        var text = control.GetItemText(item);
        Assert.Equal("TestItem", text);

        control.DisplayMemberPath = "Value";
        text = control.GetItemText(item);
        Assert.Equal("123", text);

        control.DisplayMemberPath = "NonExistent";
        text = control.GetItemText(item);
        Assert.Equal(item.ToString(), text);
    }

    [Fact]
    public void ItemsControl_Uses_ItemTemplate_When_Provided()
    {
        var control = new TestItemsControl();

        var template = new DataTemplate(() =>
        {
            var textBlock = new TextBlock();
            textBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            return textBlock;
        });

        control.ItemTemplate = template;

        var items = new ObservableCollection<TestItem>
        {
            new TestItem { Name = "First" },
            new TestItem { Name = "Second" }
        };
        control.ItemsSource = items;

        // Force creation of visual tree
        var presenter = new ItemsPresenter();
        presenter.TemplatedParent = control;
        // ItemsControl creates template automatically when measured/arranged?
        // Wait, ItemsControl is a Control, so it has a Template.
        // Let's manually trigger the generation.
        control.Template = new ControlTemplate(parent =>
        {
            var ip = new ItemsPresenter();
            ip.TemplatedParent = parent;
            return ip;
        });
        control.ApplyTemplate();

        var root = control.GetVisualChild(0) as ItemsPresenter;
        Assert.NotNull(root);

        // Measure to trigger panel generation
        root.Measure(new Size(100, 100));

        var panel = root.GetVisualChild(0) as StackPanel;
        Assert.NotNull(panel);

        Assert.Equal(2, panel.VisualChildrenCount);

        var cp1 = panel.GetVisualChild(0) as ContentPresenter;
        var cp2 = panel.GetVisualChild(1) as ContentPresenter;

        Assert.NotNull(cp1);
        Assert.NotNull(cp2);

        // ContentPresenter should contain the raw item
        Assert.Equal(items[0], cp1.Content);
        Assert.Equal(items[1], cp2.Content);

        // ContentPresenter should have the ItemTemplate
        Assert.Equal(template, cp1.ContentTemplate);
        Assert.Equal(template, cp2.ContentTemplate);

        // Trigger ContentPresenter visual update
        cp1.Measure(new Size(100, 100));
        cp2.Measure(new Size(100, 100));

        var tb1 = cp1.GetVisualChild(0) as TextBlock;
        var tb2 = cp2.GetVisualChild(0) as TextBlock;

        Assert.NotNull(tb1);
        Assert.NotNull(tb2);

        Assert.Equal("First", tb1.Text);
        Assert.Equal("Second", tb2.Text);
    }

    [Fact]
    public void ItemTemplate_Change_Repopulates_ItemsPresenter()
    {
        var control = new TestItemsControl();

        var items = new[] { new TestItem { Name = "A" }, new TestItem { Name = "B" } };
        control.ItemsSource = items;

        control.Template = new ControlTemplate(parent =>
        {
            var ip = new ItemsPresenter();
            ip.TemplatedParent = parent;
            return ip;
        });
        control.ApplyTemplate();

        var root = control.GetVisualChild(0) as ItemsPresenter;
        Assert.NotNull(root);

        root.Measure(new Size(100, 100));

        var panel = root.GetVisualChild(0) as StackPanel;
        Assert.NotNull(panel);

        // Initially no template: fallback to GetItemText
        Assert.Equal(2, panel.VisualChildrenCount);
        var cp1Before = panel.GetVisualChild(0) as ContentPresenter;
        Assert.NotNull(cp1Before);
        Assert.Null(cp1Before.ContentTemplate);

        // Now set an ItemTemplate
        var template = new DataTemplate(() =>
        {
            var tb = new TextBlock();
            tb.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            return tb;
        });
        control.ItemTemplate = template;

        // After setting ItemTemplate, panel should be repopulated with containers using the template
        Assert.Equal(2, panel.VisualChildrenCount);
        var cp1After = panel.GetVisualChild(0) as ContentPresenter;
        Assert.NotNull(cp1After);
        Assert.Equal(template, cp1After.ContentTemplate);
    }

    private class TestItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
