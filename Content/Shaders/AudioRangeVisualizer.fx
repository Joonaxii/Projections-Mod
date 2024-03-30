float uAlpha;
float uInnerEdge;
float uSizePix;

float InverseLerp(float a, float b, float v)
{
    if (v <= a)
    {
        return 0.0;
    }
    else if (v >= b)
    {
        return 1.0;
    }
    return (v - a) / (b - a);
}

float4 AudioRange(float4 position : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{   
    coords.x -= 0.5;
    coords.x *= 2.0;

    coords.y -= 0.5;
    coords.y *= 2.0;
    
    float tileS = max(uSizePix / 16, 16.0);
    coords.x = round(coords.x * tileS) / tileS;
    coords.y = round(coords.y * tileS) / tileS;

    float dist = sqrt(coords.x * coords.x + coords.y * coords.y);

    float t = InverseLerp(uInnerEdge, 1.0, dist);
    float tA = (1.0 - (t * t * t * t)) * uAlpha;
    float4 color = lerp(float4(0.0, 1.0, 0.125, tA), float4(1.0, 0.125, 0.0, tA), round(t * tileS) / tileS);
    color.rgb *= color.a;
    return color;
}

technique Technique1
{
    pass AudioRange
    {
        PixelShader = compile ps_2_0 AudioRange();
    }
}