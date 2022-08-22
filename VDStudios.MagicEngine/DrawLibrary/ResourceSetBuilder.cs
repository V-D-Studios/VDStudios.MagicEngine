using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using static VDStudios.MagicEngine.DrawLibrary.ResourceLayoutBuilder;
using static VDStudios.MagicEngine.DrawLibrary.ResourceSetBuilder;
using ResourceSet = Veldrid.ResourceSet;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// Represents a buffer that maintains a set of <see cref="ResourceLayoutBuilder"/>s to be organized in a final set. 
/// </summary>
/// <remarks>
/// An object of this class is always thread-safe by the use of locking: If a thread accesses an object while another one is using it, that thread will be blocked until the object is available again
/// </remarks>
public sealed class ResourceSetBuilder : IPoolableObject, IEnumerable<ResourceSetEntry>
{
    private static ResourceLayoutBuilder DefaultNewLayout() => new();
    private readonly Func<ResourceLayoutBuilder>? rlf;
    private readonly Action<ResourceLayoutBuilder>? rlc;
    private ResourceLayoutBuilder NewLayout() => rlf?.Invoke() ?? DefaultNewLayout();

    /// <summary>
    /// Instances a new object of type <see cref="ResourceSetBuilder"/>
    /// </summary>
    public ResourceSetBuilder() { }

    /// <summary>
    /// Instances a new object of type <see cref="ResourceSetBuilder"/>
    /// </summary>
    /// <param name="resourceLayoutFactory">The method to ues when adding or inserting new <see cref="ResourceLayoutBuilder"/>s</param>
    /// <param name="resourceLayoutCleaner">The method to use on each <see cref="ResourceLayoutBuilder"/> when clearing this object</param>
    public ResourceSetBuilder(Func<ResourceLayoutBuilder> resourceLayoutFactory, Action<ResourceLayoutBuilder> resourceLayoutCleaner)
    {
        ArgumentNullException.ThrowIfNull(resourceLayoutFactory);
        ArgumentNullException.ThrowIfNull(resourceLayoutCleaner);
        rlf = resourceLayoutFactory;
        rlc = resourceLayoutCleaner;
    }

    private readonly object sync = new();
    /// <summary>
    /// Represents an entry describing a resource set in this <see cref="ResourceSetBuilder"/>
    /// </summary>
    public struct ResourceSetEntry
    {
        /// <summary>
        /// The actual layout, if exists. Overrides <see cref="LayoutBuilder"/>
        /// </summary>
        public (ResourceLayout layout, BindableResource[] resources)? Layout;

        /// <summary>
        /// The actual set, if exists. Overrides <see cref="Layout"/> and <see cref="LayoutBuilder"/>
        /// </summary>
        public global::Veldrid.ResourceSet? BuiltSet;

        /// <summary>
        /// The builder of the layout
        /// </summary>
        public ResourceLayoutBuilder LayoutBuilder { get; set; }

        /// <summary>
        /// The relative position the resource layout will have in this set when built
        /// </summary>
        /// <remarks>
        /// The actual position of the resource will be defined by the other layouts. For example, even if <see cref="Position"/> is set to <see cref="int.MaxValue"/>, the resource will be positioned after every other resource with a smaller position parameter. If there are 6 resources in the layout, all of which have a position smaller than <see cref="int.MaxValue"/>, this resource will be last, and set at index 5
        /// </remarks>
        public int Position { get; set; }
    }

    private readonly List<ResourceSetEntry> ResourceList = new(10);
    private int lastPos = 0;
    private int firstPos = 0;

    /// <summary>
    /// Gets the number of <see cref="ResourceLayoutBuilder"/>s contained in this <see cref="ResourceSetBuilder"/>
    /// </summary>
    public int Count => ResourceList.Count;

    /// <summary>
    /// Builds the ResourceSets and Layouts described in this builder
    /// </summary>
    /// <param name="sets"></param>
    /// <param name="layouts"></param>
    /// <param name="factory"></param>
    public void Build(out global::Veldrid.ResourceSet[]? sets, out ResourceLayout[]? layouts, ResourceFactory factory)
    {
        if (Count is 0)
        {
            sets = null;
            layouts = null;
            return;
        }

        sets = new global::Veldrid.ResourceSet[Count];
        layouts = new ResourceLayout[Count];
        for (int i = 0; i < ResourceList.Count; i++)
        {
            var entry = ResourceList[i];
            sets[i] = entry.Layout is (ResourceLayout rl, BindableResource[] br)
                ? factory.CreateResourceSet(new(layouts[i] = rl, br))
                : factory.CreateResourceSet(new(layouts[i] = entry.LayoutBuilder.Build(out var bindings, factory), bindings));
        }
    }

