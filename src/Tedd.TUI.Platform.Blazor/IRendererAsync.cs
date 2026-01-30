using System.Threading.Tasks;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor;

public interface IRendererAsync
{
    Task RenderAsync(VirtualBuffer buffer);
    Task<(int CharWidth, int CharHeight)> InitAsync(int width, int height);
}
