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
    private static readonly ObjectPool<Dictionary<int, ResourceEntry>> ResourceEntryDictionaryPool = new(x => new(10), x => x.Clear(), 3, 3);

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

    /// <summary>
    /// Represents a resource binding in the shader
    /// </summary>
    protected struct ResourceEntry
    {
        /// <summary>
        /// The arguments of the binding, for example, with <c>(set=0,binding=0,rgba34)</c>, <c>rgba34</c> would be the argument
        /// </summary>
        public string? Arguments;
        
        /// <summary>
        /// The body or the ending of the binding. It can be either a ';', or the struct body
        /// </summary>
        public string Body;

        /// <summary>
        /// The types of the binding, such as <c>uniform image2d</c>
        /// </summary>
        public string Typing;

        /// <summary>
        /// The variable identifier of the binding. Does not include identifiers of the struct definition. Must match with an element description
        /// </summary>
        public string Name;
    }


    #endregion

    private readonly List<ResourceEntry> ResourceEntries = new();

    /// <summary>
    /// Instances a new object of type <see cref="ShaderBuilder"/>
    /// </summary>
    public ShaderBuilder() { }

    /// <summary>
    /// Clears this <see cref="ShaderBuilder"/> of all data
    /// </summary>
    public void Clear()
    {
        ResourceEntries.Clear();
    }

    /// <summary>
    /// Takes in a base shader, WITHOUT ANY RESOURCE BINDINGS, in which to inject the bindings analyzed from <see cref="ResourceSet"/> and described by this builder
    /// </summary>
    /// <param name="baseShader"></param>
    /// <param name="sets"></param>
    /// <returns></returns>
    public string BuildAgainst(string baseShader, ResourceSet[] sets)
    {
        int start = baseShader.IndexOf('\n', baseShader.IndexOf(@"#version"));
        var builder = SharedObjectPools.StringBuilderPool.Rent();
        var bindings = SharedObjectPools.StringBuilderPool.Rent();
        try
        {
            builder.Clear();
            bindings.Clear();
            builder.Append(baseShader);
            BuildBindingSet(bindings, sets);
            builder.Append(bindings, start, bindings.Length);
            return builder.ToString();
        }
        finally
        {
            SharedObjectPools.StringBuilderPool.Return(builder);
            SharedObjectPools.StringBuilderPool.Return(bindings);
        }
    }

    private void BuildBindingSet(StringBuilder builder, ResourceSet[] sets)
    {
        Dictionary<int, ResourceEntry> entries;

        lock (ResourceEntries)
        {
            if (ResourceEntries.Count <= 0)
                return;

            entries = ResourceEntryDictionaryPool.Rent();
            for (int i = 0; i < ResourceEntries.Count; i++)
                entries.Add(i, ResourceEntries[i]);
        }

        try
        {
            for (int set = 0; set < sets.Length; set++)
            {
                var layout = sets[set].Layout;
                for (int binding = 0; binding < layout.ElementCount; binding++)
                {
                    var element = layout[binding].Name;
                    var (index, entry) = entries.FirstOrDefault(x => x.Value.Name == element);
                    entries.Remove(index);
                    BuildBinding(builder, set, binding, entry.Name, entry.Typing, entry.Arguments, entry.Body);
                }
            }
        }
#if DEBUG
        catch
        {
            ; // For breakpoint purposes
            throw;
        }
#endif
        finally
        {
            ResourceEntryDictionaryPool.Return(entries);
        }
    }

    private static StringBuilder BuildBinding
        (StringBuilder builder, int set, int binding, string name, string type, string? args, string body)
    {
        builder.Append($"layout(set={set},binding={binding}");
        if (args?.Length is >0) 
            builder.Append(',').Append(args);
        return builder.Append(") ").Append(type).Append(' ').Append(name).Append(body);
    }

    /* 
     * First, document ShaderBuilder
     * The shader can be analyzed for bound resources first, then it can be analyzed for unbound resources. Or would it be better to just work with unbound resources?
     * Somehow, if the shader throws an exception, THE CODE GENERATED BY THIS CLASS **NEEDS** TO SHOW UP SOMEWHERE! otherwise, how the heck is it going to be debugged?
     */
}
