Shader "Custom/FlickerUIShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {} // 1. Added this
        _IdleColor ("Idle Color", Color) = (1,1,1,1)
        _FlickerColor ("Flicker Color", Color) = (0,0,0,1)
        _FlickerState ("Flicker State (0/1)", Float) = 0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // 2. Added sampler for the texture
            sampler2D _MainTex;
            float4 _IdleColor;
            float4 _FlickerColor;
            float _FlickerState;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0; // 3. Need to receive UVs
                float4 color : COLOR;         // 4. Need to receive UI color
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 texcoord : TEXCOORD0; // 5. Need to pass UVs to frag
                fixed4 color : COLOR;        // 6. Need to pass color to frag
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord; // Pass UVs through
                o.color = v.color;       // Pass UI Color through
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float f = saturate(_FlickerState);
                float3 finalRGB = lerp(_IdleColor.rgb, _FlickerColor.rgb, f > 0.5 ? 1.0 : 0.0);

                // 7. Sample the sprite texture
                fixed4 spriteTex = tex2D(_MainTex, i.texcoord);

                // 8. Use the sprite's alpha and the UI's vertex alpha
                return fixed4(finalRGB, spriteTex.a * i.color.a);
            }
            ENDCG
        }
    }
}