namespace VDStudios.MagicEngine.Internal;

/// <summary>
/// Represents an object of the <see cref="Game"/>
/// </summary>
/// <remarks>
/// This interface should not be implemented by user code and is meant to use internally only
/// </remarks>
public interface IGameObject
{
    /// <summary>
    /// Caches <see cref="GameObjectId.ToString(string?, IFormatProvider?)"/> from <see cref="Id"/> when called for the first time, as it is a <see langword="readonly"/> <see langword="struct"/> in a <see langword="readonly"/> <see langword="property"/>. Saves memory.
    /// </summary>
    public string IdString { get; }

    /// <summary>
    /// The <see cref="MagicEngine.Game"/> this <see cref="GameObject"/> belongs to.
    /// </summary>
    public Game Game { get; }

    /// <summary>
    /// An unique Id that identifies this <see cref="GameObject"/> for debugging and logging purposes
    /// </summary>
    public GameObjectId Id { get; }

    /// <summary>
    /// Logging information. The facility the GameObject operates for
    /// </summary>
    public string Facility { get; }

    /// <summary>
    /// Logging information. The area the GameObject belongs to
    /// </summary>
    public string Area { get; }

    /// <summary>
    /// <see langword="true"/> if this <see cref="GameObject"/> has already been disposed of
    /// </summary>
    public bool IsDisposed { get; }

    /// <summary>
    /// An optional name for debugging purposes
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets an identifying name for this <see cref="GameObject"/> without its <see cref="Id"/>
    /// </summary>
    /// <remarks>
    /// Usually, this looks like: <c><see cref="Name"/>-TypeName</c>
    /// </remarks>
    /// <returns></returns>
    public string GetGameObjectName();

    internal static string CreateGameObjectName(IGameObject obj)
        => Helper.BuildTypeNameAsCSharpTypeExpression(obj.GetType());
}