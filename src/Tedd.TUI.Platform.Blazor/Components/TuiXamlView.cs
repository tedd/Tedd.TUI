using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

/// <summary>
/// Hosts a TUI defined in XAML markup inside a Blazor application.
/// </summary>
/// <remarks>
/// <para>Two usage modes:</para>
/// <list type="bullet">
///   <item><b>Standalone</b> — placed anywhere in a page, it renders its own
///   <see cref="TuiView"/> surface: <c>&lt;TuiXamlView Source="tui/app.xaml" /&gt;</c>.
///   <see cref="Width"/>, <see cref="Height"/> and <see cref="Mode"/> configure the surface.</item>
///   <item><b>Nested</b> — placed inside an existing <see cref="TuiView"/>, the loaded
///   element becomes the window content of the surrounding view.</item>
/// </list>
/// <para>Markup comes from <see cref="Xaml"/> (inline string, takes precedence) or
/// <see cref="Source"/> (file path, absolute URL, or path relative to the app base —
/// see <see cref="XamlSource.FetchAsync"/>). Event handler attributes in the markup are
/// wired against <see cref="Controller"/> exactly like <c>XamlLoader.Load</c>.</para>
/// <para>A <c>TuiWindow</c> root element is honored in standalone mode; in nested mode its
/// content is unwrapped because the surrounding <see cref="TuiView"/> already owns a window.</para>
/// </remarks>
public class TuiXamlView : ComponentBase
{
    [CascadingParameter] public ITuiContainer? ParentContainer { get; set; }
    [Inject] public IServiceProvider Services { get; set; } = default!;

    /// <summary>XAML source reference: file path, absolute URL, or app-base-relative path.</summary>
    [Parameter] public string? Source { get; set; }

    /// <summary>Inline XAML markup. Takes precedence over <see cref="Source"/>.</summary>
    [Parameter] public string? Xaml { get; set; }

    /// <summary>Object whose methods/fields are bound to event attributes and x:Name fields in the markup.</summary>
    [Parameter] public object? Controller { get; set; }

    /// <summary>Surface width in character cells (standalone mode only).</summary>
    [Parameter] public int Width { get; set; } = 80;

    /// <summary>Surface height in character cells (standalone mode only).</summary>
    [Parameter] public int Height { get; set; } = 25;

    /// <summary>Render mode of the internal surface (standalone mode only).</summary>
    [Parameter] public TuiRenderMode Mode { get; set; } = TuiRenderMode.Canvas;

    /// <summary>The loaded root element (window content).</summary>
    public UIElement? Element { get; private set; }

    /// <summary>The hosted window (standalone mode only; null while loading and in nested mode).</summary>
    public TuiWindow? Window { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        string markup;
        if (Xaml != null)
            markup = Xaml;
        else if (Source != null)
            markup = await XamlSource.FetchAsync(Source, Services);
        else
            throw new InvalidOperationException($"{nameof(TuiXamlView)} requires either the {nameof(Xaml)} or the {nameof(Source)} parameter.");

        var root = XamlLoader.Load(markup, Controller);

        if (ParentContainer != null)
        {
            // Nested inside a TuiView: the parent already owns a window, so a TuiWindow
            // root is unwrapped and only its content contributes to the parent.
            Element = root is TuiWindow windowRoot ? windowRoot.Content : root;
            if (Element != null)
                ParentContainer.AddChild(Element);
        }
        else
        {
            Window = root as TuiWindow ?? new TuiWindow { Content = root };
            Element = Window.Content;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Nested mode renders no DOM of its own — the parent TuiView owns the surface.
        // Standalone mode waits for the async load before creating the surface so the
        // TuiView starts with the loaded window.
        if (ParentContainer != null || Window == null)
            return;

        builder.OpenComponent<TuiView>(0);
        builder.AddComponentParameter(1, nameof(TuiView.Width), Width);
        builder.AddComponentParameter(2, nameof(TuiView.Height), Height);
        builder.AddComponentParameter(3, nameof(TuiView.Mode), Mode);
        builder.AddComponentParameter(4, nameof(TuiView.Window), Window);
        builder.CloseComponent();
    }
}
