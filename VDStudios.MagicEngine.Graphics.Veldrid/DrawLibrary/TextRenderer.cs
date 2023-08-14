using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawLibrary;

/// <summary>
/// A text renderer that renders a string of text from a given source
/// </summary>
public abstract class TextRenderer : SharedDrawResource
{
    /// <summary>
    /// Renders the given string of text into a Texture
    /// </summary>
    /// <param name="text">The string of text to render</param>
    /// <param name="size">The font size in points (pt)</param>
    /// <param name="commandList">The command list to use for this context</param>
    /// <param name="textureDescription">The description for the texture to create</param>
    /// <param name="lineSeparation">The amount of space to add between lines. Functions as a multiplier to <paramref name="size"/></param>
    /// <param name="factory">The <see cref="ResourceFactory"/> to create the resulting <see cref="Texture"/> with</param>
    /// <returns>The resulting <see cref="Texture"/> with the text rendered on it</returns>
    public abstract Texture RenderText(string text, float size, CommandList commandList, ResourceFactory factory, ref TextureDescription textureDescription, float lineSeparation = 1f);

    /// <summary>
    /// Renders the given string of text into a Texture
    /// </summary>
    /// <param name="text">The string of text to render</param>
    /// <param name="size">The font size in points (pt)</param>
    /// <param name="commandList">The command list to use for this context</param>
    /// <param name="textureDescription">The description for the texture to create</param>
    /// <param name="lineSeparation">The amount of space to add between lines. Functions as a multiplier to <paramref name="size"/></param>
    /// <param name="factory">The <see cref="ResourceFactory"/> to create the resulting <see cref="Texture"/> with</param>
    /// <returns>The resulting <see cref="Texture"/> with the text rendered on it</returns>
    public Texture RenderText(string text, float size, CommandList commandList, ResourceFactory factory, TextureDescription textureDescription, float lineSeparation = 1f)
        => RenderText(text, size, commandList, factory, ref textureDescription, lineSeparation);
}
