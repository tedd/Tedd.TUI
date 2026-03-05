using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class DependencyObjectTests
{
    public class MyObject : DependencyObject
    {
        public static readonly DependencyProperty TestProperty =
            DependencyProperty.Register("Test", typeof(int), typeof(MyObject), 42);

        public int Test
        {
            get { return (int)GetValue(TestProperty); }
            set { SetValue(TestProperty, value); }
        }
    }

    [Fact]
    public void TestDefaultValue()
    {
        var obj = new MyObject();
        Assert.Equal(42, obj.Test);
    }

    [Fact]
    public void TestSetValue()
    {
        var obj = new MyObject();
        obj.Test = 100;
        Assert.Equal(100, obj.Test);
    }

    [Fact]
    public void TestClearValue()
    {
        var obj = new MyObject();
        obj.Test = 100;
        Assert.Equal(100, obj.Test);

        obj.ClearValue(MyObject.TestProperty);
        Assert.Equal(42, obj.Test); // Should fall back to default value
    }
}
