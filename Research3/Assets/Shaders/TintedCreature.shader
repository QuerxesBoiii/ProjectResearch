Shader "Custom/TintedCreature"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _MaskColor ("Mask Color (Excluded from Tint)", Color) = (0,0,0,1) // Black by default
        _MaskThreshold ("Mask Threshold", Range(0,1)) = 0.1 // Tolerance for matching mask color
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _TintColor;
            float4 _MaskColor;
            float _MaskThreshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Calculate distance from mask color
                float dist = distance(col.rgb, _MaskColor.rgb);
                
                // If close to mask color, don’t tint; otherwise, apply tint
                if (dist < _MaskThreshold)
                    return col; // Keep original color (e.g., black eyes)
                else
                    return col * _TintColor; // Apply tint to body
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}