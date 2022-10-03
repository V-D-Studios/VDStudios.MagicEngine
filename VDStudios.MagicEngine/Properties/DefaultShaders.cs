using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.DrawLibrary;
using VDStudios.MagicEngine.DrawLibrary.Geometry;

namespace VDStudios.MagicEngine.Properties;

/// <summary>
/// This class presents access to MagicEngine's default shaders
/// </summary>
public static class DefaultShaders
{
    #region TexturedShapeRenderer

    #region Builders 

    private static readonly Lazy<ShaderBuilder> lazy_dtsrfs
        = new(() =>
        {
            var builder = new ShaderBuilder(
                shaderbase:
@"#version 450

const int grayscaleFx = 1 << 0;
const int tintFx = 1 << 1;
const int overlayFx = 1 << 2;
const int opacityOverrideFx = 1 << 3;
const int opacityMultiplyFx = 1 << 4;

layout(location = 0) out vec4 outColor;
layout(location = 0) in vec4 fragTexCoord;

vec4 toGrayscale(vec4 color)
{
    float average = (color.r + color.g + color.b) / 3.0;
    return vec4(average, average, average, color.a);
}

void main() {
    vec4 c = texture(sampler2D(Tex, TSamp), vec2(fragTexCoord.x, fragTexCoord.y));
    if ((trans.colorfx & grayscaleFx) != 0) { c = toGrayscale(c); }
    if ((trans.colorfx & tintFx) != 0) { c *= trans.tint; }
    if ((trans.colorfx & overlayFx) != 0) { c *= trans.overlay; }
    if ((trans.colorfx & opacityOverrideFx) != 0) { c.a = trans.opacity; }
    else if ((trans.colorfx & opacityMultiplyFx) != 0) { c.a *= trans.opacity; }
    outColor = c;
}",
                bindings:
@"
#binding uniform sampler TSamp;
#binding uniform texture2D Tex;
#binding uniform Transform {
    layout(offset = 0) mat4 opTrans;
    layout(offset = 64) vec4 tint;
    layout(offset = 80) vec4 overlay;
    layout(offset = 96) float opacity;
    layout(offset = 100) uint colorfx;
} trans;
"
            );
            return builder;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// The default fragment shader builder for <see cref="TexturedShapeRenderer{TVertex}"/>
    /// </summary>
    public static ShaderBuilder DefaultTexturedShapeRendererFragmentShaderBuilder => lazy_dtsrfs.Value;

    private static readonly Lazy<ShaderBuilder> lazy_dtsrvs
        = new(() =>
        {
            var builder = new ShaderBuilder(
                shaderbase:
@"#version 450

layout(location = 0) in vec2 inTexCoord;
layout(location = 1) in vec2 Position;
layout(location = 0) out vec4 fragTexCoord;

void main() {
    fragTexCoord = vec4(inTexCoord, 0.0, 1.0);
    gl_Position = projection * view * opTrans * vec4(Position, 0.0, 1.0);
}

",
                bindings:
@"
#binding uniform Transform {
    layout(offset = 0) mat4 opTrans;
};
#binding uniform Parameters {
    layout(offset = 0) mat4 view;
    layout(offset = 64) mat4 projection;
};
"
            );
            return builder;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// The default vertex shader builder for <see cref="TexturedShapeRenderer{TVertex}"/>
    /// </summary>
    public static ShaderBuilder DefaultTexturedShapeRendererVertexShaderBuilder => lazy_dtsrvs.Value;

    #endregion

    #region Pre-Built

    /// <summary>
    /// The default vertex shader for <see cref="TexturedShapeRenderer{TVertex}"/>
    /// </summary>
    public const string DefaultTexturedShapeRendererVertexShader = @"#version 450
layout(set=1,binding=0) uniform Transform{
    layout(offset = 0) mat4 opTrans;
};layout (set=2,binding=0)uniform Parameters {
    layout(offset = 0) mat4 view;
    layout(offset = 64) mat4 projection;
};

layout(location = 0) in vec2 inTexCoord;
layout(location = 1) in vec2 Position;
layout(location = 0) out vec4 fragTexCoord;

void main() {
    fragTexCoord = vec4(inTexCoord, 0.0, 1.0);
    gl_Position = projection * view * opTrans * vec4(Position, 0.0, 1.0);
}
";

    /// <summary>
    /// The default fragment shader for <see cref="TexturedShapeRenderer{TVertex}"/>
    /// </summary>
    public const string DefaultTexturedShapeRendererFragmentShader = @"#version 450

const int grayscaleFx = 1 << 0;
const int tintFx = 1 << 1;
const int overlayFx = 1 << 2;
const int opacityOverrideFx = 1 << 3;
const int opacityMultiplyFx = 1 << 4;

layout(set=0,binding=0) uniform sampler TSamp;
layout(set=0,binding=1) uniform texture2D Tex;
layout(set=2,binding=0) uniform Transform {
    layout(offset = 0) mat4 opTrans;
    layout(offset = 64) vec4 tint;
    layout(offset = 80) vec4 overlay;
    layout(offset = 96) uint colorfx;
    layout(offset = 100) float opacity;
} trans;

layout(location = 0) out vec4 outColor;
layout(location = 0) in vec4 fragTexCoord;

vec4 toGrayscale(vec4 color)
{
    float average = (color.r + color.g + color.b) / 3.0;
    return vec4(average, average, average, color.a);
}

void main() {
    vec4 c = texture(sampler2D(Tex, TSamp), vec2(fragTexCoord.x, fragTexCoord.y));
    if ((trans.colorfx & grayscaleFx) != 0) { c = toGrayscale(c); }
    if ((trans.colorfx & tintFx) != 0) { c = vec4(c.r * trans.tint.r, c.g * trans.tint.g, c.b * trans.tint.b, c.a); }
    if ((trans.colorfx & overlayFx) != 0) { c *= trans.overlay; }
    if ((trans.colorfx & opacityOverrideFx) != 0) { c.a = trans.opacity; }
    else if ((trans.colorfx & opacityMultiplyFx) != 0) { c.a *= trans.opacity; }
    outColor = c;
}";

    #endregion

    #endregion

    #region ShapeRenderer

    /// <summary>
    /// The default vertex shader for <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public const string DefaultShapeRendererVertexShader = @"#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(location = 0) out vec4 fsin_Color;
layout(set=0,binding=0) uniform Transform {
    layout(offset = 0) mat4 opTrans;
};
layout(set=1,binding=0) uniform Parameters {
    layout(offset = 0) mat4 view;
    layout(offset = 64) mat4 projection;
};

void main() {
    fsin_Color = Color;
    gl_Position = projection * view * opTrans * vec4(Position, 0.0, 1.0);
}";

    /// <summary>
    /// The default fragment shader for <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public const string DefaultShapeRendererFragmentShader = @"#version 450

const int grayscaleFx = 1 << 0;
const int tintFx = 1 << 1;
const int overlayFx = 1 << 2;
const int opacityOverrideFx = 1 << 3;
const int opacityMultiplyFx = 1 << 4;

layout(location = 0) out vec4 fsout_Color;
layout(location = 0) in vec4 fsin_Color;

layout(set=1,binding=0) uniform Transform {
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
}";

    #endregion
}
