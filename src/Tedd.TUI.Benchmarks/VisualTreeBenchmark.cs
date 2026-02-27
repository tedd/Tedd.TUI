using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class VisualTreeBenchmark
{
    private UIElement _root = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create a deep tree
        var rootStack = new StackPanel();
        _root = rootStack;

        for (int i = 0; i < 100; i++)
        {
            var border = new Border();
            var innerStack = new StackPanel();
            border.Child = innerStack;
            rootStack.AddChild(border);

            // Add some items
            for (int j = 0; j < 10; j++)
            {
                innerStack.AddChild(new Button());
            }

            // Nest deeper
            if (i % 10 == 0)
            {
                var nestedStack = new StackPanel();
                innerStack.AddChild(nestedStack);
                // Add a TabControl to test specific logic
                var tab = new TabControl();
                tab.Items.Add(new TabItem { Header = "Tab 1", Content = new Button() });
                tab.Items.Add(new TabItem { Header = "Tab 2", Content = new Button() });
                tab.SelectedIndex = 0;
                nestedStack.AddChild(tab);
            }
        }
    }

    [Benchmark(Baseline = true)]
    public void IterativeYield()
    {
        foreach (var item in GetVisualTree(_root))
        {
            // simulate work
            if (item == null) { }
        }
    }

    [Benchmark]
    public void RecursiveList()
    {
        var list = new List<UIElement>();
        FlattenTree(_root, list);
        foreach (var item in list)
        {
            if (item == null) { }
        }
    }

    [Benchmark]
    public void OptimizedEnumerator()
    {
        foreach (var item in GetVisualTreeOptimized(_root))
        {
            if (item == null) { }
        }
    }

    [Benchmark]
    public void CurrentImplementation()
    {
        // This invokes the actual code in TuiWindow we just exposed
        // Note: TuiWindow is an instance, but GetVisualTree is an instance method.
        // We need a TuiWindow instance or make it static if possible.
        // GetVisualTree in TuiWindow is an instance method but doesn't use `this`.
        // However, I can't call it directly from here because I don't have a TuiWindow instance in _root setup.
        // But wait, GetVisualTree is inside TuiWindow.
        // Let's instantiate a dummy window to access the method, or rely on the fact that
        // the method is internal and I can just create an instance of TuiWindow
        // or make the method static in TuiWindow since it only takes `root`.
        // Let's assume for now I'll create a dummy window.

        var window = new TuiWindow();
        foreach (var item in window.GetVisualTree(_root))
        {
             if (item == null) { }
        }
    }

    // Original Recursive Implementation (from user snippet)
    private void FlattenTree(UIElement parent, List<UIElement> list)
    {
        list.Add(parent);

        if (parent is StackPanel stack)
        {
            foreach(var child in stack.Children) FlattenTree(child, list);
        }
        else if (parent is Border border && border.Child != null)
        {
            FlattenTree(border.Child, list);
        }
        else if (parent is TabControl tab)
        {
            // Mimic GetVisualTree logic roughly for recursive flattening?
            // Original user snippet didn't have TabControl logic, but GetVisualTree does.
            // Let's stick to the snippet logic for baseline "bad" code,
            // but maybe add basic children traversal if snippet missed it?
            // The snippet only handled StackPanel and Border explicitly.
            // But GetVisualTree handles generic VisualChildrenCount.
            // The snippet is likely incomplete.
            // Let's implement generic recursive flattening using VisualChildrenCount
            // to make it a fair comparison for "recursive list allocation" vs "iterative yield".

            // Actually, the user snippet specifically mentioned StackPanel and Border.
            // If I use generic traversal, it's better.
            int count = parent.VisualChildrenCount;
            for (int i = 0; i < count; i++)
            {
                FlattenTree(parent.GetVisualChild(i), list);
            }
        }
        // Fallback for other types
        else
        {
            int count = parent.VisualChildrenCount;
            for (int i = 0; i < count; i++)
            {
                FlattenTree(parent.GetVisualChild(i), list);
            }
        }
    }

    // Original Iterative Yield Implementation (from TuiWindow.cs)
    private IEnumerable<UIElement> GetVisualTree(UIElement root)
    {
        var stack = new Stack<(UIElement element, bool secondPass)>();
        stack.Push((root, false));

        while (stack.Count > 0)
        {
            var (current, secondPass) = stack.Pop();
            yield return current;

            if (!secondPass)
            {
                if (current is TabControl tab)
                {
                    // Second yield for tab strip
                    stack.Push((current, true));
                    // Content
                    if (tab.SelectedIndex >= 0 && tab.SelectedIndex < tab.Items.Count)
                    {
                        var item = tab.Items[tab.SelectedIndex];
                        UIElement? content = null;
                        if (item is TabItem ti) content = ti.Content as UIElement;
                        else content = item as UIElement;

                        if (content != null) stack.Push((content, false));
                    }
                }
                else
                {
                    // Normal children in reverse order
                    for (int i = current.VisualChildrenCount - 1; i >= 0; i--)
                    {
                        stack.Push((current.GetVisualChild(i), false));
                    }
                }
            }
        }
    }

    // Optimized Implementation
    private VisualTreeEnumerable GetVisualTreeOptimized(UIElement root)
    {
        return new VisualTreeEnumerable(root);
    }

    public struct VisualTreeEnumerable
    {
        private readonly UIElement _root;
        public VisualTreeEnumerable(UIElement root) => _root = root;
        public VisualTreeEnumerator GetEnumerator() => new VisualTreeEnumerator(_root);
    }

    public struct VisualTreeEnumerator
    {
        private readonly Stack<(UIElement element, bool secondPass)> _stack;
        private UIElement _current;

        public VisualTreeEnumerator(UIElement root)
        {
            _stack = new Stack<(UIElement element, bool secondPass)>();
            _stack.Push((root, false));
            _current = default!;
        }

        public UIElement Current => _current;

        public bool MoveNext()
        {
            if (_stack.Count == 0) return false;

            var (current, secondPass) = _stack.Pop();
            _current = current;

            if (!secondPass)
            {
                if (current is TabControl tab)
                {
                    _stack.Push((current, true));
                    if (tab.SelectedIndex >= 0 && tab.SelectedIndex < tab.Items.Count)
                    {
                        var item = tab.Items[tab.SelectedIndex];
                        UIElement? content = null;
                        if (item is TabItem ti) content = ti.Content as UIElement;
                        else content = item as UIElement;

                        if (content != null) _stack.Push((content, false));
                    }
                }
                else
                {
                    for (int i = current.VisualChildrenCount - 1; i >= 0; i--)
                    {
                        _stack.Push((current.GetVisualChild(i), false));
                    }
                }
            }
            return true;
        }
    }
}
