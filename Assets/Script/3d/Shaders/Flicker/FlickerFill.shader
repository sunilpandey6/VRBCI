Shader "Custom/FlickerFill"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _FlickerOnColor ("Flicker Color", Color) = (0,0,0,1)
        _FlickerState ("Flicker State (0/1)", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite On

        Pass
        {
            Name "FlickerFillPass"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _BaseColor;
            float4 _FlickerOnColor;
            float _FlickerState;

            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position = UnityObjectToClipPos(input.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float f = saturate(_FlickerState);
                // Hard square wave — identical to UI shader
                float3 color = lerp(_BaseColor.rgb, _FlickerOnColor.rgb, f > 0.5 ? 1.0 : 0.0);
                return float4(color, _BaseColor.a);
            }
            ENDCG
        }
    }
}