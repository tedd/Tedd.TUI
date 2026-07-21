using System;
using System.IO;
using SkiaSharp;
using Tedd.TUI;
using Tedd.TUI.CodeColoring;
using Tedd.TUI.Imaging;
using Tedd.TUI.Markdown;
using Tedd.TUI.Platform.Skia;

namespace Tedd.TUI.DocsScreenshots;

/// <summary>
/// Renders the sample screens shown on the GitHub Pages site (docs/index.html) and in
/// docs/README.md / README.md through the real Skia headless host, so the documentation
/// shows genuine character-cell renderer output instead of hand-drawn SVG mockups.
/// </summary>
internal static class Program
{
    private static void Main(string[] args)
    {
        var outputDir = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "assets"));
        Directory.CreateDirectory(outputDir);

        var sampleImagePath = SampleImage.Create(Path.Combine(Path.GetTempPath(), "tedd-tui-docs-sunset.png"));
        TuiImaging.RegisterDefaults(Path.GetDirectoryName(sampleImagePath));

        Render(outputDir, "hero.png", Scenes.BuildHero());
        Render(outputDir, "markdown.png", Scenes.BuildMarkdown());
        Render(outputDir, "code.png", Scenes.BuildCode());
        Render(outputDir, "images.png", Scenes.BuildImages(sampleImagePath));
        Render(outputDir, "form.png", Scenes.BuildForm());
        Render(outputDir, "table.png", Scenes.BuildTable());

        Console.WriteLine($"Wrote 6 renderer-generated PNGs to {outputDir}");
    }

    private static void Render(string outputDir, string fileName, TuiWindow window)
    {
        // A fresh host per scene: TuiSkiaHost's controller carries per-window focus and
        // frame state, so reusing one host across differently-sized scenes is not a
        // supported pattern for this one-shot batch renderer.
        using var host = new TuiSkiaHost();

        // Measure against a generous bound first so the PNG is cropped tight to the
        // scene's natural size (plus the Margin(1) padding baked into each scene) rather
        // than an arbitrarily chosen canvas with leftover blank space.
        window.Measure(new Size(400, 150));
        var size = window.DesiredSize;
        int columns = Math.Max(size.Width, 10);
        int rows = Math.Max(size.Height, 5);

        host.SetContent(window);
        host.RenderToPng(Path.Combine(outputDir, fileName), columns, rows);
        Console.WriteLine($"  {fileName}: {columns}x{rows} cells");
    }
}

internal static class SampleImage
{
    /// <summary>
    /// Draws a small synthetic sunset gradient and saves it as a PNG, so the "Image
    /// rendering" screenshot has a real bitmap to decode without checking a binary photo
    /// into the repository.
    /// </summary>
    public static string Create(string path)
    {
        const int width = 64;
        const int height = 40;

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;

        using (var sky = new SKPaint())
        {
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(0, height),
                new[] { new SKColor(0x21, 0x13, 0x45), new SKColor(0x9d, 0x4d, 0x4a), new SKColor(0xff, 0xb0, 0x3c) },
                new[] { 0f, 0.6f, 1f },
                SKShaderTileMode.Clamp);
            sky.Shader = shader;
            canvas.DrawRect(0, 0, width, height, sky);
        }

        using (var sun = new SKPaint { Color = new SKColor(0xff, 0xd7, 0x5f), IsAntialias = true })
        {
            canvas.DrawCircle(width / 2f, height * 0.62f, height * 0.22f, sun);
        }

        using (var hills = new SKPaint { Color = new SKColor(0x12, 0x0a, 0x1e), IsAntialias = true })
        {
            using var hillPath = new SKPath();
            hillPath.MoveTo(0, height * 0.8f);
            hillPath.CubicTo(width * 0.25f, height * 0.68f, width * 0.4f, height * 0.85f, width * 0.6f, height * 0.74f);
            hillPath.CubicTo(width * 0.8f, height * 0.65f, width * 0.9f, height * 0.8f, width, height * 0.75f);
            hillPath.LineTo(width, height);
            hillPath.LineTo(0, height);
            hillPath.Close();
            canvas.DrawPath(hillPath, hills);
        }

        canvas.Flush();
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
        return path;
    }
}
