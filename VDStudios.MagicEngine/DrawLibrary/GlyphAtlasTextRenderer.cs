using SDL2.NET;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// A <see cref="TextRenderer"/> that builds textures based on a glyph atlas
/// </summary>
public class GlyphAtlasTextRenderer : TextRenderer
{
    private readonly TextureFactory AtlasFactory;
    private readonly Dictionary<char, uint> Indexes;

    /// <summary>
    /// Creates a new object of type <see cref="GlyphAtlasTextRenderer"/>
    /// </summary>
    /// <param name="charset">The glyphs in the atlas in the order that they appear in</param>
    /// <param name="comparer">The comparer that will be used when fetching character-glyph relations -- This can be used, for example, to create a text renderer that uses the same glyphs for upper and lowercase letters by using <see cref="char.ToUpper(char)"/></param>
    /// <param name="glyphSize">The size of each glyph in the atlas -- <b>MUST be evenly divisible by the size of the atlas!</b></param>
    /// <param name="atlasFactory">A factory that creates the texture that contains the ordered glyphs -- Vertical brakes are fine as long as the spacing is even, avoid empty characters unless at the end</param>
    public GlyphAtlasTextRenderer(string charset, USize glyphSize, TextureFactory atlasFactory, IEqualityComparer<char>? comparer = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(charset);
        if (glyphSize.Height == 0 || glyphSize.Width == 0)
            throw new ArgumentException("Both values of glyphSize must be larger than 0", nameof(glyphSize));
        CharacterSet = charset;
        AtlasFactory = atlasFactory ?? throw new ArgumentNullException(nameof(atlasFactory));

        Indexes = new Dictionary<char, uint>(CharEqualityComparer = comparer ?? EqualityComparer<char>.Default);
        for (int i = 0; i < charset.Length; i++)
            if (charset[i] is not '\t' and not '\n' and not ' ')
                Indexes[charset[i]] = (uint)i;
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
    /// The size of each glyph in the atlas
    /// </summary>
    public USize GlyphSize { get; }

    /// <summary>
    /// The glyphs in the atlas in the order that they appear in
    /// </summary>
    public string CharacterSet { get; }

    /// <summary>
    /// The texture atlas where the glyphs reside
    /// </summary>
    public Texture? Atlas { get; private set; }

    /// <summary>
    /// The amount of glyph slots the Atlas has in each direction
    /// </summary>
    public Size AtlasSlots { get; private set; }

    /// <inheritdoc/>
    public override ValueTask Update(TimeSpan delta, GraphicsManager manager, GraphicsDevice device, CommandList commandList)
        => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory)
    {
        Atlas?.Dispose();
        Atlas = AtlasFactory.Invoke(device, factory);

        var x = GlyphSize.Width / (double)Atlas.Width;
        if (double.Floor(x) < x)
            throw new InvalidOperationException("The width of the atlas is not evenly divisible by the width of each glyph");

        var y = GlyphSize.Height / (double)Atlas.Height;
        if (double.Floor(y) < y)
            throw new InvalidOperationException("The height of the atlas is not evenly divisible by the height of each glyph");

        var xx = GlyphSize.Width / Atlas.Width;
        var yy = GlyphSize.Height / Atlas.Height;

        AtlasSlots = new Size((int)xx, (int)yy);
        if (AtlasSlots.Width * AtlasSlots.Height < CharacterSet.Length)
            throw new InvalidOperationException("The atlas is not big enough, according to its glyph size, to fit the character set");

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

        var gs = GlyphSize;
        uint charspacing = DefaultCharacterSpacing;

        uint maxlen = 0;
        uint lines = 0;
        uint len = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\t')
                len += 2;
            else if (c == ' ')
                len++;
            else if (c == '\n')
            {
                maxlen = uint.Max(maxlen, len);
                lines++;
                len = 0;
            }
            else if (Indexes.ContainsKey(c))
                len++;
        }

        uint tw = (maxlen * gs.Width) + (maxlen * charspacing) - charspacing;
        uint th = (uint)float.Ceiling(lines * gs.Height * lineSeparation);

        textureDescription.Width = tw;
        textureDescription.Height = th;

        var target = factory.CreateTexture(ref textureDescription);

        uint cline = 0;
        uint cchar = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c is '\t')
            {
                cchar += 2;
                continue;
            }
            else if (c is ' ')
            {
                cchar++;
                continue;
            }
            else if (c is '\n')
            {
                cchar = 0;
                cline++;
                continue;
            }

            if (Indexes.TryGetValue(c, out uint index) is false) continue;

            uint x = (index * gs.Width) % atlas.Width; // We wrap around the width of the atlas, and we already know the GlyphSize is evenly divisible by the Atlas's Width
            uint y = ((index * gs.Width) / atlas.Width) * gs.Height; // We know the height is also evenly divisible, so if the index is beyond the width's, it must be on the next row

            commandList.CopyTexture(Atlas, x, y, 0, 0, 0, target, cchar * gs.Width + cchar * charspacing, (uint)(cline * gs.Height * lineSeparation), 0, 0, 0, gs.Width, gs.Height, 0, 0);
            cchar++;
        }

        return target;
    }
}
