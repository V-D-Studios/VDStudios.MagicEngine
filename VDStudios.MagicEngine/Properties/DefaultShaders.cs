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

layout(location = 0) out vec4 outColor;
layout(location = 0) in vec4 fragTexCoord;

void main() {
    outColor = texture(sampler2D(Tex, TSamp), vec2(fragTexCoord.x, fragTexCoord.y));
}
",
                bindings:
@"
#binding uniform sampler TSamp;
#binding uniform texture2D Tex;
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
layout(set=1,binding=0) uniform  WindowTransform{
    layout(offset = 0) mat4 WinTrans;
};layout(set=2,binding=0) uniform  Transform{
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
layout(set=0,binding=0) uniform sampler  TSamp;layout(set=0,binding=1) uniform texture2D  Tex;

layout(location = 0) out vec4 outColor;
layout(location = 0) in vec4 fragTexCoord;

void main() {
    outColor = texture(sampler2D(Tex, TSamp), vec2(fragTexCoord.x, fragTexCoord.y));
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
