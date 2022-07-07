using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Templates;
using Veldrid;

namespace VDStudios.MagicEngine;

#region Game Delegates

/// <summary>
/// Represents an operation to be run against a given <see cref="Window"/>
/// </summary>
/// <param name="window"></param>
public delegate void WindowAction(Window window);

/// <summary>
/// Represents an event in the <see cref="Game"/> regarding the changing of a scene
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="newScene">The scene that was just set</param>
/// <param name="oldScene">The scene that was previously set</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void GameSceneChangedEvent(Game game, TimeSpan timestamp, Scene newScene, Scene oldScene);

/// <summary>
/// Represents an event in the <see cref="Game"/> regarding a scene
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="scene">The scene that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void GameSceneEvent(Game game, TimeSpan timestamp, Scene scene);

/// <summary>
/// Represents an event in the game
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void GameEvent(Game game, TimeSpan timestamp);

/// <summary>
/// Represents an event in the game that is fired when the main Window and Renderer are created
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="window">The newly created main <see cref="Window"/></param>
/// <param name="renderer">The newly created main <see cref="Renderer"/></param>
public delegate void GameMainWindowCreatedEvent(Game game, TimeSpan timestamp, Window window, GraphicsDevice renderer);

/// <summary>
/// Represents an event in the game regarding the <see cref="Game"/>'s <see cref="IGameLifetime"/>
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="lifetime">The <see cref="IGameLifetime"/> that is the object of the event</param>
public delegate void GameLifetimeChangedEvent(Game game, TimeSpan timestamp, IGameLifetime lifetime);

/// <summary>
/// Represents an event in the game in which <see cref="Game.GameTitle"/> has changed
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="newTitle">The newly set title of the game</param>
/// <param name="oldTitle">The previously set title of the game</param>
public delegate void GameTitleChangedEvent(Game game, TimeSpan timestamp, string newTitle, string oldTitle);

internal delegate void GameSetupScenesEvent(Game game, IServiceProvider gamescope);

#endregion

#region IGameLifetime Delegates

/// <summary>
/// Represents an event in the game regarding a <see cref="IGameLifetime"/>
/// </summary>
/// <param name="lifetime">The <see cref="IGameLifetime"/> that experienced the change</param>
/// <param name="shouldRun">Whether the <see cref="IGameLifetime"/> still describes that it should run</param>
public delegate void GameLifetimeEvent(IGameLifetime lifetime, bool shouldRun);

#endregion

#region Node Delegates

/// <summary>
/// Represents an event in the game regarding a <see cref="Node"/>'s readiness, represented by <see cref="Node.IsReady"/>
/// </summary>
/// <param name="node">The node that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="isReady">Whether or not <paramref name="node"/> became ready at the time this event fired</param>
public delegate void NodeReadinessChangedEvent(Node node, TimeSpan timestamp, bool isReady);

/// <summary>
/// Represents an event in the game regading a <see cref="Node"/> and its parent
/// </summary>
/// <param name="node">The Node that that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="parent">The parent of <paramref name="node"/> if it has one. Can be either a <see cref="Node"/> or a <see cref="Scene"/></param>
public delegate void NodeParentEvent(Node node, TimeSpan timestamp, NodeBase? parent);

/// <summary>
/// Represents an event in the game regarding a <see cref="Node"/>
/// </summary>
/// <param name="node">The Node that that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void NodeEvent(Node node, TimeSpan timestamp);

/// <summary>
/// Represents an event in the game regarding a <see cref="Node"/> interacting with a <see cref="Scene"/>
/// </summary>
/// <param name="node">The Node that that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="scene">The Scene in question</param>
public delegate void NodeSceneEvent(Node node, TimeSpan timestamp, Scene? scene);

/// <summary>
/// 
/// </summary>
/// <param name="node">The node that experienced the change</param>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void NodeFunctionalComponentInstallEvent(Node node, FunctionalComponent component, TimeSpan timestamp);

/// <summary>
/// Represents a method that configures a <see cref="Node"/> that has been instanced from a <see cref="TemplatedNode"/>
/// </summary>
/// <param name="node">The newly instanced <see cref="Node"/> from the template</param>
public delegate void TemplatedNodeConfigurator(Node node);