    /// <summary>
    /// Clears this <see cref="ResourceSetBuilder"/>
    /// </summary>
    public void Clear()
    {
        if (rlc is not Action<ResourceLayoutBuilder> cleaner)
        {
            ResourceList.Clear();
            return;
        }

        ResourceSetEntry[] pool;
        lock (sync)
        {
            int count = ResourceList.Count;
            pool = ArrayPool<ResourceSetEntry>.Shared.Rent(count);
            ResourceList.CopyTo(pool);
            ResourceList.Clear();
        }
        try
        {
            foreach (var rlb in ResourceList)
                cleaner(rlb.LayoutBuilder);
        }
        finally
        {
            ArrayPool<ResourceSetEntry>.Shared.Return(pool, true);
        }
    }

    /// <summary>
    /// Adds a <see cref="ResourceLayoutBuilder"/> as the first layout of the set
    /// </summary>
    /// <param name="addedAt">The relative position the layout was added at</param>
    /// <returns>The newly added layout builder</returns>
    public ResourceLayoutBuilder InsertFirst(out int addedAt)
    {
        var layout = NewLayout();
        lock (sync)
            ResourceList.Add(new ResourceSetEntry() { LayoutBuilder = layout, Position = addedAt = --firstPos });
        return layout;
    }

    /// <summary>
    /// Adds the passed resources as the first layout of the set
    /// </summary>
    /// <param name="layout">The already created <see cref="ResourceLayout"/> to add</param>
    /// <param name="resources">The resources associated with the layout</param>
    /// <param name="addedAt">The relative position the layout was added at</param>
    public void InsertFirst(ResourceLayout layout, BindableResource[] resources, out int addedAt)
    {
        lock (sync)
            ResourceList.Add(new ResourceSetEntry() { Layout = (layout, resources), Position = addedAt = --firstPos });
    }

    /// <summary>
    /// Adds the passed resources as the first set
    /// </summary>
    /// <param name="set">The already created <see cref="ResourceSet"/> to add</param>=
    /// <param name="addedAt">The relative position the layout was added at</param>
    public void InsertFirst(ResourceSet set, out int addedAt)
    {
        lock (sync)
            ResourceList.Add(new ResourceSetEntry() { BuiltSet = set, Position = addedAt = --firstPos });
    }

    /// <summary>
    /// Adds the layout description under the given relative position, but performs no sorting. Further manipulation of the builder may result in the layout being otherwised sorted
    /// </summary>
    /// <remarks>
    /// If any other layouts are in the builder that share the same relative position, the order in which they'll appear is non-deterministic
    /// </remarks>
    /// <param name="position">The position to add the layout at</param>
    /// <returns>The newly added layout builder</returns>
    public ResourceLayoutBuilder Add(int position)
    {
        var layout = NewLayout();
        lock (sync)
            ResourceList.Add(new ResourceSetEntry() { LayoutBuilder = layout, Position = position });
        return layout;
    }

    /// <summary>
    /// Adds the layout description under the given relative position, but performs no sorting. Further manipulation of the builder may result in the layout being otherwised sorted
    /// </summary>
    /// <remarks>
    /// If any other layouts are in the builder that share the same relative position, the order in which they'll appear is non-deterministic
    /// </remarks>
    /// <param name="set">The already created <see cref="ResourceSet"/> to add</param>
    /// <param name="position">The position to add the set at</param>
    public void Add(ResourceSet set, int position)
    {
        lock (sync)
            ResourceList.Add(new ResourceSetEntry() { BuiltSet = set, Position = position });
    }

    /// <summary>
    /// Adds the passed resources under the given relative position, but performs no sorting. Further manipulation of the builder may result in the layout being otherwised sorted
    /// </summary>
    /// <remarks>
    /// If any other layouts are in the builder that share the same relative position, the order in which they'll appear is non-deterministic
    /// </remarks>
    /// <param name="layout">The already created <see cref="ResourceLayout"/> to add</param>
    /// <param name="resources">The resources associated with the layout</param>
    /// <param name="position">The position to add the layout at</param>
    public void Add(ResourceLayout layout, BindableResource[] resources, int position)
    {
        lock (sync)
            ResourceList.Add(new ResourceSetEntry() { Layout = (layout, resources), Position = position });
    }

