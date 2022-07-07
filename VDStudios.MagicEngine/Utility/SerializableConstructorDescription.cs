using MessagePack;
using System.Reflection;

namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Represents a .NET <see cref="Type"/> that can be serialized by MessagePack
/// </summary>
[MessagePackObject]
public struct SerializableConstructorDescription
{
    /// <summary>
    /// The <see cref="Type.AssemblyQualifiedName"/> of the type that contains the method
    /// </summary>
    [Key(0)]
    public string AssemblyQualifiedContainingTypeName { get; init; } = "";

    /// <summary>
    /// The Types that make up the signature of the method
    /// </summary>
    [Key(1)]
    public SerializableTypeDescription[] ConstructorSignature { get; init; } = Array.Empty<SerializableTypeDescription>();

    /// <summary>
    /// Finds the method represented by this instance
    /// </summary>
    /// <returns>The found method, if no exceptions are thrown</returns>
    public ConstructorInfo FetchConstructor()
    {
        var t = Type.GetType(AssemblyQualifiedContainingTypeName) ?? throw new TypeLoadException($"Could not find the type {AssemblyQualifiedContainingTypeName}");
        Type[] ctypes;
        if (ConstructorSignature.Length == 0)
            ctypes = Type.EmptyTypes;
        else
        {
            ctypes = new Type[ConstructorSignature.Length];
            for (int i = 0; i < ConstructorSignature.Length; i++)
                ctypes[i] = ConstructorSignature[i].FetchType();
        }

        return t.GetConstructor(ctypes) ?? throw new MissingMethodException($"Type {t} does not have a constructor with a signature of [.ctor({string.Join(", ", (object[])ctypes)})]");
    }

    /// <summary>
    /// Creates a new <see cref="SerializableConstructorDescription"/> based on a type and a methodName
    /// </summary>
    /// <exception cref="MissingMethodException"/>
    public SerializableConstructorDescription(Type type, params Type[] signature)
    {
        var t = type;

        if (t.ContainsGenericParameters)
            throw new InvalidOperationException("The type owned by the method must be a closed generic type (Have all of its generic parameters replaced by concrete types)");

        AssemblyQualifiedContainingTypeName = t.AssemblyQualifiedName ?? throw new InvalidOperationException("The type the described method belongs to does not have an AssemblyQualifiedName");

        var types = new SerializableTypeDescription[signature.Length];
        for (int i = 0; i < signature.Length; i++) types[i] = new SerializableTypeDescription(signature[i]);
    }

    /// <summary>
    /// Creates a new <see cref="SerializableConstructorDescription"/> 
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public SerializableConstructorDescription(ConstructorInfo ctorInfo)
    {
        var t = ctorInfo.ReflectedType ?? ctorInfo.DeclaringType ?? throw new InvalidOperationException("The described method does not have an owning type");

        if (t.ContainsGenericParameters)
            throw new InvalidOperationException("The type owned by the method must be a closed generic type (Have all of its generic parameters replaced by concrete types)");

        AssemblyQualifiedContainingTypeName = t.AssemblyQualifiedName ?? throw new InvalidOperationException("The type the described method belongs to does not have an AssemblyQualifiedName");
        
        var para = ctorInfo.GetParameters();
        var types = new SerializableTypeDescription[para.Length];
        for (int i = 0; i < para.Length; i++) types[i] = new SerializableTypeDescription(para[i].ParameterType);
    }
}