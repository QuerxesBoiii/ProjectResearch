//UNITY_SHADER_NO_UPGRADE
#ifndef STOCHASTICUV_INCLUDED
#define STOCHASTICUV_INCLUDED
float2 hash2D2D (float2 s)
{
    //magic numbers
    return frac(sin(fmod(float2(dot(s, float2(127.1,311.7)), dot(s, float2(269.5,183.3))), 3.14159))*43758.5453);
}
void StochasticUV_float(in float2 UV, out float3 BW, out float2 UV_Tex1, out float2 UV_Tex2, out float2 UV_Tex3)
{
    //uv transformed into triangular grid space with UV scaled by approximation of 2*sqrt(3)
    float2 skewUV = mul(float2x2 (1.0 , 0.0 , -0.57735027 , 1.15470054), UV * 3.464);

    //vertex IDs and barycentric coords
    float2 vxID = float2 (floor(skewUV));
    float3 barry = float3 (frac(skewUV), 0);
    barry.z = 1.0-barry.x-barry.y;

    float4x3 BW_vx = ((barry.z>0) ?  
        float4x3(float3(vxID, 0), float3(vxID + float2(0, 1), 0), float3(vxID + float2(1, 0), 0), barry.zyx) : 
        float4x3(float3(vxID + float2 (1, 1), 0), float3(vxID + float2 (1, 0), 0), float3(vxID + float2 (0, 1), 0), float3(-barry.z, 1.0-barry.y, 1.0-barry.x)));
    //calculate derivatives to avoid triangular grid artifacts
    UV_Tex1 =  UV + hash2D2D(BW_vx[0].xy);
    UV_Tex2 =  UV + hash2D2D(BW_vx[1].xy);
    UV_Tex3 =  UV + hash2D2D(BW_vx[2].xy);
    BW = BW_vx[3];
}
#endif 