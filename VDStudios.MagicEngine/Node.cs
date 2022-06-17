﻿namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an active object in the <see cref="Game"/>
/// </summary>
/// <remarks>
/// A <see cref="Node"/> can be an entity, a bullet, code to update another <see cref="Node"/>'s values, or any kind of active game object that is not a <see cref="FunctionalComponent"/>. To make a <see cref="Node"/> updateable, have it implement one of <see cref="IUpdateable"/> or <see cref="IUpdateableAsync"/>. To make a <see cref="Node"/> drawable, have it implement one of <see cref="IDrawable"/> or <see cref="IDrawableAsync"/>
/// </remarks>
public class Node : GameObject
{
    #region private



    #endregion

    

    /// <summary>
    /// Represents this <see cref="Node"/>'s parent
    /// </summary>
    /// <remarks>
    /// Valid as long as this <see cref="Node"/> is attached. If this <see cref="Node"/> is attached directly to a <see cref="Scene"/>, this property will be null
    /// </remarks>
    public Node? Parent { get; private set; }

    /// <summary>
    /// Represents the Root <see cref="Scene"/> for this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Valid (not null) as long as this <see cref="Node"/> is attached
    /// </remarks>
    public Scene? Root { get; private set; }

    /// <summary>
    /// Represents the Children or Subnodes of this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Due to thread safety concerns, the list is locked during reads
    /// </remarks>
    public NodeList Children { get; } = new();

    /// <summary>
    /// This <see cref="Node"/>'s Index in its parents
    /// </summary>
    /// <remarks>
    /// Valid for as long as this <see cref="Node"/> is attached. -1 means a <see cref="Node"/> is not attached
    /// </remarks>
    public int Index { get; private set; }

    /// <summary>
    /// Runs when a <see cref="Node"/> is attached to a parent <see cref="Node"/>
    /// </summary>
    /// <param name="parent">The parent <see cref="Node"/></param>
    /// <param name="index">The <see cref="Index"/> of this <see cref="Node"/> in its parent</param>
    /// <param name="services">The services for this <see cref="Game"/> scoped for this node</param>
    /// <remarks>
    /// This method is called before <see cref="NodeAttached"/> is fired
    /// </remarks>
    protected virtual void Attached(Node parent, int index, IServiceProvider services) { }

    /// <summary>
    /// Runs when a <see cref="Node"/> is attached to a parent <see cref="Scene"/>
    /// </summary>
    /// <param name="parent">The parent <see cref="Scene"/></param>
    /// <param name="index">The <see cref="Index"/> of this <see cref="Node"/> in its parent</param>
    /// <param name="services">The services for this <see cref="Game"/> scoped for this node</param>
    /// <remarks>
    /// This method is called before <see cref="NodeAttached"/> is fired
    /// </remarks>
    protected virtual void Attached(Scene parent, int index, IServiceProvider services) { }

    /// <summary>
    /// Runs when a <see cref="Node"/> is detached from its parent
    /// </summary>
    /// <remarks>
    /// This method is called before <see cref="NodeDetached"/> is fired
    /// </remarks>
    protected virtual void Detached() { }

    #region Events

    /// <summary>
    /// Fired when this <see cref="Node"/> is attached to either a parent <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
    public event NodeEvent? NodeAttached;

    /// <summary>
    /// Fired when this <see cref="Node"/> is detached from its parent <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
    public event NodeEvent? NodeDetached;

    /// <summary>
    /// Fired when this <see cref="Node"/>'s <see cref="Index"/> changes
    /// </summary>
    public event NodeIndexChangedEvent? IndexChanged;

    /// <summary>
    /// Fired when a <see cref="Node"/> is installed with a <see cref="FunctionalComponent"/>
    /// </summary>
    public event NodeFunctionalComponentInstallEvent? FunctionalComponentInstalled;

    /// <summary>
    /// Fired when a <see cref="Node"/> has one of its <see cref="FunctionalComponent"/>s uninstalled
    /// </summary>
    public event NodeFunctionalComponentInstallEvent? FunctionalComponentUninstalled;

    #endregion
}