/// <summary>
/// Represents a method that creates a new <see cref="Node"/>
/// </summary>
/// <returns>The newly created <see cref="Node"/></returns>
public delegate Node NodeFactory();

/// <summary>
/// Represents a method that creates a new <typeparamref name="TNode"/>
/// </summary>
/// <returns>The newly created <typeparamref name="TNode"/></returns>
public delegate TNode NodeFactory<TNode>();

#endregion

#region FunctionalComponent Delegates

/// <summary>
/// Represents an event in the game regarding a <see cref="FunctionalComponent"/>'s readiness, represented by <see cref="FunctionalComponent.IsReady"/>
/// </summary>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="isReady">Whether or not <paramref name="component"/> became ready at the time this event fired</param>
public delegate void FunctionalComponentReadinessChangedEvent(FunctionalComponent component, TimeSpan timestamp, bool isReady);

/// <summary>
/// Represents an event in the game regarding a <see cref="FunctionalComponent"/>
/// </summary>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void FunctionalComponentEvent(FunctionalComponent component, TimeSpan timestamp);

/// <summary>
/// Represents an event in the game regarding a <see cref="FunctionalComponent"/>'s <see cref="FunctionalComponent.Owner"/>
/// </summary>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="node">The <see cref="Node"/> that experienced the change</param>
public delegate void FunctionalComponentNodeEvent(FunctionalComponent component, TimeSpan timestamp, Node node);

#endregion

#region Scene Delegates

/// <summary>
/// Represents an event in the game regarding a <see cref="Scene"/>
/// </summary>
/// <param name="scene">The scene that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void SceneEvent(Scene scene, TimeSpan timestamp);

/// <summary>
/// Represents an event in the game regarding a <see cref="Scene"/> and a <see cref="Node"/>
/// </summary>
/// <param name="scene">The scene that experienced the change</param>
/// <param name="node">The node that experienced the change alongside <paramref name="scene"/></param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void SceneNodeEvent(Scene scene, TimeSpan timestamp, Node node);

#endregion

#region GraphicsManager Delegates

/// <summary>
/// Represents an event in the game in which a <see cref="GraphicsManager"/> stopped or started
/// </summary>
/// <remarks>
/// Specifically, when <see cref="GraphicsManager.IsRunning"/> changes
/// </remarks>
/// <param name="graphicsManager">The <see cref="GraphicsManager"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="isRunning">The new value of <see cref="GraphicsManager.IsRunning"/> at the time this event was fired</param>
public delegate void GraphicsManagerRunStateChanged(GraphicsManager graphicsManager, TimeSpan timestamp, bool isRunning);

#endregion

#region GUIElement Delegates

/// <summary>
/// Represents an event in the game regarding a <see cref="GUIElement"/>'s <see cref="GUIElement.DataContext"/>
/// </summary>
/// <param name="element">The <see cref="GUIElement"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="oldContext">The data context <paramref name="element"/> previously had, if any</param>
/// <param name="newContext">The data context <paramref name="element"/> now has, if any</param>
public delegate void GUIElementDataContextChangedEvent(GUIElement element, TimeSpan timestamp, object? oldContext, object? newContext);

/// <summary>
/// Represents an event in the game regarding a <see cref="GUIElement"/>'s <see cref="GUIElement.IsActive"/> property
/// </summary>
/// <param name="element">The <see cref="GUIElement"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="isActive">The value that <paramref name="element"/>'s <see cref="GUIElement.IsActive"/> changed into</param>
public delegate void GUIElementActiveChanged(GUIElement element, TimeSpan timestamp, bool isActive);

/// <summary>
/// Represents a method that configures a <see cref="GUIElement"/> that has been instanced from a <see cref="TemplatedGUIElement"/>
/// </summary>
/// <param name="element">The newly instanced <see cref="GUIElement"/> from the template</param>
/// <returns>An object representing the <see cref="GUIElement.DataContext"/> of the element, or <c>null</c> if it's not meant to have one</returns>
public delegate object? TemplatedGUIElementConfigurator(GUIElement element);

#endregion