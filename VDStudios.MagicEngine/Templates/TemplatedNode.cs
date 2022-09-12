using System.Buffers;

namespace VDStudios.MagicEngine.Templates;

/// <summary>
/// Represents a <see cref="Node"/> that has been templated with a given set of configurations and tree structure
/// </summary>
public sealed class TemplatedNode
{
    #region Construction

    /// <summary>
    /// Creates a new <see cref="TemplatedNode"/> with its target set to <typeparamref name="TNode"/>
    /// </summary>
    /// <typeparam name="TNode">The type of the target <see cref="Node"/></typeparam>
    /// <param name="factory">The method to use to instantiate a new node of type <typeparamref name="TNode"/></param>
    /// <param name="configurator">The configurator method for this <see cref="TemplatedNode"/></param>
    /// <returns>The new <see cref="TemplatedNode"/></returns>
    public static TemplatedNode New<TNode>(NodeFactory<TNode> factory, TemplatedNodeConfigurator? configurator = null)
        where TNode : Node
    {
        ArgumentNullException.ThrowIfNull(factory);
        var n = new TemplatedNode();
        n.SetTargetElement(factory);
        n.SetConfigurationMethod(configurator);
        return n;
    }

    /// <summary>
    /// Creates a new <see cref="TemplatedNode"/> with its target set to <typeparamref name="TNode"/>
    /// </summary>
    /// <typeparam name="TNode">The type of the target <see cref="Node"/></typeparam>
    /// <param name="configurator">The configurator method for this <see cref="TemplatedNode"/></param>
    /// <returns>The new <see cref="TemplatedNode"/></returns>
    public static TemplatedNode New<TNode>(TemplatedNodeConfigurator? configurator = null)
        where TNode : Node, new()
    {
        var n = new TemplatedNode();
        n.SetTargetElement<TNode>();
        n.SetConfigurationMethod(configurator);
        return n;
    }

    private static T ConstructorProxy<T>() where T : new() => new();

    #endregion

    #region Instantiation

    private readonly HashSet<Thread> Syncs = new();

    /// <summary>
    /// Instantiates a new <see cref="Node"/> tree following this <see cref="TemplatedNode"/>
    /// </summary>
    public ValueTask<Node> Instance()
        => Instance(Thread.CurrentThread, null);

    private async ValueTask<Node> Instance(Thread thread, Node? parent)
    {
        lock (Syncs)
        {
            if (Syncs.Contains(thread))
            {
                Syncs.Remove(thread);
                throw new InvalidOperationException($"Circular reference detected in TemplatedNode");
            }
            Syncs.Add(thread);
        }

        Node node = ActivateAndConfigure();
        if (parent is not null)
            await parent.Attach(node);

        var buffer = ArrayPool<ValueTask<Node>>.Shared.Rent(Children.Count);
        try
        {
            int i = 0;
            foreach (var child in Children)
                buffer[i++] = child.Instance(thread, node).Preserve();
            while (i > 0) await buffer[--i];
        }
        finally
        {
            ArrayPool<ValueTask<Node>>.Shared.Return(buffer, true);
        }

        lock (Syncs)
            Syncs.Remove(thread);

        return node;
    }

