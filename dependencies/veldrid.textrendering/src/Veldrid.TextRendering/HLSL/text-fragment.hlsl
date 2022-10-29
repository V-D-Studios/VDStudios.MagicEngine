﻿cbuffer AtlasInfo : register(b1)
{
    float AtlasWidth;
    float3 __unused0;
}

struct PS_INPUT
{
    float4 position : SV_POSITION;
    float2 texCoords : TEXCOORD0;
    float4 color : COLOR0;
};

sampler sampler0;
Texture2D<uint> FontAtlas;

float4 PS(PS_INPUT input) : SV_TARGET
{
    int3 pixelCoords = int3(input.texCoords * AtlasWidth, 0);
    uint fontSample = FontAtlas.Load(pixelCoords);
    float floatSample = (float)fontSample / 255.0;
    float4 outputColor = input.color;
    outputColor.a *= floatSample;
    return outputColor;
}