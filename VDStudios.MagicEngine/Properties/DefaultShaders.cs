using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.DrawLibrary;

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
    /// The default fragment shader for <see cref="TexturedShapeRenderer{TVertex}"/>
    /// </summary>
    public static ShaderBuilder DefaultTexturedShapeRendererFragmentShader => lazy_dtsrfs.Value;

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
    gl_Position = WindowScale * vec4(Position, 0.0, 1.0);
}

",
                bindings:
@"
#binding uniform WindowAspectTransform {
    layout(offset = 0) mat4 WindowScale;
};
"
            );
            return builder;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// The default vertex shader for <see cref="TexturedShapeRenderer{TVertex}"/>
    /// </summary>
    public static ShaderBuilder DefaultTexturedShapeRendererVertexShader => lazy_dtsrvs.Value;
}
