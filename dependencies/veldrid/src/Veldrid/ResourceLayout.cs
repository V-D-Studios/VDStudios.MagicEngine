using System;

namespace Veldrid
{
    /// <summary>
    /// A device resource which describes the layout and kind of <see cref="BindableResource"/> objects available
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
        /// Gets the <see cref="ResourceLayoutElementDescription"/> present at the given index in the <see cref="ResourceLayoutDescription.Elements"/> array of this <see cref="ResourceLayout"/>
        /// </summary>
        /// <param name="index">The index at which the element is</param>
        /// <returns>A copy of the <see cref="ResourceLayoutElementDescription"/></returns>
        public ResourceLayoutElementDescription this[int index] => Description.Elements[index];

        /// <summary>
        /// The amount of <see cref="ResourceLayoutElementDescription"/>s in the <see cref="ResourceLayoutDescription.Elements"/> array of this <see cref="ResourceLayout"/>
        /// </summary>
        public int ElementCount => Description.Elements.Length;

        internal ResourceLayout(ref ResourceLayoutDescription description)
        {
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
}
