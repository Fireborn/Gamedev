float4x4 World;
float4x4 View;
float4x4 Projection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
};

VertexShaderOutput ShadowVolumeVS( in VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    return output;
}

float4 ShadowVolumePS() : COLOR0
{
    return float4(0, 0, 0, 0.1f);
}

// Depth pass shadow volumes
technique ShadowVolume
{
    pass P0
    {
        VertexShader = compile vs_2_0 ShadowVolumeVS();
        PixelShader  = compile ps_2_0 ShadowVolumePS();
        CullMode = Ccw;
        
        // Disable writing to the frame buffer
        AlphaBlendEnable = true;
        SrcBlend = Zero;
        DestBlend = One;
        
        // Disable writing to depth buffer
        ZWriteEnable = false;
        ZFunc = Less;
        
        // Setup stencil states
        StencilEnable = true;
        StencilRef = 1;
        StencilMask = 0xFFFFFFFF;
        StencilWriteMask = 0xFFFFFFFF;
        StencilFunc = Always;
        StencilZFail = Decr;
        StencilPass = Keep;
    }
    pass P1
    {
        VertexShader = compile vs_2_0 ShadowVolumeVS();
        PixelShader  = compile ps_2_0 ShadowVolumePS();
        CullMode = Cw;
        StencilZFail = Incr;
    }
	pass P2
	{
		//VertexShader = complie vs_2_0 ShadowVS();
		//PixelShader= compile ps_2_0 ShadowPS();

		//Stencil
	}
}
