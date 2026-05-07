Shader "Custom/Outline Mask" {
  Properties {
    [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMask("ZTestMask", Float) = 0
  }

  SubShader {
    Tags {
      "Queue" = "Transparent+100"
      "RenderType" = "Transparent"
    }

    Pass {
      Name "Mask"
      Cull Off
      ZTest [_ZTestMask]
      ZWrite Off
      ColorMask 0

      Stencil {
        Ref 1
        Pass Replace
      }
    }
  }
}
