//float4x4 World;
//float4x4 View;
//float4x4 Projection;
float4x4 ModelViewProjection;
texture ModelTexture;

sampler2D TexSampler = sampler_state
{
	Texture = (ModelTexture);
	MinFilter = Linear;
	MagFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

// TODO: add effect parameters here.

struct VertexShaderInput
{
    float4 Position : POSITION0;
	float4 Normal : NORMAL0;
	float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 TextureCoordinate : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    //float4 worldPosition = mul(input.Position, World);
    //float4 viewPosition = mul(worldPosition, View);
    //output.Position = mul(viewPosition, Projection);
	output.Position = mul(input.Position, ModelViewProjection);
	output.TextureCoordinate = input.TextureCoordinate;

    // TODO: add your vertex shader code here.

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    return tex2D(TexSampler, input.TextureCoordinate);
    //return float4(1, 0, 0, 1);
}

technique Technique1
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();

		CullMode = Ccw;
        
        // Disable writing to the frame buffer
        AlphaBlendEnable = false;
        //SrcBlend = Zero;
        //DestBlend = One;
        
        // Disable writing to depth buffer
        ZWriteEnable = True;
    }
}