    private void OpenUpSpace(int position, ref int moved)
    {
        var span = CollectionsMarshal.AsSpan(ResourceList);

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
    }

    /// <summary>
    /// Adds the layout description in the layout, and moves layouts that come after
    /// </summary>
    /// <param name="position">The position to add the layout at</param>
    /// <param name="moved">The amount of layouts that were moved in the layout after this operation</param>
    /// <param name="addedAt">The relative position the layout was added at</param>
    /// <returns>The newly added layout builder</returns>
    public ResourceLayoutBuilder Insert(int position, out int addedAt, out int moved)
    {
        moved = 0;
        lock (sync)
        {
            if (position >= lastPos)
                return InsertLast(out addedAt);
            if (position < firstPos)
                return InsertFirst(out addedAt);

            OpenUpSpace(position, ref moved);
            addedAt = position;
            var layout = NewLayout();
            ResourceList.Add(new ResourceSetEntry() { LayoutBuilder = layout, Position = position });
            return layout;
        }
    }

    /// <summary>
    /// Adds the passed resources in the set, and moves layouts that come after
    /// </summary>
    /// <param name="position">The position to add the layout at</param>
    /// <param name="moved">The amount of layouts that were moved in the layout after this operation</param>
    /// <param name="addedAt">The relative position the layout was added at</param>
    /// <param name="layout">The already created <see cref="ResourceLayout"/> to add</param>
    /// <param name="resources">The resources associated with the layout</param>
    public void Insert(ResourceLayout layout, BindableResource[] resources, int position, out int addedAt, out int moved)
    {
        moved = 0;
        lock (sync)
        {
            if (position >= lastPos)
                InsertLast(layout, resources, out addedAt);
            if (position < firstPos)
                InsertFirst(layout, resources, out addedAt);

            OpenUpSpace(position, ref moved);
            addedAt = position;

            ResourceList.Add(new ResourceSetEntry() { Layout = (layout, resources), Position = position });
        }
    }

    /// <summary>
    /// Adds the passed resources in the set, and moves layouts that come after
    /// </summary>
    /// <param name="set">The already created <see cref="ResourceSet"/> to add</param>
    /// <param name="position">The position to add the set at</param>
    /// <param name="moved">The amount of layouts that were moved in the layout after this operation</param>
    /// <param name="addedAt">The relative position the layout was added at</param>
    public void Insert(ResourceSet set, int position, out int addedAt, out int moved)
    {
        moved = 0;
        lock (sync)
        {
            if (position >= lastPos)
                InsertLast(set, out addedAt);
            if (position < firstPos)
                InsertFirst(set, out addedAt);

            OpenUpSpace(position, ref moved);
            addedAt = position;

            ResourceList.Add(new ResourceSetEntry() { BuiltSet = set, Position = position });
        }
    }

    /// <summary>
    /// Adds the layout description as the last layout of the set
    /// </summary>
    /// <param name="addedAt">The relative position the layout was added at</param>
    /// <returns>The newly added layout builder</returns>
    public ResourceLayoutBuilder InsertLast(out int addedAt)
    {
        var layout = NewLayout();
        lock (sync)
            ResourceList.Add(new ResourceSetEntry() { LayoutBuilder = layout, Position = addedAt = lastPos++ });
        return layout;
    }

    /// <summary>
    /// Adds the passed resources as the first layout of the set
    /// </summary>
    /// <param name="layout">The already created <see cref="ResourceLayout"/> to add</param>
    /// <param name="resources">The resources associated with the layout</param>
    /// <param name="addedAt">The relative position the layout was added at</param>
    public void InsertLast(ResourceLayout layout, BindableResource[] resources, out int addedAt)
    {
        lock (sync)
            ResourceList.Add(new ResourceSetEntry() { Layout = (layout, resources), Position = addedAt = lastPos++ });
    }

    /// <summary>
    /// Adds the passed resources as the first set
    /// </summary>
    /// <param name="set">The already created <see cref="ResourceSet"/> to add</param>
    /// <param name="addedAt">The relative position the layout was added at</param>
    public void InsertLast(ResourceSet set, out int addedAt)
    {
        lock (sync)
            ResourceList.Add(new ResourceSetEntry() { BuiltSet = set, Position = addedAt = lastPos++ });
    }

    /// <inheritdoc/>
    public IEnumerator<ResourceSetEntry> GetEnumerator()
    {
        return ((IEnumerable<ResourceSetEntry>)ResourceList).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)ResourceList).GetEnumerator();
    }
}
