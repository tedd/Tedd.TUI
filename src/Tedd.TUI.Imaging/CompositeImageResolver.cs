using System;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.Imaging;

/// <summary>
/// Tries a sequence of inner <see cref="IImageResolver"/> implementations in order and
/// returns the first successful result. Useful for composing
/// <see cref="FileImageResolver"/> + <see cref="HttpImageResolver"/> so a single
/// registration handles both local-disk and remote sources.
/// </summary>
public sealed class CompositeImageResolver : IImageResolver
{
    private readonly IImageResolver[] _resolvers;

    public CompositeImageResolver(params IImageResolver[] resolvers)
    {
        _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
    }

    public bool TryResolve(string source, string? baseDirectory, out byte[] data, out string? mediaType)
    {
        for (int i = 0; i < _resolvers.Length; i++)
        {
            if (_resolvers[i].TryResolve(source, baseDirectory, out data, out mediaType))
            {
                return true;
            }
        }
        data = Array.Empty<byte>();
        mediaType = null;
        return false;
    }
}
