using System;

namespace Veldrid;

/// <summary>
/// Describes a <see cref="TextureView"/>, for creation using a <see cref="ResourceFactory"/>.
/// </summary>
public struct TextureViewDescription : IEquatable<TextureViewDescription>
{
    /// <summary>
    /// The desired target <see cref="Texture"/>.
    /// </summary>
    public Texture Target;

    /// <summary>
    /// The base mip level visible in the view. Must be less than <see cref="Texture.MipLevels"/>.
    /// </summary>
    public uint? BaseMipLevel;

    /// <summary>
    /// The number of mip levels visible in the view.
    /// </summary>
    public uint? MipLevels;

    /// <summary>
    /// The base array layer visible in the view.
    /// </summary>
    public uint? BaseArrayLayer;

    /// <summary>
    /// The number of array layers visible in the view.
    /// </summary>
    public uint? ArrayLayers;

    /// <summary>
    /// An optional <see cref="PixelFormat"/> which specifies how the data within <see cref="Target"/> will be viewed.
    /// If this value is null, then the created TextureView will use the same <see cref="PixelFormat"/> as the target
    /// <see cref="Texture"/>. If not null, this format must be "compatible" with the target Texture's. For uncompressed
    /// formats, the overall size and number of components in this format must be equal to the underlying format. For
    /// compressed formats, it is only possible to use the same PixelFormat or its sRGB/non-sRGB counterpart.
    /// </summary>
    public PixelFormat? Format;

    /// <summary>
    /// Constructs a new TextureViewDescription.
    /// </summary>
    /// <param name="target">The desired target <see cref="Texture"/>. This <see cref="Texture"/> must have been created
    /// with the <see cref="TextureUsage.Sampled"/> usage flag.</param>
    public TextureViewDescription(Texture target)
    {
        Target = target;
        BaseMipLevel = null;
        MipLevels = null;
        BaseArrayLayer = null;
        ArrayLayers = null;
        Format = null;
    }

    public void GetData(out PixelFormat format, out uint arrayLayers, out uint baseArrayLayer, out uint mipLevels, out uint baseMipLevel)
    {
        if (Target is not Texture target)
            throw new InvalidOperationException("Cannot get the data from a TextureViewDescription with no Target");
        baseMipLevel = BaseMipLevel ?? 0;
        mipLevels = MipLevels ?? target.MipLevels;
        baseArrayLayer = BaseArrayLayer ?? 0;
        arrayLayers = ArrayLayers ?? target.ArrayLayers;
        format = Format ?? target.Format;
    }

    /// <summary>
    /// Constructs a new TextureViewDescription.
    /// </summary>
    /// <param name="target">The desired target <see cref="Texture"/>. This <see cref="Texture"/> must have been created
    /// with the <see cref="TextureUsage.Sampled"/> usage flag.</param>
    /// <param name="format">Specifies how the data within the target Texture will be viewed.
    /// This format must be "compatible" with the target Texture's. For uncompressed formats, the overall size and number of
    /// components in this format must be equal to the underlying format. For compressed formats, it is only possible to use
    /// the same PixelFormat or its sRGB/non-sRGB counterpart.</param>
    public TextureViewDescription(Texture target, PixelFormat format)
    {
        Target = target;
        BaseMipLevel = null;
        MipLevels = null;
        BaseArrayLayer = null;
        ArrayLayers = null;
        Format = format;
    }

    /// <summary>
    /// Constructs a new TextureViewDescription.
    /// </summary>
    /// <param name="target">The desired target <see cref="Texture"/>.</param>
    /// <param name="baseMipLevel">The base mip level visible in the view. Must be less than <see cref="Texture.MipLevels"/>.
    /// </param>
    /// <param name="mipLevels">The number of mip levels visible in the view.</param>
    /// <param name="baseArrayLayer">The base array layer visible in the view.</param>
    /// <param name="arrayLayers">The number of array layers visible in the view.</param>
    public TextureViewDescription(Texture target, uint baseMipLevel, uint mipLevels, uint baseArrayLayer, uint arrayLayers)
    {
        Target = target;
        BaseMipLevel = baseMipLevel;
        MipLevels = mipLevels;
        BaseArrayLayer = baseArrayLayer;
        ArrayLayers = arrayLayers;
        Format = null;
    }

    /// <summary>
    /// Constructs a new TextureViewDescription.
    /// </summary>
    /// <param name="target">The desired target <see cref="Texture"/>.</param>
    /// <param name="format">Specifies how the data within the target Texture will be viewed.
    /// This format must be "compatible" with the target Texture's. For uncompressed formats, the overall size and number of
    /// components in this format must be equal to the underlying format. For compressed formats, it is only possible to use
    /// the same PixelFormat or its sRGB/non-sRGB counterpart.</param>
    /// <param name="baseMipLevel">The base mip level visible in the view. Must be less than <see cref="Texture.MipLevels"/>.
    /// </param>
    /// <param name="mipLevels">The number of mip levels visible in the view.</param>
    /// <param name="baseArrayLayer">The base array layer visible in the view.</param>
    /// <param name="arrayLayers">The number of array layers visible in the view.</param>
    public TextureViewDescription(Texture target, PixelFormat format, uint baseMipLevel, uint mipLevels, uint baseArrayLayer, uint arrayLayers)
    {
        Target = target;
        BaseMipLevel = baseMipLevel;
        MipLevels = mipLevels;
        BaseArrayLayer = baseArrayLayer;
        ArrayLayers = arrayLayers;
        Format = format;
    }

    /// <summary>
    /// Element-wise equality.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>True if all elements are equal; false otherswise.</returns>
    public bool Equals(TextureViewDescription other)
    {
        return Target.Equals(other.Target)
            && BaseMipLevel == other.BaseMipLevel
            && MipLevels == other.MipLevels
            && BaseArrayLayer == other.BaseArrayLayer
            && ArrayLayers == other.ArrayLayers
            && Format == other.Format;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return HashHelper.Combine(
            Target?.GetHashCode() ?? 0,
            BaseMipLevel?.GetHashCode() ?? 0,
            MipLevels?.GetHashCode() ?? 0,
            BaseArrayLayer?.GetHashCode() ?? 0,
            ArrayLayers?.GetHashCode() ?? 0,
            Format?.GetHashCode() ?? 0);
    }
}
