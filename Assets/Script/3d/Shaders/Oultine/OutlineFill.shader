Shader "Custom/Outline Fill"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestFill("ZTestFill", Float) = 0
        
        _IdleColor ("Idle Color", Color) = (1,1,1,1)
        _MidColor ("Mid Color", Color) = (1,0.5,0,1)
        _ActiveColor ("Active Color", Color) = (0,1,0,1)
        _Progress ("Progress", Range(0,1)) = 0

        _OutlineWidth ("Outline Width", Range(0, 30)) = 10
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+110"
            "RenderType" = "Transparent"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "Fill"
            Cull Off
            ZTest [_ZTestFill]
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            CGPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 smoothNormal : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _IdleColor;
            float4 _MidColor;
            float4 _ActiveColor;
            float _Progress;
            float _OutlineWidth;

            v2f vert(appdata input)
            {
                v2f output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Outline extrusion always runs — visibility is gated by _Progress
                // and _OutlineWidth in the fragment stage and via ZTest/Stencil.
                float3 normal = any(input.smoothNormal) ? input.smoothNormal : input.normal;
                float3 viewPosition = UnityObjectToViewPos(input.vertex);
                float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));

                float outlineScale = _OutlineWidth / 1000.0;

                output.position = UnityViewToClipPos(
                    viewPosition + viewNormal * -viewPosition.z * outlineScale
                );

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                if(_Progress == 0)
                    discard;
                
                float3 color;

                if (_Progress < 0.5)
                {
                    float t = _Progress / 0.5;
                    color = lerp(_IdleColor.rgb, _MidColor.rgb, t);
                }
                else
                {
                    float t = (_Progress - 0.5) / 0.5;
                    color = lerp(_MidColor.rgb, _ActiveColor.rgb, t);
                }
                
                return float4(color, _Progress);
            }
            ENDCG
        }
    }
}