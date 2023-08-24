using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Input;

namespace VDStudios.MagicEngine;

#region General Delegates

/// <summary>
/// Represents a generic event in the game that has a sender of type <typeparamref name="TSender"/> and some data of type <typeparamref name="TData"/>
/// </summary>
/// <param name="data">Some data that contains more information about the event</param>
/// <param name="sender">The object that experienced the event</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <typeparam name="TData">The type of the object that contains more information about the event</typeparam>
/// <typeparam name="TSender">The type of the object that experienced the event</typeparam>
public delegate void GeneralGameEvent<TData, in TSender>(TSender sender, TData data, TimeSpan timestamp)
    where TSender : GameObject;

/// <summary>
/// Represents a generic event in the game that has a sender of type <typeparamref name="TSender"/>
/// </summary>
/// <param name="sender">The object that experienced the event</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <typeparam name="TSender">The type of the object that experienced the event</typeparam>
public delegate void GeneralGameEvent<in TSender>(TSender sender, TimeSpan timestamp)
    where TSender : GameObject;

/// <summary>
/// Represents a generic event in the game that represents an update of some kind and has a sender of type <typeparamref name="TSender"/> and some data of type <typeparamref name="TData"/>
/// </summary>
/// <param name="data">Some data that contains more information about the event</param>
/// <param name="sender">The object that experienced the event</param>
/// <param name="delta">The amount of time that has passed since the last time this event was fired</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <typeparam name="TData">The type of the object that contains more information about the event</typeparam>
/// <typeparam name="TSender">The type of the object that experienced the event</typeparam>
public delegate void UpdateGameEvent<TData, in TSender>(TSender sender, TData data, TimeSpan delta, TimeSpan timestamp)
    where TSender : GameObject;

/// <summary>
/// Represents a generic event in the game that represents an update of some kind and has a sender of type <typeparamref name="TSender"/>
/// </summary>
/// <param name="sender">The object that experienced the event</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <param name="delta">The amount of time that has passed since the last time this event was fired</param>
/// <typeparam name="TSender">The type of the object that experienced the event</typeparam>
public delegate void UpdateGameEvent<in TSender>(TSender sender, TimeSpan delta, TimeSpan timestamp)
    where TSender : GameObject;

/// <summary>
/// Represents a generic event that represents an update of some kind and has a sender of type <typeparamref name="TSender"/> and some data of type <typeparamref name="TData"/>
/// </summary>
/// <param name="data">Some data that contains more information about the event</param>
/// <param name="sender">The object that experienced the event</param>
/// <param name="delta">The amount of time that has passed since the last time this event was fired</param>
/// <typeparam name="TData">The type of the object that contains more information about the event</typeparam>
/// <typeparam name="TSender">The type of the object that experienced the event</typeparam>
public delegate void UpdateEvent<TData, in TSender>(TSender sender, TData data, TimeSpan delta);

/// <summary>
/// Represents a generic event that represents an update of some kind and has a sender of type <typeparamref name="TSender"/>
/// </summary>
/// <param name="sender">The object that experienced the event</param>
/// <param name="delta">The amount of time that has passed since the last time this event was fired</param>
/// <typeparam name="TSender">The type of the object that experienced the event</typeparam>
public delegate void UpdateEvent<in TSender>(TSender sender, TimeSpan delta);

#endregion

#region Game Delegates

/// <summary>
/// Represents an event in the <see cref="Game"/> regarding the changing of a scene
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="newScene">The scene that was just set</param>
/// <param name="oldScene">The scene that was previously set</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
public delegate void GameSceneChangedEvent(Game game, TimeSpan timestamp, Scene newScene, Scene oldScene);

/// <summary>
/// Represents an event in the <see cref="Game"/> regarding a scene
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="scene">The scene that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
public delegate void GameSceneEvent(Game game, TimeSpan timestamp, Scene scene);

/// <summary>
/// Represents an event in the game
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
public delegate void GameEvent(Game game, TimeSpan timestamp);

/// <summary>
/// Represents an event in the game that is fired when the main Window and Renderer are created
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <param name="graphicsManager">The <see cref="GraphicsManager"/> that was created</param>
public delegate void GameGraphicsManagerCreatedEvent(Game game, TimeSpan timestamp, GraphicsManager graphicsManager);

/// <summary>
/// Represents an event in the game regarding the <see cref="Game"/>'s <see cref="IGameLifetime"/>
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <param name="lifetime">The <see cref="IGameLifetime"/> that is the object of the event</param>
public delegate void GameLifetimeChangedEvent(Game game, TimeSpan timestamp, IGameLifetime lifetime);

/// <summary>
/// Represents an event in the game in which <see cref="Game.GameTitle"/> has changed
/// </summary>
/// <param name="game">The <see cref="Game"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <param name="newTitle">The newly set title of the game</param>
/// <param name="oldTitle">The previously set title of the game</param>
public delegate void GameTitleChangedEvent(Game game, TimeSpan timestamp, string newTitle, string oldTitle);

internal delegate void GameSetupScenesEvent(Game game);

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
/// Represents an event in the game regarding a <see cref="Node"/>'s readiness, represented by <see cref="Node.IsActive"/>
/// </summary>
/// <param name="node">The node that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <param name="isReady">Whether or not <paramref name="node"/> became ready at the time this event fired</param>
public delegate void NodeReadinessChangedEvent(Node node, TimeSpan timestamp, bool isReady);

