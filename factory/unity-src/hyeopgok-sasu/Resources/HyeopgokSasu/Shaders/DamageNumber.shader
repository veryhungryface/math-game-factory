Shader "Mgf/HyeopgokDamageNumber" {
 Properties { _MainTex("Damage bitmap",2D)="white"{} }
 SubShader {
  Tags { "RenderType"="Transparent" "Queue"="Transparent+20" }
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.5
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Interpolated {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
   Interpolated vert(Input v){UNITY_SETUP_INSTANCE_ID(v);Interpolated o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
   fixed4 frag(Interpolated i):SV_Target{return tex2D(_MainTex,i.uv);}
   ENDCG
  }
 }
}
