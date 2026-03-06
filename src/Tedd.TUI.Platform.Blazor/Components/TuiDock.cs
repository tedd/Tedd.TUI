using Microsoft.AspNetCore.Components;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor.Components;

// Helper component to attach DockPanel.Dock to an element. In Blazor we might just do this via code, or a wrapper.
// Alternatively, since Blazor ComponentBase doesn't easily expose attached properties in this framework,
// we can wrap elements or just use programmatic for the demo if it's too complex.
// Actually, I'll modify TuiComponentBase.cs or add Dock support.
