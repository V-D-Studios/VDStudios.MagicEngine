using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// Represents a set of shader functions and resource bindings that can be finalized into a complete GLSL Style SPIR-V shader string
/// </summary>
public class ShaderBuilder
{
    private static readonly ObjectPool<Dictionary<string, (int set, int binding)>> ResourceEntryDictionaryPool = new(x => new(10), x => x.Clear(), 3, 3);

    #region Regexes

    /// <summary>
    /// Analyzes the given shader in search of resource bindings that can then be used to re-organize a <see cref="ResourceBuilder"/>
    /// </summary>
    /// <remarks>
    /// The first group is 'set', which captures the set index of the binding; the second is 'binding' which captures the binding slot, and finally 'name' which captures the variable name
    /// </remarks>
#warning Use Regex Generator
    protected static readonly Regex ShaderBoundResourcesRegex 
        = new(@"layout\s*\((?:\s*set\s*=\s*(?<set>\d+)\s*,)?\s*binding\s*=\s*(?<binding>\d+)\s*,?\s*\w*\)\s*\w+\s*\w*\s+(?<name>\w+)\s*[;{]", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    /// <summary>
    /// Analyzes the given shader in search of unbound resource statements that can then be filled by this <see cref="ShaderBuilder"/>
    /// </summary>
    /// <remarks>
    /// The first group is 'arguments', which captures the arguments of the binding other than the <c>set</c> and <c>binding</c>; and finally 'name' which captures the variable name
    /// </remarks>
    protected static readonly Regex ShaderUnboundResourcesRegex
        = new(@"#binding\s*(?:\((?<arguments>(?:\s*\w+\s*,?)*)\))?(?<type>\s*\w+\s*\w*\s+)(?<name>\w+)\s*(?<body>;|(?:{(?:.|\n)*?};))", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    #endregion

    #region Helper Classes

    /// <summary>
    /// Represents a resource binding in the shader
    /// </summary>
    public struct ResourceEntry : IEquatable<ResourceEntry>
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

        /// <inheritdoc/>
        public bool Equals(ResourceEntry other) => other.Name == Name;

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is ResourceEntry entry && Equals(entry);

        /// <inheritdoc/>
        public override int GetHashCode() => Name.GetHashCode();

        /// <inheritdoc/>
        public static bool operator ==(ResourceEntry left, ResourceEntry right) => left.Equals(right);

        /// <inheritdoc/>
        public static bool operator !=(ResourceEntry left, ResourceEntry right) => !(left == right);
    }

    #endregion

    private readonly List<ResourceEntry> ResourceEntries = new();

    /// <summary>
    /// The base shader this <see cref="ShaderBuilder"/> will inject resource bindings into
    /// </summary>
    public string ShaderBase { get; }

    /// <summary>
    /// Instances a new object of type <see cref="ShaderBuilder"/>
    /// </summary>
    public ShaderBuilder(string shaderbase, string bindings) 
    {
        ShaderBase = shaderbase;
        Add(bindings);
    }

    /// <summary>
    /// Matches the resource bindings described in the string and adds them into this <see cref="ShaderBuilder"/>
    /// </summary>
    /// <param name="resources">A string containing the resource bindings of the shader</param>
    public void Add(string resources)
    {
        var matches = ShaderUnboundResourcesRegex.Matches(resources);
        foreach (Match match in matches)
        {
            match.Groups.TryGetValue("arguments", out var argumentsGroup);
            if (!match.Groups.TryGetValue("type", out var typeGroup) || !typeGroup.Success)
                throw new ArgumentException("One of the matches does not have a valid type", nameof(resources));
            if (!match.Groups.TryGetValue("body", out var bodyGroup) || !bodyGroup.Success)
                throw new ArgumentException("One of the matches does not have a valid body or ending", nameof(resources));
            if (!match.Groups.TryGetValue("name", out var nameGroup) || !nameGroup.Success)
                throw new ArgumentException("One of the matches does not have a valid name", nameof(resources));

            Add(nameGroup.Value, typeGroup.Value, bodyGroup.Value, argumentsGroup?.Value);
        }
    }

    /// <summary>
    /// Adds a resource binding entry into the <see cref="ShaderBuilder"/>
    /// </summary>
    /// <param name="name">The variable identifier of the binding. Does not include identifiers of the struct definition. Must match with an element description</param>
    /// <param name="typing">The types of the binding, such as <c>uniform image2d</c></param>
    /// <param name="body">The body or the ending of the binding. It can be either a ';', or the struct body</param>
    /// <param name="arguments">The arguments of the binding, for example, with <c>(set=0,binding=0,rgba34)</c>, <c>rgba34</c> would be the argument</param>
    public void Add(string name, string typing, string body, string? arguments = null)
        => Add(new ResourceEntry()
        {
            Name = name,
            Typing = typing,
            Body = body,
            Arguments = arguments
        });

    /// <summary>
    /// Adds a resource binding entry into the <see cref="ShaderBuilder"/>
    /// </summary>
    /// <param name="entry">The entry describing essential data about the binding</param>
    public void Add(ResourceEntry entry)
    {
        lock (ResourceEntries)
        {
            if (ResourceEntries.Contains(entry))
                throw new InvalidOperationException($"This {nameof(ShaderBuilder)} already contains a {nameof(ResourceEntry)} with the name \"{entry.Name}\"");
            ResourceEntries.Add(entry);
        }
    }

    /// <summary>
    /// The amount of <see cref="ResourceEntry"/>s this Builder currently has
    /// </summary>
    public int Count => ResourceEntries.Count;

    /// <summary>
    /// Clears this <see cref="ShaderBuilder"/> of all data
    /// </summary>
    public void Clear()
    {
        ResourceEntries.Clear();
    }

    /// <summary>
    /// Sorts through and injects the bindings analyzed from <see cref="ResourceSet"/> and described by this builder
    /// </summary>
    /// <param name="sets"></param>
    /// <returns></returns>
    public string BuildAgainst(ResourceSet[] sets)
    {
        ArgumentNullException.ThrowIfNull(sets);
        int start = ShaderBase.IndexOf('\n', ShaderBase.IndexOf(@"#version"));
        var builder = SharedObjectPools.StringBuilderPool.Rent();
        var bindings = SharedObjectPools.StringBuilderPool.Rent();
        try
        {
            builder.Clear();
            bindings.Clear();
            builder.Append(ShaderBase);
            BuildBindingSet(bindings, sets);
            builder.Insert(start, bindings);
#if DEBUG
            var result = builder.ToString();
            return result;
#else
            return builder.ToString();
#endif
        }
        finally
        {
            SharedObjectPools.StringBuilderPool.Return(builder);
            SharedObjectPools.StringBuilderPool.Return(bindings);
        }
    }

    private void BuildBindingSet(StringBuilder builder, ResourceSet[] sets)
    {
        Dictionary<string, (int set, int binding)>? resources = null;
        ResourceEntry[]? entries = null;
        int entrycount;

        try
        {
            lock (ResourceEntries)
            {
                if (ResourceEntries.Count <= 0)
                    return;
                entries = ArrayPool<ResourceEntry>.Shared.Rent(entrycount = ResourceEntries.Count);
                ResourceEntries.CopyTo(entries);
            }

            resources = ResourceEntryDictionaryPool.Rent();
            for (int set = 0; set < sets.Length; set++)
            {
                var layout = sets[set].Layout;
                for (int binding = 0; binding < layout.ElementCount; binding++)
                    resources[layout[binding].Name] = (set, binding);
            }

            for (int i = 0; i < entrycount; i++)
            {
                var entry = entries[i];
                var name = entry.Name;
                if (resources.Remove(name, out var location) is false)
                    throw new InvalidOperationException($"Could not find a resource in the set by the name of {name}");

                BuildBinding(builder, location.set, location.binding, name, entry.Typing, entry.Arguments, entry.Body);
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
            if (resources is not null)
                ResourceEntryDictionaryPool.Return(resources);
            if (entries is not null)
                ArrayPool<ResourceEntry>.Shared.Return(entries);
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
