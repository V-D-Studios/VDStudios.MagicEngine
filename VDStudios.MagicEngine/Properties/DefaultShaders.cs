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
    private static readonly Lazy<ShaderBuilder> lazy_dtsrfs 
        = new(() =>
        {
            var builder = new ShaderBuilder(
                shaderbase:
@"#version 450

const int grayscaleFx = 1 << 0;
const int tintFx = 1 << 1;
const int overlayFx = 1 << 2;

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
    layout(offset = 96) uint colorfx;
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
    gl_Position = WinTrans * opTrans * vec4(Position, 0.0, 1.0);
}

",
                bindings:
@"
#binding uniform WindowTransform {
    layout(offset = 0) mat4 WinTrans;
};
#binding uniform Transform {
    layout(offset = 0) mat4 opTrans;
};
"
            );
            return builder;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// The default vertex shader builder for <see cref="TexturedShapeRenderer{TVertex}"/>
    /// </summary>
    public static ShaderBuilder DefaultTexturedShapeRendererVertexShaderBuilder => lazy_dtsrvs.Value;

    /// <summary>
    /// The default vertex shader for <see cref="TexturedShapeRenderer{TVertex}"/>
    /// </summary>
    public const string DefaultTexturedShapeRendererVertexShader = @"#version 450
layout(set=1,binding=0) uniform WindowTransform{
    layout(offset = 0) mat4 WinTrans;
};layout(set=2,binding=0) uniform Transform{
    layout(offset = 0) mat4 opTrans;
};

layout(location = 0) in vec2 inTexCoord;
layout(location = 1) in vec2 Position;
layout(location = 0) out vec4 fragTexCoord;

void main() {
    fragTexCoord = vec4(inTexCoord, 0.0, 1.0);
    gl_Position = WinTrans * opTrans * vec4(Position, 0.0, 1.0);
}
";

    /// <summary>
    /// The default fragment shader for <see cref="TexturedShapeRenderer{TVertex}"/>
    /// </summary>
    public const string DefaultTexturedShapeRendererFragmentShader = @"#version 450

const int grayscaleFx = 1 << 0;
const int tintFx = 1 << 1;
const int overlayFx = 1 << 2;

layout(set=0,binding=0) uniform sampler TSamp;
layout(set=0,binding=1) uniform texture2D Tex;
layout(set=2,binding=0) uniform Transform {
    layout(offset = 0) mat4 opTrans;
    layout(offset = 64) vec4 tint;
    layout(offset = 80) vec4 overlay;
    layout(offset = 96) uint colorfx;
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
    if ((trans.colorfx & tintFx) != 0) { c *= trans.tint; }
    if ((trans.colorfx & overlayFx) != 0) { c *= trans.overlay; }
    outColor = c;
}";

    /// <summary>
    /// The default vertex shader for <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public const string DefaultShapeRendererVertexShader = @"#version 450

layout(location = 0) in vec2 Position;

void main() {
    gl_Position = vec4(Position, 0.0, 1.0);
}";

    /// <summary>
    /// The default fragment shader for <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public const string DefaultShapeRendererFragmentShader = @"#version 450

layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4(1.0, 50.0, 1.0, 0.5);
}";
}
