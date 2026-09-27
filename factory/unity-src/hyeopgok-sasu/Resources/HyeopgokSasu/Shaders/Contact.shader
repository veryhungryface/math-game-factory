// Game-local transparent instancing: pooled contact blobs / additive star rays.
Shader "Mgf/HyeopgokContact" {
 Properties {
  _Color("Tint",Color)=(1,1,1,1)
  _Strength("Soft core strength",Range(.5,1.5))=1.18
 }
 SubShader {
  Tags { "RenderType"="Transparent" "Queue"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.5
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   fixed4 _Color;half _Strength;
   struct Input { float4 vertex:POSITION; fixed4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct Interpolated { float4 pos:SV_POSITION; fixed4 color:COLOR; };
   Interpolated vert(Input v) {
    UNITY_SETUP_INSTANCE_ID(v);
    Interpolated o; o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;return o;
   }
   fixed4 frag(Interpolated i):SV_Target {
    fixed4 result=i.color;
    // The meshes already carry a broad centre-to-edge alpha feather. A small
    // bounded lift keeps teal contact readable without a screen-space AO pass.
    result.a=saturate(result.a*_Strength);
    return result;
   }
   ENDCG
  }
 }
}
