using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class VirtualBufferBlendTests
{
    [Fact]
    public void BlendPixel_OpaqueBackground_OverwritesCharacter()
    {
        // Arrange
        var buffer = new VirtualBuffer(1, 1);
        buffer.SetPixel(0, 0, 'A', TuiColor.White, TuiColor.Black);

        // Act - blend space with opaque background
        buffer.BlendPixel(0, 0, ' ', TuiColor.White, TuiColor.Gray); // Gray is opaque

        // Assert
        var pixel = buffer.GetPixel(0, 0);
        Assert.Equal(' ', pixel.Character); // Should be overwritten by space
        Assert.Equal(TuiColor.Gray, pixel.Background);
    }

    [Fact]
    public void BlendPixel_SemiTransparentBackground_PreservesCharacter()
    {
        // Arrange
        var buffer = new VirtualBuffer(1, 1);
        buffer.SetPixel(0, 0, 'A', TuiColor.White, TuiColor.Black);

        // Act - blend space with semi-transparent background (alpha = 128)
        var semiTrans = TuiColor.FromRgb(100, 100, 100, 128);
        buffer.BlendPixel(0, 0, ' ', TuiColor.White, semiTrans);

        // Assert
        var pixel = buffer.GetPixel(0, 0);
        Assert.Equal('A', pixel.Character); // Should preserve 'A'
        Assert.NotEqual(TuiColor.Black, pixel.Background); // Background is blended
    }

    [Fact]
    public void Clear_TransparentBackground_UsesTransparentForeground()
    {
        // Arrange
        var buffer = new VirtualBuffer(1, 1);

        // Act
        buffer.Clear(TuiColor.Transparent);

        // Assert
        var pixel = buffer.GetPixel(0, 0);
        Assert.True(pixel.Foreground.IsTransparent);
        Assert.True(pixel.Background.IsTransparent);
    }
}
