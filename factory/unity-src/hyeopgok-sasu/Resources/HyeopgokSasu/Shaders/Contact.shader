// Game-local transparent instancing: pooled contact blobs / additive star rays.
Shader "Mgf/HyeopgokContact" {
 Properties { _Color("Tint",Color)=(1,1,1,1) }
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
   fixed4 _Color;
   struct Input { float4 vertex:POSITION; fixed4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct Interpolated { float4 pos:SV_POSITION; fixed4 color:COLOR; };
   Interpolated vert(Input v) {
    UNITY_SETUP_INSTANCE_ID(v);
    Interpolated o; o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;return o;
   }
   fixed4 frag(Interpolated i):SV_Target {return i.color;}
   ENDCG
  }
 }
}
