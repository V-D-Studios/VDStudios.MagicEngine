namespace VDStudios.MagicEngine.Templates;

/// <summary>
/// Represents a <see cref="GUIElement"/> that has been templated with a given set of configurations and tree structure
/// </summary>
public sealed class TemplatedGUIElement
{
    #region Construction

    /// <summary>
    /// Creates a new <see cref="TemplatedGUIElement"/> with its target element set to <typeparamref name="TElement"/>
    /// </summary>
    /// <typeparam name="TElement">The type of the target <see cref="GUIElement"/></typeparam>
    /// <param name="configurator">The configurator method for this <see cref="TemplatedGUIElement"/></param>
    /// <returns>The new <see cref="TemplatedGUIElement"/></returns>
    public static TemplatedGUIElement New<TElement>(TemplatedGUIElementConfigurator? configurator = null)
        where TElement : GUIElement, new()
    {
        var n = new TemplatedGUIElement();
        n.SetTargetElement<TElement>();
        n.SetConfigurationMethod(configurator);
        return n;
    }

    #endregion

    #region Instantiation

    private readonly HashSet<Thread> Syncs = new();

    /// <summary>
    /// Instantiates and adds a new <see cref="GUIElement"/> tree on <paramref name="manager"/> following this <see cref="TemplatedGUIElement"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> to build the tree on</param>
    public void Instance(GraphicsManager manager)
    {
        var thread = Thread.CurrentThread;
        lock (Syncs)
        {
            if (Syncs.Contains(thread))
            {
                Syncs.Remove(thread);
                throw new InvalidOperationException($"Circular reference detected in TemplatedGUIElement");
            }
            Syncs.Add(thread);
        }

        GUIElement el = ActivateAndConfigure(out var dc);
        manager.AddElement(el, dc);

        foreach (var child in SubElements)
            child.SubElementInstance(thread, el);

        lock (Syncs)
            Syncs.Remove(thread);
    }

    private GUIElement SubElementInstance(Thread thread, GUIElement parent)
    {
        lock (Syncs)
        {
            if (Syncs.Contains(thread))
            {
                Syncs.Remove(thread);
                throw new InvalidOperationException($"Circular reference detected in TemplatedGUIElement");
            }
            Syncs.Add(thread);
        }

        GUIElement el = ActivateAndConfigure(out var dc);
        parent.AddElement(el, dc);

        foreach (var child in SubElements)
            child.SubElementInstance(thread, el);

        lock (Syncs)
            Syncs.Remove(thread);

        return el;
    }

    private GUIElement ActivateAndConfigure(out object? dc)
    {
        var el = (GUIElement)Activator.CreateInstance(typeCache ??= TargetGUIElement.FetchType())!;
        dc = ConfigurationMethod is SerializableMethodDescription mdesc
            ? (configuratorCache ??= mdesc.FetchMethod<TemplatedGUIElementConfigurator>(null)).Invoke(el)
            : null;
        return el;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// The description of the type of the target <see cref="GUIElement"/>
    /// </summary>
    public SerializableTypeDescription TargetGUIElement
    {
        get => tgui;
        init => tgui = value;
    }
    private SerializableTypeDescription tgui;
    private Type? typeCache;

    /// <summary>
    /// The description of a static method that configures the element created from this template
    /// </summary>
    public SerializableMethodDescription? ConfigurationMethod
    {
        get => _confg;
        init => _confg = value;
    }
    private SerializableMethodDescription? _confg;
    private TemplatedGUIElementConfigurator? configuratorCache;

    /// <summary>
    /// The Sub Elements of this Template
    /// </summary>
    public LinkedList<TemplatedGUIElement> SubElements { get; init; } = new();

    #endregion

    #region Methods

    /// <summary>
    /// Adds a new <see cref="TemplatedGUIElement"/> as a sub element of this one
    /// </summary>
    /// <typeparam name="TElement">The type of the target sub <see cref="GUIElement"/></typeparam>
    /// <param name="configurator">The configuration method for the sub <see cref="GUIElement"/></param>
    /// <returns>This very same <see cref="TemplatedGUIElement"/> for the purposes of method call chaining</returns>
    public TemplatedGUIElement AddSubElement<TElement>(TemplatedGUIElementConfigurator? configurator = null) where TElement : GUIElement, new()
    {
        SubElements.AddLast(New<TElement>(configurator));
        return this;
    }

    /// <summary>
    /// Adds a new <see cref="TemplatedGUIElement"/> as a sub element of this one
    /// </summary>
    /// <typeparam name="TElement">The type of the target sub <see cref="GUIElement"/></typeparam>
    /// <param name="configurator">The configuration method for the sub <see cref="GUIElement"/></param>
    /// <param name="newTemplated">The newly created and added <see cref="TemplatedGUIElement"/></param>
    /// <returns>This very same <see cref="TemplatedGUIElement"/> for the purposes of method call chaining</returns>
    public TemplatedGUIElement AddSubElement<TElement>(out TemplatedGUIElement newTemplated, TemplatedGUIElementConfigurator? configurator = null) where TElement : GUIElement, new()
    {
        SubElements.AddLast(newTemplated = New<TElement>(configurator));
        return this;
    }

    /// <summary>
    /// Sets the <see cref="ConfigurationMethod"/> property of this <see cref="TemplatedGUIElement"/>
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Make very sure that <paramref name="configurator"/> is a <c>static</c> method! Otherwise this will fail!
    /// </remarks>
    /// <param name="configurator">The method to call when configuring this <see cref="TemplatedGUIElement"/></param>
    public void SetConfigurationMethod(TemplatedGUIElementConfigurator? configurator)
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
    /// Sets the <see cref="TargetGUIElement"/> property of this <see cref="TemplatedGUIElement"/>
    /// </summary>
    /// <typeparam name="TElement">The type of the target <see cref="GUIElement"/></typeparam>
    public void SetTargetElement<TElement>() where TElement : GUIElement, new()
    {
        typeCache = null;
        var t = typeof(TElement);

        if (t.ContainsGenericParameters)
            throw new ArgumentException("TElement must be a closed generic type or a concrete type", nameof(TElement));

        if (t.IsAbstract)
            throw new ArgumentException("TElement must be an instanceable type", nameof(TElement));

        tgui = SerializableTypeDescription.Describe<TElement>();
    }

    #endregion
}
