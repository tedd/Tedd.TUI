using System;
using System.Collections.Generic;
using Tedd.TUI;

namespace Repro
{
    class Program
    {
        static int _failures = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Running DataContext Propagation Tests...");

            RunTest("PropagationTest", PropagationTest);
            RunTest("LocalOverrideTest", LocalOverrideTest);
            RunTest("NotificationTest", NotificationTest);
            // RunTest("InheritanceChainTest", InheritanceChainTest); // Covered by PropagationTest

            if (_failures == 0)
            {
                Console.WriteLine("\nALL TESTS PASSED");
            }
            else
            {
                Console.WriteLine($"\n{_failures} TESTS FAILED");
                Environment.Exit(1);
            }
        }

        static void RunTest(string name, Action test)
        {
            Console.WriteLine($"\n[TEST] {name}");
            try
            {
                test();
                Console.WriteLine("PASS");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: {ex.Message}");
                _failures++;
            }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        static void PropagationTest()
        {
            var window = new TuiWindow();
            var stackPanel = new StackPanel();
            var textBlock = new TextBlock();

            window.Content = stackPanel;
            stackPanel.AddChild(textBlock);

            var data = "Test Data";
            window.DataContext = data;

            Assert((string)window.DataContext == data, "Window DataContext not set");
            Assert((string)stackPanel.DataContext == data, "StackPanel DataContext not propagated");
            Assert((string)textBlock.DataContext == data, "TextBlock DataContext not propagated");
        }

        static void LocalOverrideTest()
        {
            var window = new TuiWindow();
            var stackPanel = new StackPanel();
            var textBlock = new TextBlock();

            window.Content = stackPanel;
            stackPanel.AddChild(textBlock);

            var overrideData = "Override";
            var rootData = "Root";

            // Set local override
            textBlock.DataContext = overrideData;
            Assert((string)textBlock.DataContext == overrideData, "Local set failed");

            // Set root data
            window.DataContext = rootData;

            Assert((string)window.DataContext == rootData, "Window DataContext not set");
            Assert((string)stackPanel.DataContext == rootData, "StackPanel DataContext not propagated");

            // This checks if local value is respected.
            Assert((string)textBlock.DataContext == overrideData, $"TextBlock DataContext overwritten! Expected '{overrideData}', got '{textBlock.DataContext}'");
        }

        static void NotificationTest()
        {
            var window = new TuiWindow();
            var stackPanel = new StackPanel();
            var textBlock = new TestTextBlock();

            window.Content = stackPanel;
            stackPanel.AddChild(textBlock);

            var data1 = "Data1";
            var data2 = "Data2";

            window.DataContext = data1;
            Assert((string)textBlock.DataContext == data1, "Initial propagation failed");
            // Count can be 1 or 2 depending on whether AddChild triggers change. With Push model, AddChild sets DC (trigger 1), then window sets DC (trigger 2).
            // With Inheritance, AddChild shouldn't set DC.
            // We just verify it's > 0
            Assert(textBlock.ChangedCount > 0, $"OnDataContextChanged not called. Count={textBlock.ChangedCount}");
            int countAfterFirst = textBlock.ChangedCount;

            window.DataContext = data2;
            Assert((string)textBlock.DataContext == data2, "Update propagation failed");
            Assert(textBlock.ChangedCount > countAfterFirst, $"OnDataContextChanged not called on update. Count={textBlock.ChangedCount}");
        }

        class TestTextBlock : TextBlock
        {
            public int ChangedCount = 0;
            protected override void OnDataContextChanged(object newValue)
            {
                base.OnDataContextChanged(newValue);
                ChangedCount++;
            }
        }
    }
}