    private Node ActivateAndConfigure()
    {
        var el = (Node)Activator.CreateInstance(typeCache ??= TargetNode.FetchType())!;
        if (ConfigurationMethod is SerializableMethodDescription mdesc) 
            (configuratorCache ??= mdesc.FetchMethod<TemplatedNodeConfigurator>(null)).Invoke(el);
        return el;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// The description of the type of the target <see cref="Node"/>
    /// </summary>
    public SerializableTypeDescription TargetNode
    {
        get => tnode;
        init => tnode = value;
    }
    private SerializableTypeDescription tnode;
    private Type? typeCache;

    /// <summary>
    /// The description of a static method that instantiates a new Node
    /// </summary>
    public SerializableMethodDescription? FactoryMethod
    {
        get => _confg;
        init => _confg = value;
    }
    private SerializableMethodDescription? _factory;
    private TemplatedNodeConfigurator? factoryCache;

    /// <summary>
    /// The description of a static method that configures the node created from this template
    /// </summary>
    public SerializableMethodDescription? ConfigurationMethod
    {
        get => _confg;
        init => _confg = value;
    }
    private SerializableMethodDescription? _confg;
    private TemplatedNodeConfigurator? configuratorCache;

    /// <summary>
    /// The Children of this Template
    /// </summary>
    public LinkedList<TemplatedNode> Children { get; init; } = new();

    #endregion

    #region Methods

    /// <summary>
    /// Adds a new <see cref="TemplatedNode"/> as a child of this one
    /// </summary>
    /// <typeparam name="TNode">The type of the target <see cref="Node"/></typeparam>
    /// <param name="configurator">The configuration method for the <see cref="Node"/></param>
    /// <returns>This very same <see cref="TemplatedNode"/> for the purposes of method call chaining</returns>
    public TemplatedNode AddChild<TNode>(TemplatedNodeConfigurator? configurator = null) where TNode : Node, new()
        => AddChild(ConstructorProxy<TNode>, configurator);

    /// <summary>
    /// Adds a new <see cref="TemplatedNode"/> as a child of this one
    /// </summary>
    /// <typeparam name="TNode">The type of the target <see cref="Node"/></typeparam>
    /// <param name="configurator">The configuration method for the <see cref="Node"/></param>
    /// <param name="newTemplated">The newly created and added <see cref="TemplatedNode"/></param>
    /// <returns>This very same <see cref="TemplatedNode"/> for the purposes of method call chaining</returns>
    public TemplatedNode AddChild<TNode>(out TemplatedNode newTemplated, TemplatedNodeConfigurator? configurator = null) where TNode : Node, new()
        => AddChild(ConstructorProxy<TNode>, out newTemplated, configurator);

    /// <summary>
    /// Adds a new <see cref="TemplatedNode"/> as a child of this one
    /// </summary>
    /// <typeparam name="TNode">The type of the target <see cref="Node"/></typeparam>
    /// <param name="factory">The method to use to instantiate a new node of type <typeparamref name="TNode"/></param>
    /// <param name="configurator">The configuration method for the <see cref="Node"/></param>
    /// <returns>This very same <see cref="TemplatedNode"/> for the purposes of method call chaining</returns>
    public TemplatedNode AddChild<TNode>(NodeFactory<TNode> factory, TemplatedNodeConfigurator? configurator = null) where TNode : Node
    {
        Children.AddLast(New(factory, configurator));
        return this;
    }

    /// <summary>
    /// Adds a new <see cref="TemplatedNode"/> as a child of this one
    /// </summary>
    /// <typeparam name="TNode">The type of the target <see cref="Node"/></typeparam>
    /// <param name="factory">The method to use to instantiate a new node of type <typeparamref name="TNode"/></param>
    /// <param name="configurator">The configuration method for the <see cref="Node"/></param>
    /// <param name="newTemplated">The newly created and added <see cref="TemplatedNode"/></param>
    /// <returns>This very same <see cref="TemplatedNode"/> for the purposes of method call chaining</returns>
    public TemplatedNode AddChild<TNode>(NodeFactory<TNode> factory, out TemplatedNode newTemplated, TemplatedNodeConfigurator? configurator = null) where TNode : Node
    {
        Children.AddLast(newTemplated = New(factory, configurator));
        return this;
    }

    /// <summary>
    /// Sets the <see cref="ConfigurationMethod"/> property of this <see cref="TemplatedNode"/>
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Make very sure that <paramref name="configurator"/> is a <c>static</c> method! Otherwise this will fail!
    /// </remarks>
    /// <param name="configurator">The method to call when configuring this <see cref="TemplatedNode"/></param>
    public void SetConfigurationMethod(TemplatedNodeConfigurator? configurator)
    {
        configuratorCache = null;
        if (configurator is null)
        {
            _confg = null;
            return;
        }

        if (configurator.Target is not null)
            throw new InvalidOperationException($"Cannot use an instance method as a configuration method. There will be no attempts at instancing or serializing/deserializing a target object for the configurator");
        _confg = SerializableMethodDescription.Describe(configurator);
    }

    /// <summary>
    /// Sets the <see cref="TargetNode"/> property of this <see cref="TemplatedNode"/>
    /// </summary>
    /// <typeparam name="TNode">The type of the target <see cref="Node"/></typeparam>
    public void SetTargetElement<TNode>() where TNode : Node, new()
        => SetTargetElement(ConstructorProxy<TNode>);

    /// <summary>
    /// Sets the <see cref="TargetNode"/> property of this <see cref="TemplatedNode"/>
    /// </summary>
    /// <typeparam name="TNode">The type of the target <see cref="Node"/></typeparam>
    public void SetTargetElement<TNode>(NodeFactory<TNode> factory) where TNode : Node
    {
        typeCache = null;
        factoryCache = null;
        var t = typeof(TNode);

        if (t.ContainsGenericParameters)
            throw new ArgumentException("TNode must be a closed generic type or a concrete type", nameof(TNode));

        if (t.IsAbstract)
            throw new ArgumentException("TNode must be an instanceable type", nameof(TNode));

        _factory = SerializableMethodDescription.Describe(factory);
        tnode = SerializableTypeDescription.Describe<TNode>();
    }

    #endregion
}
