using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using static VDStudios.MagicEngine.DrawLibrary.ResourceLayoutBuilder;
using static VDStudios.MagicEngine.DrawLibrary.ResourceSetBuilder;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// Represents a buffer that maintains a set of <see cref="ResourceLayoutBuilder"/>s to be organized in a final set. 
/// </summary>
/// <remarks>
/// An object of this class is always thread-safe by the use of locking: If a thread accesses an object while another one is using it, that thread will be blocked until the object is available again
/// </remarks>
public sealed class ResourceSetBuilder : IPoolableObject, IEnumerable<ResourceSetEntry>
{
    private readonly object sync = new();
    /// <summary>
    /// Represents an entry describing a resource set in this <see cref="ResourceSetBuilder"/>
    /// </summary>
    public struct ResourceSetEntry
    {
        /// <summary>
        /// The builder of the layout
        /// </summary>
        public ResourceLayoutBuilder Resource { get; set; }

        /// <summary>
        /// The relative position the resource layout will have in this set when built
        /// </summary>
        /// <remarks>
        /// The actual position of the resource will be defined by the other layouts. For example, even if <see cref="Position"/> is set to <see cref="int.MaxValue"/>, the resource will be positioned after every other resource with a smaller position parameter. If there are 6 resources in the layout, all of which have a position smaller than <see cref="int.MaxValue"/>, this resource will be last, and set at index 5
        /// </remarks>
        public int Position { get; set; }
    }

    private readonly List<ResourceSetEntry> resources = new(10);
    private int lastPos = 0;
    private int firstPos = 0;

    /// <summary>
    /// Gets the number of <see cref="ResourceLayoutBuilder"/>s contained in this <see cref="ResourceSetBuilder"/>
    /// </summary>
    public int Count => resources.Count;

    /// <summary>
    /// Clears this <see cref="ResourceSetBuilder"/>
    /// </summary>
    public void Clear()
    {
        resources.Clear();
    }

    /// <summary>
    /// Adds the <see cref="ResourceLayoutBuilder"/> as the first layout of the set
    /// </summary>
    /// <param name="layout">The layout to add</param>
    /// <returns>The relative position the layout was added at</returns>
    public int InsertFirst(ResourceLayoutBuilder layout)
    {
        int ret;
        lock (sync)
            resources.Add(new ResourceSetEntry() { Resource = layout, Position = ret = --firstPos });
        return ret;
    }

    /// <summary>
    /// Adds the layout description under the given relative position, but performs no sorting. Further manipulation of the builder may result in the layout being otherwised sorted
    /// </summary>
    /// <remarks>
    /// If any other layouts are in the builder that share the same relative position, the order in which they'll appear is non-deterministic
    /// </remarks>
    /// <param name="layout">The layout to add</param>
    /// <param name="position">The position to add the layout at</param>
    public void Add(ResourceLayoutBuilder layout, int position)
    {
        lock (sync)
            resources.Add(new ResourceSetEntry() { Resource = layout, Position = position });
    }

    /// <summary>
    /// Adds the layout description under the given relative position, but performs no sorting. Further manipulation of the builder may result in the layout being otherwised sorted
    /// </summary>
    /// <remarks>
    /// If any other layouts are in the builder that share the same relative position, the order in which they'll appear is non-deterministic
    /// </remarks>
    /// <param name="layout">The layout to add</param>
    public void Add(ResourceSetEntry layout)
    {
        lock (sync)
            resources.Add(layout);
    }

    /// <summary>
    /// Adds the layout description in the layout, and moves layouts that come after
    /// </summary>
    /// <param name="layout">The layout to add</param>
    /// <param name="position">The position to add the layout at</param>
    /// <param name="moved">The amount of layouts that were moved in the layout after this operation</param>
    /// <returns>The relative position the layout was added at</returns>
    public int Insert(ResourceLayoutBuilder layout, int position, out int moved)
    {
        moved = 0;
        lock (sync)
        {
            if (position >= lastPos)
                return InsertLast(layout);
            if (position < firstPos)
                return InsertFirst(layout);

            var span = CollectionsMarshal.AsSpan(resources);

            var abpos = Math.Abs(position);
            if (abpos + firstPos > abpos - lastPos) // firstPos should always be 0 or negative, while lastPos is always 0 or positive
            {
                // in this case, there are more layouts before than there are layouts after, so we'll move the layouts that are after
                for (int i = 0; i < span.Length; i++)
                    if (span[i].Position >= position)
                    {
                        moved++;
                        span[i].Position++;
                    }
            }
            else
            {
                // in this case, there are more layouts after than there are layouts before, so we'll move the layouts that are before
                for (int i = 0; i < span.Length; i++)
                    if (span[i].Position <= position)
                    {
                        moved++;
                        span[i].Position--;
                    }
            }

            return position;
        }
    }

    /// <summary>
    /// Adds the layout description as the last layout of the layout
    /// </summary>
    /// <param name="layout">The layout to add</param>
    /// <returns>The position the layout was added at</returns>
    public int InsertLast(ResourceLayoutBuilder layout)
    {
        int ret;
        lock (sync)
            resources.Add(new ResourceSetEntry() { Resource = layout, Position = ret = lastPos++ });
        return ret;
    }

    /// <inheritdoc/>
    public IEnumerator<ResourceSetEntry> GetEnumerator()
    {
        return ((IEnumerable<ResourceSetEntry>)resources).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)resources).GetEnumerator();
    }
}
