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

    public class NullableObject : DependencyObject
    {
        public static readonly DependencyProperty NullableBoolProperty =
            DependencyProperty.Register("NullableBool", typeof(bool?), typeof(NullableObject), (bool?)null);

        public bool? NullableBool
        {
            get => (bool?)GetValue(NullableBoolProperty);
            set => SetValue(NullableBoolProperty, value);
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

    [Fact]
    public void SetValue_NullableDP_AcceptsBoxedBool()
    {
        // bool? DP must accept a boxed bool (true/false) without throwing ArgumentException
        var obj = new NullableObject();
        obj.NullableBool = true;
        Assert.Equal(true, obj.NullableBool);

        obj.NullableBool = false;
        Assert.Equal(false, obj.NullableBool);
    }

    [Fact]
    public void SetValue_NullableDP_AcceptsNull()
    {
        var obj = new NullableObject();
        obj.NullableBool = true;
        obj.NullableBool = null;
        Assert.Null(obj.NullableBool);
    }

    [Fact]
    public void SetValue_NullableDP_RejectsWrongType()
    {
        var obj = new NullableObject();
        Assert.Throws<ArgumentException>(() => obj.SetValue(NullableObject.NullableBoolProperty, 42));
    }
}
