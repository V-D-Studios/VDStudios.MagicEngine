using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// Represents a set of shader functions and resource bindings that can be finalized into a complete GLSL Style SPIR-V shader string
/// </summary>
public class ShaderBuilder
{
    #region Regexes

    /// <summary>
    /// Analyzes the given shader in search of resource bindings that can then be used to re-organize a <see cref="ResourceBuilder"/>
    /// </summary>
    /// <remarks>
    /// The first group is 'set', which captures the set index of the binding; the second is 'binding' which captures the binding slot, and finally 'name' which captures the variable name
    /// </remarks>
#warning Use Regex Generator
    protected static readonly Regex ShaderBoundResourcesRegex 
        = new(@"layout\s*\((?:\s*set\s*=\s*(?<set>\d+)\s*,)?\s*binding\s*=\s*(?<binding>\d+)\s*,?\s*\w*\)\s*\w+\s*\w*\s+(?<name>\w+)\s*[;{]", RegexOptions.Compiled);

    /// <summary>
    /// Analyzes the given shader in search of unbound resource statements that can then be filled by this <see cref="ShaderBuilder"/>
    /// </summary>
    /// <remarks>
    /// The first group is 'arguments', which captures the arguments of the binding other than the <c>set</c> and <c>binding</c>; and finally 'name' which captures the variable name
    /// </remarks>
    protected static readonly Regex ShaderUnboundResourcesRegex
        = new(@"#binding\s*(?:\((?<arguments>(?:\s*\w+\s*,?)*)\))?(?<type>\s*\w+\s*\w*\s+)(?<name>\w+)\s*(?<body>;|(?:{(?:.|\n)*?};))", RegexOptions.Compiled);

    #endregion

    #region Helper Classes

    protected readonly struct ResourceEntry
    {
        public StringBuilder? Arguments { get; init; }
        public StringBuilder? Body { get; init; }
        public StringBuilder Typing { get; init; }
        public string Name { get; init; }
    }

    #endregion

    private readonly List<ResourceEntry> ResourceEntries = new();
    private readonly List<StringBuilder> Functions = new();
    private StringBuilder? Main;

    public ShaderBuilder() { }

    /// <summary>
    /// Clears this <see cref="ShaderBuilder"/> of all data
    /// </summary>
    public void Clear()
    {
        ResourceEntries.Clear();
        foreach (var i in Functions)
            SharedObjectPools.StringBuilderPool.Return(i);
        Functions.Clear();
        if (Main is StringBuilder main)
            SharedObjectPools.StringBuilderPool.Return(main);
        Main = null;
    }

    public void Build()
    {
        ResourceEntry[] entries = null!;
        StringBuilder[] functions = null!;
        int entryCount = 0;
        int funcCount = 0;
        bool hasResources = false;
        bool hasFunctions = false;

        var final = SharedObjectPools.StringBuilderPool.Rent().Clear();

        try
        {
            if (Functions.Count > 0)
            {
                hasFunctions = true;
                lock (Functions)
                {
                    functions = ArrayPool<StringBuilder>.Shared.Rent(funcCount = Functions.Count);
                    Functions.CopyTo(functions);
                }
            }

            try
            {
                if (hasResources)
                {

                }
            }
            finally
            {
                SharedObjectPools.StringBuilderPool.Return(final);
            }
        }
        finally
        {
            if (entries != null)
                ArrayPool<ResourceEntry>.Shared.Return(entries);
            if (functions != null)
                ArrayPool<StringBuilder>.Shared.Return(functions);
        }
    }

    private void BuildFunctionSet(StringBuilder builder)
    {
    }

    private void BuildBindingSet(StringBuilder builder, ResourceSet[] sets, ResourceLayout[] layouts)
    {
        ResourceEntry[] entries;
        int entryCount;

        lock (ResourceEntries)
        {
            if (ResourceEntries.Count <= 0)
                return;

            entries = ArrayPool<ResourceEntry>.Shared.Rent(entryCount = ResourceEntries.Count);
            ResourceEntries.CopyTo(entries);
        }

        for (int i = 0; i < entryCount; i++)
        {
            var entry = entries[i];
            layouts.First(x => x.)
            BuildBinding(builder, entry.)
        }
    }

    private static StringBuilder BuildBinding
        (StringBuilder builder, int set, int binding, string name, StringBuilder type, StringBuilder args, StringBuilder body)
    {
        builder.Append($"layout(set={set},binding={binding}");
        if (args.Length > 0) 
            builder.Append(',').Append(args);
        return builder.Append(") ").Append(type).Append(' ').Append(name).Append(body);
    }

    /* 
     * First, document ShaderBuilder
     * The shader can be analyzed for bound resources first, then it can be analyzed for unbound resources. Or would it be better to just work with unbound resources?
     * Somehow, if the shader throws an exception, THE CODE GENERATED BY THIS CLASS **NEEDS** TO SHOW UP SOMEWHERE! otherwise, how the heck is it going to be debugged?
     */
}
