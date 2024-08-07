﻿using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Properties;

/// <summary>
/// This class presents access to MagicEngine's default shaders
/// </summary>
public static class DefaultShaders
{
    #region TexturedShapeRenderer

    /// <summary>
    /// The default vertex shader for <see cref="TexturedShape2DRenderer"/>
    /// </summary>
    public const string DefaultTexturedShape2DRendererVertexShader = """
        #version 450
        
        layout(location = 0) in vec2 Position;
        layout(location = 1) in vec2 TexCoords;

        layout(location = 1) out vec2 outtex;

        layout(set=0,binding=0) uniform FrameReport {
            layout(offset = 0) mat4 projection;
            layout(offset = 64) float delta;
        };

        layout(set=1,binding=0) uniform Parameters {
            layout(offset = 0) mat4 view;
        };

        layout(set=2,binding=0) uniform DrawOp {
            layout(offset = 0) mat4 transform;
        };

        void main() {
            gl_Position = projection * view * transform * vec4(Position, 0.0, 1.0);
            outtex = TexCoords;
        }
        """;

    /// <summary>
    /// The default fragment shader for <see cref="TexturedShape2DRenderer"/>
    /// </summary>
    public
#if DEBUG
    readonly static
#else
    const
#endif
    string DefaultTexturedShapeRendererFragmentShader = @"#version 450

const int grayscaleFx = 1 << 0;
const int tintFx = 1 << 1;
const int overlayFx = 1 << 2;
const int opacityOverrideFx = 1 << 3;
const int opacityMultiplyFx = 1 << 4;

layout(location = 0) out vec4 outColor;

layout(location = 1) in vec2 TextureCoordinate;

layout(set=2,binding=0) uniform Transform {
    layout(offset = 64) vec4 tint;
    layout(offset = 80) vec4 overlay;
    layout(offset = 96) uint colorfx;
    layout(offset = 100) float opacity;
} trans;

layout(set=3,binding=0) uniform texture2D Tex;

layout(set=3,binding=1) uniform sampler TSamp;

vec4 toGrayscale(vec4 color)
{
    float average = (color.r + color.g + color.b) / 3.0;
    return vec4(average, average, average, color.a);
}

void main() {
    vec4 c = texture(sampler2D(Tex, TSamp), vec2(TextureCoordinate.x, TextureCoordinate.y));
    if ((trans.colorfx & grayscaleFx) != 0) { c = toGrayscale(c); }
    if ((trans.colorfx & tintFx) != 0) { c = vec4(c.r * trans.tint.r, c.g * trans.tint.g, c.b * trans.tint.b, c.a); }
    if ((trans.colorfx & overlayFx) != 0) { c *= trans.overlay; }
    if ((trans.colorfx & opacityOverrideFx) != 0) { c.a = trans.opacity; }
    else if ((trans.colorfx & opacityMultiplyFx) != 0) { c.a *= trans.opacity; }
    outColor = c;
}";

#endregion

    #region ShapeRenderer

    /// <summary>
    /// The default vertex shader for <see cref="Shape2DRenderer"/>
    /// </summary>
    public const string DefaultShape2DRendererVertexShader = """
        #version 450
        
        layout(location = 0) in vec2 Position;
        layout(location = 1) in vec4 Color;
        layout(location = 0) out vec4 fsin_Color;

        layout(set=0,binding=0) uniform FrameReport {
            layout(offset = 0) mat4 projection;
            layout(offset = 64) float delta;
        };

        layout(set=1,binding=0) uniform Parameters {
            layout(offset = 0) mat4 view;
        };

        layout(set=2,binding=0) uniform DrawOp {
            layout(offset = 0) mat4 transform;
        };
        
        void main() {
            fsin_Color = Color;
            gl_Position = projection * view * transform * vec4(Position, 0.0, 1.0);
        }
        """;

    internal static readonly byte[] DefaultShape2DRendererVertexShaderBytes
        = Encoding.UTF8.GetBytes(DefaultShape2DRendererVertexShader);

    /// <summary>
    /// The default fragment shader for <see cref="Shape2DRenderer"/>
    /// </summary>
    public const string DefaultShape2DRendererFragmentShader = """
        #version 450
        
        const int grayscaleFx = 1 << 0;
        const int tintFx = 1 << 1;
        const int overlayFx = 1 << 2;
        const int opacityOverrideFx = 1 << 3;
        const int opacityMultiplyFx = 1 << 4;
        
        layout(location = 0) out vec4 fsout_Color;
        layout(location = 0) in vec4 fsin_Color;
        
        layout(set=0,binding=0) uniform FrameReport {
            layout(offset = 64) float delta;
        };
        
        layout(set=2,binding=0) uniform Transform {
            layout(offset = 0) mat4 opTrans;
            layout(offset = 64) vec4 tint;
            layout(offset = 80) vec4 overlay;
            layout(offset = 96) uint colorfx;
            layout(offset = 100) float opacity;
        } trans;
        
        vec4 toGrayscale(vec4 color)
        {
            float average = (color.r + color.g + color.b) / 3.0;
            return vec4(average, average, average, color.a);
        }
        
        void main() {
            vec4 c = fsin_Color;
            if ((trans.colorfx & grayscaleFx) != 0) { c = toGrayscale(c); }
            if ((trans.colorfx & tintFx) != 0) { c = vec4(c.r * trans.tint.r, c.g * trans.tint.g, c.b * trans.tint.b, c.a); }
            if ((trans.colorfx & overlayFx) != 0) { c *= trans.overlay; }
            if ((trans.colorfx & opacityOverrideFx) != 0) { c.a = trans.opacity; }
            else if ((trans.colorfx & opacityMultiplyFx) != 0) { c.a *= trans.opacity; }
            fsout_Color = c;
        }
        """;

    internal static readonly byte[] DefaultShape2DRendererFragmentShaderBytes
        = Encoding.UTF8.GetBytes(DefaultShape2DRendererFragmentShader);

    #endregion
}