/// <summary>
/// Represents an event in the game regarding a <see cref="Node"/>
/// </summary>
/// <param name="node">The Node that that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
public delegate void NodeEvent(Node node, TimeSpan timestamp);

/// <summary>
/// Represents an event in the game regarding a <see cref="Node"/> interacting with a <see cref="Scene"/>
/// </summary>
/// <param name="node">The Node that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <param name="scene">The Scene in question</param>
public delegate void NodeSceneEvent(Node node, TimeSpan timestamp, Scene? scene);

/// <summary>
/// 
/// </summary>
/// <param name="node">The node that experienced the change</param>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
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
public delegate TNode NodeFactory<TNode>() where TNode : Node;

#endregion

#region FunctionalComponent Delegates

/// <summary>
/// Represents an event in the game regarding a <see cref="FunctionalComponent"/>'s readiness, represented by <see cref="FunctionalComponent.IsReady"/>
/// </summary>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <param name="isReady">Whether or not <paramref name="component"/> became ready at the time this event fired</param>
public delegate void FunctionalComponentReadinessChangedEvent(FunctionalComponent component, TimeSpan timestamp, bool isReady);

/// <summary>
/// Represents an event in the game regarding a <see cref="FunctionalComponent"/>
/// </summary>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
public delegate void FunctionalComponentEvent(FunctionalComponent component, TimeSpan timestamp);

/// <summary>
/// Represents an event in the game regarding a <see cref="FunctionalComponent"/>'s <see cref="FunctionalComponent.Owner"/>
/// </summary>
/// <param name="component">The component that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <param name="node">The <see cref="Node"/> that experienced the change</param>
public delegate void FunctionalComponentNodeEvent(FunctionalComponent component, TimeSpan timestamp, Node node);

#endregion

#region Scene Delegates

/// <summary>
/// Represents a method that, given a <see cref="Game"/> will produce a <see cref="Scene"/>
/// </summary>
/// <param name="game">The <see cref="Game"/> that will own the produced <see cref="Scene"/></param>
public delegate Scene SceneFactory(Game game);

/// <summary>
/// Represents an event in the game regarding a <see cref="Scene"/>
/// </summary>
/// <param name="scene">The scene that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
public delegate void SceneEvent(Scene scene, TimeSpan timestamp);

/// <summary>
/// Represents an event in the game regarding a <see cref="Scene"/> and a <see cref="Node"/>
/// </summary>
/// <param name="scene">The scene that experienced the change</param>
/// <param name="node">The node that experienced the change alongside <paramref name="scene"/></param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
public delegate void SceneNodeEvent(Scene scene, TimeSpan timestamp, Node node);

#endregion

#region GraphicsManager Delegates

/// <summary>
/// Represents an event in the game in which a <see cref="GraphicsManager{TGraphicsContext}"/> stopped or started
/// </summary>
/// <remarks>
/// Specifically, when <see cref="GraphicsManager{TGraphicsContext}.IsRunning"/> changes
/// </remarks>
/// <param name="graphicsManager">The <see cref="GraphicsManager{TGraphicsContext}"/> that experienced the change</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <param name="isRunning">The new value of <see cref="GraphicsManager{TGraphicsContext}.IsRunning"/> at the time this event was fired</param>
public delegate void GraphicsManagerRunStateChanged<TGraphicsContext>(GraphicsManager<TGraphicsContext> graphicsManager, TimeSpan timestamp, bool isRunning)
    where TGraphicsContext : GraphicsContext<TGraphicsContext>;

/// <summary>
/// Represents an event in the game regarding a <see cref="GraphicsManager"/> having finished receiving input
/// </summary>
/// <param name="graphicsManager">The <see cref="GraphicsManager"/> that experienced the change</param>
/// <param name="inputSnapshot">The <see cref="InputSnapshot"/> containing the input data</param>
/// <param name="timestamp">The system time at the moment this event fired</param>
public delegate void GraphicsManagerInputEventHandler(GraphicsManager graphicsManager, InputSnapshot inputSnapshot, DateTime timestamp);

#endregion

#region DrawOperation Delegates

/// <summary>
/// Represents an event regarding a <see cref="DrawOperation{TGraphicsContext}"/>
/// </summary>
/// <typeparam name="TGraphicsContext">The <see cref="GraphicsContext{TSelf}"/> of the <see cref="DrawOperation{TGraphicsContext}"/></typeparam>
/// <param name="drawOperation">The <see cref="DrawOperation{TGraphicsContext}"/> that triggered the event</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
public delegate void DrawOperationEvent<TGraphicsContext>(DrawOperation<TGraphicsContext> drawOperation, TimeSpan timestamp)
    where TGraphicsContext : GraphicsContext<TGraphicsContext>;

/// <summary>
/// Represents an event regarding a <see cref="DrawOperation{TGraphicsContext}"/>
/// </summary>
/// <typeparam name="TGraphicsContext">The <see cref="GraphicsContext{TSelf}"/> of the <see cref="DrawOperation{TGraphicsContext}"/></typeparam>
/// <typeparam name="TData">The type of extra data regarding the event</typeparam>
/// <param name="drawOperation">The <see cref="DrawOperation{TGraphicsContext}"/> that triggered the event</param>
/// <param name="timestamp">The amount of time that has passed since the library's initialization and this event firing</param>
/// <param name="data">Extra data regarding the event</param>
public delegate void DrawOperationEvent<TGraphicsContext, TData>(DrawOperation<TGraphicsContext> drawOperation, TData data, TimeSpan timestamp)
    where TGraphicsContext : GraphicsContext<TGraphicsContext>;

#endregion