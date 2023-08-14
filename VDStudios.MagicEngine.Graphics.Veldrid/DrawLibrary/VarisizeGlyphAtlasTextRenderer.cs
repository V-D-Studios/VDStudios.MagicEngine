using System.Collections.Immutable;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid.Graphics;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawLibrary;

/// <summary>
/// A <see cref="TextRenderer"/> that builds textures based on a glyph atlas, where the glyphs themselves may vary in size
/// </summary>
public class VarisizeGlyphAtlasTextRenderer : TextRenderer
{
    /// <summary>
    /// Information about a given Glyph in the atlas
    /// </summary>
    /// <param name="Character">The character that this glyph represents</param>
    /// <param name="Size">The size of the glyph</param>
    /// <param name="Offset">The offset the glyph resides at</param>
    public readonly record struct GlyphDefinition(char Character, USize Size, UPoint Offset);

    private readonly TextureFactory AtlasFactory;
    private readonly Dictionary<char, GlyphDefinition> GlyphMap;

    /// <summary>
    /// Creates a new object of type <see cref="GlyphAtlasTextRenderer"/>
    /// </summary>
    /// <param name="glyphs">Information about the glyphs stored in the atlas</param>
    /// <param name="spaceSize">The size of a ' ' character</param>
    /// <param name="comparer">The comparer that will be used when fetching character-glyph relations -- This can be used, for example, to create a text renderer that uses the same glyphs for upper and lowercase letters by using <see cref="char.ToUpper(char)"/></param>
    /// <param name="variableLineHeight"><see langword="true"/> if the line height should be dependent on the tallest glyph being rendered, <see langword="false"/> to have it set to the tallest defined glyph</param>
    /// <param name="atlasFactory">A factory that creates the texture that contains the ordered glyphs -- Vertical brakes are fine as long as the spacing is even, avoid empty characters unless at the end</param>
    public VarisizeGlyphAtlasTextRenderer(IEnumerable<GlyphDefinition> glyphs, USize spaceSize, TextureFactory atlasFactory, IEqualityComparer<char>? comparer = null, bool variableLineHeight = false)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        AtlasFactory = atlasFactory ?? throw new ArgumentNullException(nameof(atlasFactory));

        GlyphMap = new Dictionary<char, GlyphDefinition>(CharEqualityComparer = comparer ?? EqualityComparer<char>.Default);
        GlyphInformation = glyphs.ToImmutableArray();

        if (GlyphInformation.Length <= 0)
            throw new ArgumentException("The enumerable contains no elements", nameof(glyphs));

        SpaceSize = spaceSize;

