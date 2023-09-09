using System;
using System.Collections.Immutable;

namespace Veldrid;

/// <summary>
/// A device resource which describes the layout and kind of <see cref="IBindableResource"/> objects available
/// to a shader set.
/// See <see cref="ResourceLayoutDescription"/>.
/// </summary>
public abstract class ResourceLayout : DeviceResource, IDisposable
{
#if VALIDATE_USAGE
    internal readonly ResourceLayoutDescription Description;
    internal readonly uint DynamicBufferCount;
#endif

    /// <summary>
    /// Represents the elements in this layout
    /// </summary>
    public ImmutableArray<ResourceLayoutElementDescription> Elements { get; }

    internal ResourceLayout(ref ResourceLayoutDescription description)
    {
        Elements = ImmutableArray.Create(description.Elements);
#if VALIDATE_USAGE
        Description = description;
        foreach (ResourceLayoutElementDescription element in description.Elements)
        {
            if ((element.Options & ResourceLayoutElementOptions.DynamicBinding) != 0)
            {
                DynamicBufferCount += 1;
            }
        }
#endif
    }

    /// <summary>
    /// A string identifying this instance. Can be used to differentiate between objects in graphics debuggers and other
    /// tools.
    /// </summary>
    public abstract string Name { get; set; }

    /// <summary>
    /// A bool indicating whether this instance has been disposed.
    /// </summary>
    public abstract bool IsDisposed { get; }

    /// <summary>
    /// Frees unmanaged device resources controlled by this instance.
    /// </summary>
    public abstract void Dispose();
}
