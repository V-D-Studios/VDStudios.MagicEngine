using System.Collections;
using System.Runtime.InteropServices;
using Veldrid;
using static VDStudios.MagicEngine.Graphics.Veldrid.DrawLibrary.ResourceLayoutBuilder;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawLibrary;

/// <summary>
/// Represents a buffer that maintains a set of resources to be organized in a final layout. 
/// </summary>
/// <remarks>
/// An object of this class is always thread-safe by the use of locking: If a thread accesses an object while another one is using it, that thread will be blocked until the object is available again
/// </remarks>
public sealed class ResourceLayoutBuilder : IEnumerable<ResourceLayoutEntry>
{
    private readonly object sync = new();
    /// <summary>
    /// Represents an entry describing a resource in this <see cref="ResourceLayoutBuilder"/>
    /// </summary>
    public struct ResourceLayoutEntry
    {
        /// <summary>
        /// The description of the resource
        /// </summary>
        public ResourceLayoutElementDescription Description;

        /// <summary>
        /// The actual resource that will be bound
        /// </summary>
        public BindableResource Resource;

        /// <summary>
        /// The relative position the resource will have in this layout when built
        /// </summary>
        /// <remarks>
        /// The actual position of the resource will be defined by the other elements. For example, even if <see cref="Position"/> is set to <see cref="int.MaxValue"/>, the resource will be positioned after every other resource with a smaller position parameter. If there are 6 resources in the layout, all of which have a position smaller than <see cref="int.MaxValue"/>, this resource will be last, and set at index 5
        /// </remarks>
        public int Position;
    }

    private readonly List<ResourceLayoutEntry> resources = new(10);
    private int lastPos = 0;
    private int firstPos = 0;

    /// <summary>
    /// Gets the number of resource descriptions contained in this <see cref="ResourceLayoutBuilder"/>
    /// </summary>
    public int Count => resources.Count;

    /// <summary>
    /// Builds the Layouts described in this builder, along with the resource array
    /// </summary>
    /// <param name="bindings"></param>
    /// <param name="factory"></param>
    public ResourceLayout Build(out BindableResource[] bindings, ResourceFactory factory)
    {
        lock (sync)
        {
            bindings = new BindableResource[Count];
            var elements = new ResourceLayoutElementDescription[Count];
            int i = 0;
            foreach (var resc in this.OrderBy(x => x.Position))
            {
                bindings[i] = resc.Resource;
                elements[i++] = resc.Description;
            }

            return factory.CreateResourceLayout(new ResourceLayoutDescription(elements));
        }
    }

    /// <summary>
    /// Clears this <see cref="ResourceLayoutBuilder"/>
    /// </summary>
    public void Clear()
    {
        resources.Clear();
    }

    /// <summary>
    /// Adds the element description as the first element of the layout
    /// </summary>
    /// <param name="element">The element to add</param>
    /// <param name="resource">The resource to be bound</param>
    /// <returns>The relative position the element was added at</returns>
    public int InsertFirst(ResourceLayoutElementDescription element, BindableResource resource)
    {
        int ret;
        lock (sync)
            resources.Add(new ResourceLayoutEntry() { Description = element, Position = ret = --firstPos, Resource = resource });
        return ret;
    }

    /// <summary>
    /// Adds the element description under the given relative position, but performs no sorting. Further manipulation of the builder may result in the element being otherwised sorted
    /// </summary>
    /// <remarks>
    /// If any other elements are in the builder that share the same relative position, the order in which they'll appear is non-deterministic
    /// </remarks>
    /// <param name="element">The element to add</param>
    /// <param name="position">The position to add the element at</param>
    /// <param name="resource">The resource to be bound</param>
    public void Add(ResourceLayoutElementDescription element, int position, BindableResource resource)
    {
        lock (sync)
            resources.Add(new ResourceLayoutEntry() { Description = element, Position = position, Resource = resource });
    }

    /// <summary>
    /// Adds the element description under the given relative position, but performs no sorting. Further manipulation of the builder may result in the element being otherwised sorted
    /// </summary>
    /// <remarks>
    /// If any other elements are in the builder that share the same relative position, the order in which they'll appear is non-deterministic
    /// </remarks>
    /// <param name="element">The element to add</param>
    public void Add(ResourceLayoutEntry element)
    {
        lock (sync)
            resources.Add(element);
    }

    /// <summary>
    /// Adds the element description in the layout, and moves elements that come after
    /// </summary>
    /// <param name="element">The element to add</param>
    /// <param name="position">The position to add the element at</param>
    /// <param name="resource">The resource to be bound</param>
    /// <param name="moved">The amount of elements that were moved in the layout after this operation</param>
    /// <returns>The relative position the element was added at</returns>
    public int Insert(ResourceLayoutElementDescription element, int position, BindableResource resource, out int moved)
    {
        moved = 0;
        lock (sync)
        {
            if (position >= lastPos)
                return InsertLast(element, resource);
            if (position < firstPos)
                return InsertFirst(element, resource);

            var span = CollectionsMarshal.AsSpan(resources);

            var abpos = Math.Abs(position);
            if (abpos + firstPos > abpos - lastPos) // firstPos should always be 0 or negative, while lastPos is always 0 or positive
            {
                // in this case, there are more elements before than there are elements after, so we'll move the elements that are after
                for (int i = 0; i < span.Length; i++)
                    if (span[i].Position >= position)
                    {
                        moved++;
                        span[i].Position++;
                    }
            }
            else
            {
                // in this case, there are more elements after than there are elements before, so we'll move the elements that are before
                for (int i = 0; i < span.Length; i++)
                    if (span[i].Position <= position)
                    {
                        moved++;
                        span[i].Position--;
                    }
            }

            resources.Add(new ResourceLayoutEntry() { Description = element, Position = position, Resource = resource });

            return position;
        }
    }

    /// <summary>
    /// Adds the element description as the last element of the layout
    /// </summary>
    /// <param name="element">The element to add</param>
    /// <param name="resource">The resource to be bound</param>
    /// <returns>The position the element was added at</returns>
    public int InsertLast(ResourceLayoutElementDescription element, BindableResource resource)
    {
        int ret;
        lock (sync)
            resources.Add(new ResourceLayoutEntry() { Description = element, Position = ret = lastPos++, Resource = resource });
        return ret;
    }

    /// <inheritdoc/>
    public IEnumerator<ResourceLayoutEntry> GetEnumerator()
    {
        return ((IEnumerable<ResourceLayoutEntry>)resources).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)resources).GetEnumerator();
    }
}
