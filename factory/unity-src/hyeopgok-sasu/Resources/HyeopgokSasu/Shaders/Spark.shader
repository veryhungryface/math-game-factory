// Game-local transparent instancing: pooled contact blobs / additive star rays.
Shader "Mgf/HyeopgokSpark" {
 Properties {
  _MainTex("Glow atlas",2D)="white" {}
  _UseMainTex("Use glow atlas",Range(0,1))=0
  _Color("Tint",Color)=(1,1,1,1)
 }
 SubShader {
  Tags { "RenderType"="Transparent" "Queue"="Transparent" }
  Blend SrcAlpha One
  ZWrite Off Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.5
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   sampler2D _MainTex;half _UseMainTex;fixed4 _Color;
   struct Input { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct Interpolated { float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
   Interpolated vert(Input v) {
    UNITY_SETUP_INSTANCE_ID(v);
    Interpolated o; o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;o.uv=v.uv;return o;
   }
   fixed4 frag(Interpolated i):SV_Target {
    fixed4 atlas=tex2D(_MainTex,i.uv);
    return lerp(i.color,atlas*_Color,_UseMainTex);
   }
   ENDCG
  }
 }
}
