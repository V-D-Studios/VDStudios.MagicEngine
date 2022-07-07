using MessagePack;

namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Represents a .NET <see cref="Type"/> that can be serialized by MessagePack
/// </summary>
[MessagePackObject]
public struct SerializableTypeDescription
{
    /// <summary>
    /// The <see cref="Type.AssemblyQualifiedName"/> of the type this represents
    /// </summary>
    [Key(0)]
    public string AssemblyQualifiedName { get; init; } = "";

    /// <summary>
    /// Finds the type represented by this instance
    /// </summary>
    /// <returns>The found type, if no exceptions are thrown</returns>
    public Type FetchType() => Type.GetType(AssemblyQualifiedName, true)!;

    /// <summary>
    /// Creates a new <see cref="SerializableTypeDescription"/> based on <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type that the returned <see cref="SerializableTypeDescription"/> will describe</typeparam>
    /// <returns>A new <see cref="SerializableTypeDescription"/> describing <typeparamref name="T"/></returns>
    public static SerializableTypeDescription Describe<T>() => new(typeof(T));

    /// <summary>
    /// Creates a new <see cref="SerializableTypeDescription"/> 
    /// </summary>
    public SerializableTypeDescription(Type type)
    {
        AssemblyQualifiedName = type.AssemblyQualifiedName ?? throw new InvalidOperationException($"Type {type} does not have an AssemblyQualifiedName");
    }
}
