using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

#region Game Delegates

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
public delegate void GameMainWindowCreatedEvent(Game game, TimeSpan timestamp, Window window, Renderer renderer);

/// <summary>
/// Represents an event in the game regarding the <see cref="Game"/>'s <see cref="IGameLifetime"/>
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="lifetime">The <see cref="IGameLifetime"/> that is the object of the event</param>
public delegate void GameLifetimeChangedEvent(Game game, TimeSpan timestamp, IGameLifetime lifetime);

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
/// Represents an event in the game regarding a <see cref="Node"/>'s index
/// </summary>
/// <param name="node">The node that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="oldIndex">The previous <see cref="Node.Index"/> of <paramref name="node"/></param>
/// <param name="newIndex">The new <see cref="Node.Index"/>of <paramref name="node"/></param>
public delegate void NodeIndexChangedEvent(Node node, TimeSpan timestamp, int oldIndex, int newIndex);

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
public delegate void NodeSceneEvent(Node node, TimeSpan timestamp, Scene scene);

/// <summary>
/// 
/// </summary>
/// <param name="node">The node that experienced the change</param>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void NodeFunctionalComponentInstallEvent(Node node, FunctionalComponent component, TimeSpan timestamp);

#endregion

#region FunctionalComponent Delegates

/// <summary>
/// Represents an event in the game regarding a <see cref="FunctionalComponent"/>
/// </summary>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void FunctionalComponentEvent(FunctionalComponent component, TimeSpan timestamp);

/// <summary>
/// Represents an event in the game regarding a <see cref="FunctionalComponent"/>'s index
/// </summary>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="oldIndex">The previous <see cref="FunctionalComponent.Index"/> of <paramref name="component"/></param>
/// <param name="newIndex">The new <see cref="FunctionalComponent.Index"/>of <paramref name="component"/></param>
public delegate void FunctionalComponentIndexChangedEvent(FunctionalComponent component, TimeSpan timestamp, int oldIndex, int newIndex);

/// <summary>
/// Represents an event in the game regarding a <see cref="FunctionalComponent"/>'s <see cref="FunctionalComponent.AttachedNode"/>
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