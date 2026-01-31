using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tedd.TUI.Platform.Blazor;

public struct RenderLayer
{
    public VirtualBuffer Buffer;
    public int X;
    public int Y;
    public int ZIndex;
}

public interface ILayeredRenderer
{
    Task RenderLayersAsync(List<RenderLayer> layers);
}
