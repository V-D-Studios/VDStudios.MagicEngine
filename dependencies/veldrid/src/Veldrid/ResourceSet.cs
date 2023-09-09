using System;

namespace Veldrid;

/// <summary>
/// A device resource used to bind a particular set of <see cref="IBindableResource"/> objects to a <see cref="CommandList"/>.
/// See <see cref="ResourceSetDescription"/>.
/// </summary>
public abstract class ResourceSet : DeviceResource, IDisposable
{
    internal ResourceSet(ref ResourceSetDescription description)
    {
#if VALIDATE_USAGE
        Layout = description.Layout;
        Resources = description.BoundResources;
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

#if VALIDATE_USAGE
    public ResourceLayout Layout { get; }
    internal IBindableResource[] Resources { get; }
#endif
}
