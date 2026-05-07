Shader "Custom/DwellUIShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {} // Required for UI.Image
        _IdleColor ("Idle Color", Color) = (1,1,1,1)
        _MidColor ("Mid Color", Color) = (1,0.5,0,1)
        _ActiveColor ("Active Color", Color) = (0,1,0,1)
        _Progress ("Progress", Range(0,1)) = 0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _IdleColor;
            fixed4 _MidColor;
            fixed4 _ActiveColor;
            float _Progress;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. Calculate your lerped color
                float3 finalRGB;
                if (_Progress < 0.5)
                {
                    float t = _Progress / 0.5;
                    finalRGB = lerp(_IdleColor.rgb, _MidColor.rgb, t);
                }
                else
                {
                    float t = (_Progress - 0.5) / 0.5;
                    finalRGB = lerp(_MidColor.rgb, _ActiveColor.rgb, t);
                }

                // 2. Sample the texture (your sprite)
                fixed4 spriteTex = tex2D(_MainTex, i.texcoord);

                // 3. COMBINE: Use the color you calculated, but take the ALPHA from the sprite
                // Also multiply by i.color to respect the Image component's color/alpha settings
                fixed4 outColor = fixed4(finalRGB, spriteTex.a * i.color.a);

                return outColor;
            }
            ENDCG
        }
    }
}