        if (variableLineHeight)
        {
            for (int i = 0; i < GlyphInformation.Length; i++)
            {
                var c = GlyphInformation[i];
                if (c.Character is '\t' or '\n' or ' ')
                    throw new ArgumentException("A glyph cannot represent the characters '\\t', '\\n' or ' '", nameof(glyphs));
                if (GlyphMap.TryAdd(c.Character, c) is false)
                    throw new ArgumentException("Multiple glyph definitions for a single character are not allowed", nameof(glyphs));
            }
        }
        else
        {
            uint lheight = 0;
            for (int i = 0; i < GlyphInformation.Length; i++)
            {
                var c = GlyphInformation[i];
                if (c.Character is '\t' or '\n' or ' ')
                    throw new ArgumentException("A glyph cannot represent the characters '\\t', '\\n' or ' '", nameof(glyphs));
                if (GlyphMap.TryAdd(c.Character, c) is false)
                    throw new ArgumentException("Multiple glyph definitions for a single character are not allowed", nameof(glyphs));

                lheight = uint.Max(c.Size.Height, lheight);
            }

            LineHeight = uint.Max(spaceSize.Height, lheight);
        }
    }

    /// <summary>
    /// The comparer that will be used when fetching character-glyph relations
    /// </summary>
    public IEqualityComparer<char> CharEqualityComparer { get; }

    /// <summary>
    /// The default amount of pixels to space each character by
    /// </summary>
    public uint DefaultCharacterSpacing { get; set; }

    /// <summary>
    /// The size of a single ' ' character
    /// </summary>
    public USize SpaceSize { get; }

    /// <summary>
    /// The minimum height a line must have to accomodate the tallest glyph
    /// </summary>
    public uint? LineHeight { get; }

    /// <summary>
    /// The glyphs in the atlas in the order that they appear in
    /// </summary>
    public ImmutableArray<GlyphDefinition> GlyphInformation { get; }

    /// <summary>
    /// The texture atlas where the glyphs reside
    /// </summary>
    public Texture? Atlas { get; private set; }

    /// <inheritdoc/>
    public override ValueTask Update(TimeSpan delta, GraphicsManager manager, GraphicsDevice device, CommandList commandList)
        => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory)
    {
        Atlas?.Dispose();
        Atlas = AtlasFactory.Invoke(device, factory);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Renders the given string of text into a Texture
    /// </summary>
    /// <remarks>
    /// <see cref="GlyphAtlasTextRenderer"/> ignores the font size, and merely copies the glyphs from the atlas into a <see cref="Texture"/>
    /// </remarks>
    /// <param name="text">The string of text to render</param>
    /// <param name="size">The font size in points (pt) -- For <see cref="GlyphAtlasTextRenderer"/>, this parameter is ignored</param>
    /// <param name="commandList">The command list to use for this context</param>
    /// <param name="textureDescription">The description for the texture to create</param>
    /// <param name="lineSeparation">The amount of space to add between lines. Functions as a multiplier to <paramref name="size"/></param>
    /// <param name="factory">The <see cref="ResourceFactory"/> to create the resulting <see cref="Texture"/> with</param>
    /// <returns>The resulting <see cref="Texture"/> with the text rendered on it</returns>
    public override Texture RenderText(string text, float size, CommandList commandList, ResourceFactory factory, ref TextureDescription textureDescription, float lineSeparation = 1)
    {
        if (Atlas is not Texture atlas)
            throw new InvalidOperationException("Cannot render text with a null atlas");

        uint charspacing = DefaultCharacterSpacing;

        uint maxlen = 0;
        uint lines = 0;
        uint len = 0;
        if (LineHeight is not uint lheight)
        {
            lheight = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\t')
                {
                    len += SpaceSize.Width * 2 + charspacing;
                    lheight = uint.Max(lheight, SpaceSize.Height);
                }
                else if (c == ' ')
                {
                    len += SpaceSize.Width + charspacing;
                    lheight = uint.Max(lheight, SpaceSize.Height);
                }
                else if (c == '\n')
                {
                    maxlen = uint.Max(maxlen, len);
                    lines++;
                    len = 0;
                }
                else if (GlyphMap.TryGetValue(c, out var gl))
                {
                    len += gl.Size.Width + charspacing;
                    lheight = uint.Max(lheight, gl.Size.Height);
                }
            }
            maxlen -= charspacing;
        }
        else
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\t')
                    len += SpaceSize.Width * 2 + charspacing;
                else if (c == ' ')
                    len += SpaceSize.Width + charspacing;
                else if (c == '\n')
                {
                    maxlen = uint.Max(maxlen, len);
                    lines++;
                    len = 0;
                }
                else if (GlyphMap.TryGetValue(c, out var gl))
                    len += gl.Size.Width + charspacing;
            }
            maxlen -= charspacing;
        }

        uint tw = maxlen;
        uint th = (uint)float.Ceiling(lines * lheight * lineSeparation);

        textureDescription.Width = tw;
        textureDescription.Height = th;

        var target = factory.CreateTexture(ref textureDescription);

        uint chpos = 0;
        uint cline = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c is '\t')
            {
                chpos += SpaceSize.Width * 2 + charspacing;
                continue;
            }
            else if (c is ' ')
            {
                chpos += SpaceSize.Width + charspacing;
                continue;
            }
            else if (c is '\n')
            {
                chpos = 0;
                cline++;
                continue;
            }

            if (GlyphMap.TryGetValue(c, out var glyph) is false) continue;

            commandList.CopyTexture(
                atlas,
                glyph.Offset.X, glyph.Offset.Y, 0, 0, 0,

                target,
                chpos, (uint)(cline * lheight * lineSeparation), 0, 0, 0,

                glyph.Size.Width, glyph.Size.Height, 0, 0
            );

            chpos += glyph.Size.Width + charspacing;
        }

        return target;
    }
}
