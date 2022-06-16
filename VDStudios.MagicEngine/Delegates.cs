using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

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
/// Represents an event in the game regarding its <see cref="IGameLifetime"/>
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="lifetime">The <see cref="IGameLifetime"/> that is the object of the event</param>
public delegate void GameLifetimeEvent(Game game, TimeSpan timestamp, IGameLifetime lifetime);

/// <summary>
/// Represents an event in the game that is fired when the main Window and Renderer are created
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
/// <param name="window">The newly created main <see cref="Window"/></param>
/// <param name="renderer">The newly created main <see cref="Renderer"/></param>
public delegate void GameMainWindowCreatedEvent(Game game, TimeSpan timestamp, Window window, Renderer renderer);

/// <summary>
/// Represents an event in the game regarding a <see cref="Scene"/>
/// </summary>
/// <param name="scene">The scene that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void SceneEvent(Scene scene, TimeSpan timestamp);

/// <summary>
/// Represents an event in the game regarding a <see cref="FunctionalComponent"/>
/// </summary>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void FunctionalComponentEvent(FunctionalComponent component, TimeSpan timestamp);

/// <summary>
/// 
/// </summary>
/// <param name="node">The node that experienced the change</param>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since SDL's initialization and this event firing</param>
public delegate void NodeFunctionalComponentAttachmentEvent(Node node, FunctionalComponent component, TimeSpan timestamp);