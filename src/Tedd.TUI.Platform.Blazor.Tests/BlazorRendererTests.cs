using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Moq;
using Tedd.TUI;
using Tedd.TUI.Platform.Blazor;
using Xunit;

namespace Tedd.TUI.Platform.Blazor.Tests;

public class BlazorRendererTests
{
    [Fact]
    public async Task RenderAsync_SendsFullBuffer_Baseline()
    {
        // Arrange
        var jsMock = new Mock<IJSRuntime>();
        var renderer = new BlazorRenderer(jsMock.Object, "testCanvas");
        var width = 10;
        var height = 5;
        var buffer = new VirtualBuffer(width, height);

        // Act
        await renderer.RenderAsync(buffer);

        // Assert
        // We capture the invocation to inspect arguments
        jsMock.Verify(js => js.InvokeAsync<object>(
            "tuiInterop.render",
            It.Is<object[]>(args =>
                args.Length == 4 &&
                (string)args[0] == "testCanvas" &&
                (int)args[1] == width &&
                (int)args[2] == height &&
                ((int[])args[3]).Length == width * height * 3
            )),
            Times.Once);
    }

    [Fact]
    public async Task RenderAsync_SendsDiff_ForSmallChanges()
    {
        // Arrange
        var jsMock = new Mock<IJSRuntime>();
        var renderer = new BlazorRenderer(jsMock.Object, "testCanvas");
        var width = 10;
        var height = 5;
        var buffer = new VirtualBuffer(width, height);

        // Initial render (full)
        await renderer.RenderAsync(buffer);
        jsMock.Invocations.Clear(); // Clear baseline call

        // Act
        // Change one pixel
        buffer.SetPixel(0, 0, 'X', ConsoleColor.Red, ConsoleColor.Blue);
        await renderer.RenderAsync(buffer);

        // Assert
        // Should call renderDiff
        jsMock.Verify(js => js.InvokeAsync<object>(
            "tuiInterop.renderDiff",
            It.Is<object[]>(args =>
                args.Length == 2 &&
                (string)args[0] == "testCanvas" &&
                ((int[])args[1]).Length == 5 // 1 change * 5 ints
            )),
            Times.Once);

        // Should NOT call render
        jsMock.Verify(js => js.InvokeAsync<object>(
            "tuiInterop.render",
            It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public async Task RenderAsync_SendsFull_ForLargeChanges()
    {
        // Arrange
        var jsMock = new Mock<IJSRuntime>();
        var renderer = new BlazorRenderer(jsMock.Object, "testCanvas");
        var width = 10;
        var height = 5;
        var buffer = new VirtualBuffer(width, height);

        // Initial render (full)
        await renderer.RenderAsync(buffer);
        jsMock.Invocations.Clear();

        // Act
        // Change all pixels
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buffer.SetPixel(x, y, 'X', ConsoleColor.Red, ConsoleColor.Blue);
            }
        }
        await renderer.RenderAsync(buffer);

        // Assert
        // Should call render (full) because 100% changed > 60%
        jsMock.Verify(js => js.InvokeAsync<object>(
            "tuiInterop.render",
            It.Is<object[]>(args =>
                args.Length == 4 &&
                ((int[])args[3]).Length == width * height * 3
            )),
            Times.Once);

        // Should NOT call renderDiff
        jsMock.Verify(js => js.InvokeAsync<object>(
            "tuiInterop.renderDiff",
            It.IsAny<object[]>()),
            Times.Never);
    }
}
