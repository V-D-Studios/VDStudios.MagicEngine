using MessagePack;
using System.Reflection;

namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Represents a .NET <see cref="Type"/> that can be serialized by MessagePack
/// </summary>
[MessagePackObject]
public struct SerializableMethodDescription
{
    /// <summary>
    /// The <see cref="Type.AssemblyQualifiedName"/> of the type that contains the method
    /// </summary>
    [Key(0)]
    public string AssemblyQualifiedContainingTypeName { get; init; } = "";

    /// <summary>
    /// The Name of the method
    /// </summary>
    [Key(1)]
    public string MethodName { get; init; } = "";

    /// <summary>
    /// Finds the method represented by this instance
    /// </summary>
    /// <returns>The found method, if no exceptions are thrown</returns>
    public MethodInfo FetchMethod()
        => Type.GetType(AssemblyQualifiedContainingTypeName, true)!.GetMethod(MethodName) ?? throw new MissingMethodException($"Method {MethodName} does not exist under type {AssemblyQualifiedContainingTypeName}");

    /// <summary>
    /// Finds the method represented by this instance and creates a delegate that represents the method
    /// </summary>
    /// <remarks>
    /// If <paramref name="target"/> is a value type, it'll get boxed by the delegate generation anyway, no need to worry about the <see cref="object"/> downcast at this point.
    /// </remarks>
    /// <param name="target">The target of the delegate, null if the method is static</param>
    /// <typeparam name="TDelegate">The type of <see cref="Delegate"/> to generate</typeparam>
    /// <returns>The generated <see cref="Delegate"/></returns>
    public TDelegate FetchMethod<TDelegate>(object? target) where TDelegate : Delegate
        => FetchMethod().CreateDelegate<TDelegate>(target);

    /// <summary>
    /// Creates a new <see cref="SerializableMethodDescription"/> based on <typeparamref name="TDelegate"/>
    /// </summary>
    /// <typeparam name="TDelegate">The type that the returned <see cref="SerializableMethodDescription"/> will describe</typeparam>
    /// <returns>A new <see cref="SerializableMethodDescription"/> describing <typeparamref name="TDelegate"/></returns>
    public static SerializableMethodDescription Describe<TDelegate>(TDelegate method) where TDelegate : Delegate
        => new(method.Method);

    /// <summary>
    /// Creates a new <see cref="SerializableMethodDescription"/> based on a type and a methodName
    /// </summary>
    /// <exception cref="MissingMethodException"/>
    public SerializableMethodDescription(Type type, string methodName) 
        : this(type.GetMethod(methodName) ?? throw new MissingMethodException($"Method {methodName} does not exist under type {type.AssemblyQualifiedName}")) { }

    /// <summary>
    /// Creates a new <see cref="SerializableMethodDescription"/> 
    /// </summary>
    /// <exception cref="MissingMethodException"/>
    public SerializableMethodDescription(SerializableTypeDescription typeDesc, string methodName)
        : this(typeDesc.GetType(), methodName) { }

    /// <summary>
    /// Creates a new <see cref="SerializableMethodDescription"/> 
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public SerializableMethodDescription(MethodInfo methodInfo)
    {
        var t = methodInfo.ReflectedType ?? methodInfo.DeclaringType ?? throw new InvalidOperationException("The described method does not have an owning type");

        if (t.ContainsGenericParameters)
            throw new InvalidOperationException("The type owned by the method must be a closed generic type (Have all of its generic parameters replaced by concrete types)");

        AssemblyQualifiedContainingTypeName = t.AssemblyQualifiedName ?? throw new InvalidOperationException("The type the described method belongs to does not have an AssemblyQualifiedName");
        MethodName = methodInfo.Name;
    }
}