﻿using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.DrawLibrary.Geometry;
using VDStudios.MagicEngine.Geometry;
using Veldrid;
using Veldrid.ImageSharp;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// A draw operation that renders a Texture onto the screen
/// </summary>
/// <remarks>
/// This class overrides <see cref="ShapeRenderer{TVertex}.InterceptResources(ref ResourceLayout[], ref ResourceSet[])"/> and ensures the very first <see cref="ResourceLayout"/> and <see cref="ResourceSet"/> pair bind to this Renderer's <see cref="global::Veldrid.Sampler"/>
/// </remarks>
public class TexturedShapeRenderer<TVertex> : ShapeRenderer<TVertex> where TVertex : unmanaged
{
    #region Construction

    /// <summary>
    /// Creates a new <see cref="TexturedShapeRenderer{TVertex}"/> object
    /// </summary>
    /// <param name="texture">The Device Texture for this Renderer</param>
    /// <param name="shapes">The shapes to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="TexturedShapeRenderer{TVertex}"/></param>
    /// <param name="vertexGenerator">The <see cref="IShapeRendererVertexGenerator{TVertex}"/> object that will generate the vertices for all shapes in the buffer</param>
    public TexturedShapeRenderer(ImageSharpTexture texture, IEnumerable<ShapeDefinition> shapes, TexturedShapeRenderDescription description, IShapeRendererVertexGenerator<TVertex> vertexGenerator) 
        : base(shapes, description.ShapeRenderer, vertexGenerator)
        // allow for many shapes, one texture. Spread over all shapes, draw on each shape, etc.
    {
        ArgumentNullException.ThrowIfNull(texture);
        TextureFactory = texture.CreateDeviceTexture;
    }

    /// <summary>
    /// Creates a new <see cref="TexturedShapeRenderer{TVertex}"/> object
    /// </summary>
    /// <param name="textureFactory">Represents the method that will create the Device Texture for this Renderer</param>
    /// <param name="shapes">The shapes to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="TexturedShapeRenderer{TVertex}"/></param>
    /// <param name="vertexGenerator">The <see cref="IShapeRendererVertexGenerator{TVertex}"/> object that will generate the vertices for all shapes in the buffer</param>
    public TexturedShapeRenderer(TextureFactory textureFactory, IEnumerable<ShapeDefinition> shapes, TexturedShapeRenderDescription description, IShapeRendererVertexGenerator<TVertex> vertexGenerator)
        : base(shapes, description.ShapeRenderer, vertexGenerator)
    {
        ArgumentNullException.ThrowIfNull(textureFactory);
        TextureFactory = textureFactory;
    }

    #endregion

    #region Resources

    private readonly TexturedShapeRenderDescription Description;
    private Sampler Sampler;
    private TextureFactory TextureFactory;

    /// <summary>
    /// The texture that this <see cref="TexturedShapeRenderer{TVertex}"/> is in charge of rendering. Will become available after <see cref="CreateResources(GraphicsDevice, ResourceFactory)"/> is called
    /// </summary>
    protected Texture Texture;

    #endregion

    #region DrawOperation

    /// <inheritdoc/>
    protected override ValueTask CreateWindowSizedResources(GraphicsDevice device, ResourceFactory factory, DeviceBuffer screenSizeBuffer)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Intercepts the <see cref="ShapeRenderer"/>'s resources, and injects the Sampler's layout and set to the first element of the respective arrays
    /// </summary>
    /// <param name="layouts"></param>
    /// <param name="sets"></param>
    /// <param name="factory"></param>
    protected override void InterceptResources(ref ResourceLayout[] layouts, ref ResourceSet[] sets, ResourceFactory factory)
    {
        var nl = new ResourceLayout[layouts.Length + 1];
        var ns = new ResourceSet[sets.Length + 1];
        layouts.CopyTo(nl, 1);
        sets.CopyTo(ns, 1);

        var layoutDesc = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription(
                "Sampler",
                ResourceKind.Sampler,
                ShaderStages.Fragment
        ));

        var layout = factory.CreateResourceLayout(ref layoutDesc);

        var setDesc = new ResourceSetDescription(layout, Sampler);

        var set = factory.CreateResourceSet(ref setDesc);

        nl[0] = layout;
        ns[0] = set;
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory)
    {
        var texture = TextureFactory.Invoke(device, factory);
        if (texture is null)
        {
            var exc = new InvalidOperationException($"The TextureFactory for TextureRenderer returned null, rather than a Device Texture");
            Log.Fatal(exc, "A TextureRenderer's TextureFactory failed to create a valid Device Texture");
            throw exc;
        }
        Sampler = factory.CreateSampler(Description.Sampler);
        Texture = texture;

        return base.CreateResources(device, factory);
    }

    /// <inheritdoc/>
    protected override ValueTask Draw(TimeSpan delta, CommandList commandList, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer screenSizeBuffer)
    {
        throw new NotImplementedException();
    }

    #endregion
}