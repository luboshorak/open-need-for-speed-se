// Screen-space correction of the original 640x480 game image.
// Shaders operate on pixels only; original palette/QFS assets stay unchanged.
#if OPENGL
    #define PS_SHADERMODEL ps_3_0
#else
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

texture Texture;
sampler2D TextureSampler : register(s0) = sampler_state
{
    Texture = <Texture>;
    // Sampler filtering is selected at runtime by SpriteBatch
    // (Sharp/Nearest, Smooth/Linear, Integer), not forced here.
    AddressU = Clamp;
    AddressV = Clamp;
};

float Gamma = 1.0;
float Brightness = 0.0;
float Contrast = 1.0;

float4 ColorCorrect(float2 texCoord : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float4 pixel = tex2D(TextureSampler, texCoord) * vertexColor;
    float3 corrected = saturate((pixel.rgb - 0.5) * Contrast + 0.5 + Brightness);
    corrected = pow(corrected, 1.0 / max(Gamma, 0.05));
    return float4(saturate(corrected), pixel.a);
}

technique ColorCorrection
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL ColorCorrect();
    }
}
