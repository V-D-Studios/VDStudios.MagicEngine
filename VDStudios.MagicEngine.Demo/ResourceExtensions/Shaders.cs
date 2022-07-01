using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.SPIRV;

namespace VDStudios.MagicEngine.Demo.ResourceExtensions;

public static class Shaders
{
    public static Shader CreateTextureFragmentShader(this ResourceFactory factory, ShaderStages? stage = null, string? entryPoint = null, bool debug = false)
    {
        var (dat, stg, ep) = texture_fragment_glsl.Value;
        return factory.CreateFromSpirv(new(stage ?? stg, dat, entryPoint ?? ep, debug));
    }

    public static Shader CreateTextureVertexShader(this ResourceFactory factory, ShaderStages? stage = null, string? entryPoint = null, bool debug = false)
    {
        var (dat, stg, ep) = texture_vertex_glsl.Value;
        return factory.CreateFromSpirv(new(stage ?? stg, dat, entryPoint ?? ep, debug));
    }

    public static Shader CreateTextureComputeShader(this ResourceFactory factory, ShaderStages? stage = null, string? entryPoint = null, bool debug = false)
    {
        var (dat, stg, ep) = texture_compute_glsl.Value;
        return factory.CreateFromSpirv(new(stage ?? stg, dat, entryPoint ?? ep, debug));
    }

    //

    private static readonly Lazy<(byte[], ShaderStages stage, string entry)> texture_fragment_glsl = new(() => (Encoding.UTF8.GetBytes(__texture_fragment_glsl), ShaderStages.Fragment, "main"));
    private static readonly Lazy<(byte[], ShaderStages stage, string entry)> texture_vertex_glsl = new(() => (Encoding.UTF8.GetBytes(__texture_vertex_glsl), ShaderStages.Vertex, "main"));
    private static readonly Lazy<(byte[], ShaderStages stage, string entry)> texture_compute_glsl = new(() => (Encoding.UTF8.GetBytes(__texture_compute_glsl), ShaderStages.Compute, "main"));

    private const string __texture_fragment_glsl = @"#version 450

layout(set = 0, binding = 0) uniform texture2D Tex;
layout(set = 0, binding = 1) uniform texture2D Tex11;
layout(set = 0, binding = 2) uniform texture2D Tex22;
layout(set = 0, binding = 3) uniform sampler SS;

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 0) out vec4 OutColor;

void main()
{
    OutColor = texture(sampler2D(Tex, SS), fsin_TexCoords) + texture(sampler2D(Tex11, SS), fsin_TexCoords) * .01 + texture(sampler2D(Tex22, SS), fsin_TexCoords) * .01;
}";
    private const string __texture_vertex_glsl = @"#version 450

layout (location = 0) in vec2 Position;
layout (location = 1) in vec2 TexCoords;
layout (location = 0) out vec2 fsin_TexCoords;

void main()
{
    fsin_TexCoords = TexCoords;
    gl_Position = vec4(Position, 0, 1);
}";
    private const string __texture_compute_glsl = @"#version 450

layout(set = 0, binding = 1) uniform ScreenSizeBuffer
{
    float ScreenWidth;
    float ScreenHeight;
    vec2 Padding_;
};

layout(set = 0, binding = 2) uniform ShiftBuffer
{
    float RShift;
    float GShift;
    float BShift;
    float Padding1_;
};

layout(set = 0, binding = 0, rgba32f) uniform image2D Tex;

layout(local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

void main()
{
    float x = (gl_GlobalInvocationID.x + RShift);
    float y = (gl_GlobalInvocationID.y + GShift);

    imageStore(Tex, ivec2(gl_GlobalInvocationID.xy), vec4(x / ScreenWidth, y / ScreenHeight, BShift, 1));
}";
}