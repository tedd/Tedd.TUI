using System;
using System.Collections.Generic;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ItemCollectionTests
{
    public static IEnumerable<object[]> AddRangeData()
    {
        yield return new object[] { new List<object> { "A", "B", "C" }, 3, "A", "B", "C" };
        yield return new object[] { new List<object> { 1, 2 }, 2, 1, 2, null };
        yield return new object[] { new List<object>(), 0, null, null, null };
    }

    [Theory]
    [MemberData(nameof(AddRangeData))]
    public void AddRange_AddsItems(IEnumerable<object> itemsToAdd, int expectedCount, object expectedFirst, object expectedSecond, object expectedThird)
    {
        var coll = new ItemCollection();
        coll.AddRange(itemsToAdd);

        Assert.Equal(expectedCount, coll.Count);
        if (expectedCount > 0) Assert.Equal(expectedFirst, coll[0]);
        if (expectedCount > 1) Assert.Equal(expectedSecond, coll[1]);
        if (expectedCount > 2) Assert.Equal(expectedThird, coll[2]);
    }

    [Theory]
    [InlineData(null)]
    public void AddRange_Null_DoesNothing(IEnumerable<object>? nullCollection)
    {
        var coll = new ItemCollection();
        coll.AddRange(nullCollection!);
        Assert.Empty(coll);
    }

    [Theory]
    [InlineData("A")]
    [InlineData(1)]
    [InlineData(null)]
    public void SetReadOnly_True_ThrowsOnAdd(object? item)
    {
        var coll = new ItemCollection();
        coll.SetReadOnly(true);
        Assert.Throws<InvalidOperationException>(() => coll.Add(item!));
    }

    [Theory]
    [InlineData(0, "A")]
    [InlineData(0, 1)]
    [InlineData(0, null)]
    public void SetReadOnly_True_ThrowsOnInsert(int index, object? item)
    {
        var coll = new ItemCollection();
        coll.SetReadOnly(true);
        Assert.Throws<InvalidOperationException>(() => coll.Insert(index, item!));
    }

    [Theory]
    [InlineData("A")]
    [InlineData(1)]
    [InlineData(null)]
    public void SetReadOnly_True_ThrowsOnRemove(object? item)
    {
        var coll = new ItemCollection();
        // Since coll.Remove(item) only triggers SetItem/RemoveItem if the item is present in the list,
        // we must make sure the item we are attempting to remove is present so it actually throws on RemoveItem.
        // Or actually we could try removing at index to force the readonly exception directly.
        coll.InternalAdd(item!);
        coll.SetReadOnly(true);
        Assert.Throws<InvalidOperationException>(() => coll.Remove(item!));
    }

    [Theory]
    [InlineData(0, "B")]
    [InlineData(0, 2)]
    [InlineData(0, null)]
    public void SetReadOnly_True_ThrowsOnSet(int index, object? item)
    {
        var coll = new ItemCollection();
        coll.InternalAdd("A");
        coll.SetReadOnly(true);
        Assert.Throws<InvalidOperationException>(() => coll[index] = item!);
    }

    [Fact]
    public void SetReadOnly_True_ThrowsOnClear()
    {
        var coll = new ItemCollection();
        coll.InternalAdd("A");
        coll.SetReadOnly(true);
        Assert.Throws<InvalidOperationException>(() => coll.Clear());
    }

    public static IEnumerable<object[]> AddRangeThrowsData()
    {
        yield return new object[] { new List<object> { "A" } };
        yield return new object[] { new List<object> { 1, 2 } };
    }

    [Theory]
    [MemberData(nameof(AddRangeThrowsData))]
    public void SetReadOnly_True_ThrowsOnAddRange(IEnumerable<object> items)
    {
        var coll = new ItemCollection();
        coll.SetReadOnly(true);
        Assert.Throws<InvalidOperationException>(() => coll.AddRange(items));
    }

    [Theory]
    [InlineData("A", "B", "C")]
    [InlineData(1, 2, 3)]
    [InlineData(null, null, null)]
    public void InternalMethods_BypassReadOnly(object? item1, object? item2, object? item3)
    {
        var coll = new ItemCollection();
        coll.SetReadOnly(true);

        // Add
        coll.InternalAdd(item1!);
        Assert.Single(coll);

        // Insert
        coll.InternalInsert(0, item2!);
        Assert.Equal(2, coll.Count);
        Assert.Equal(item2!, coll[0]);

        // Set
        coll.InternalSet(0, item3!);
        Assert.Equal(item3!, coll[0]);

        // Remove
        coll.InternalRemoveAt(0);
        Assert.Single(coll);
        Assert.Equal(item1!, coll[0]);

        // Clear
        coll.InternalClear();
        Assert.Empty(coll);
    }
}
