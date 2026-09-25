// MGF 킷 가산 재질 (빛·불꽃 파티클). ZWrite Off.
Shader "Mgf/Additive" {
 Properties { _Color("Color", Color) = (1,1,1,1) _MainTex("Texture", 2D) = "white" {} }
 SubShader {
  Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
  Blend SrcAlpha One ZWrite Off Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
   struct a2v { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   v2f vert(a2v v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.color=v.color*_Color; return o; }
   fixed4 frag(v2f i):SV_Target{ return tex2D(_MainTex,i.uv)*i.color; }
   ENDCG
  }
 }
}
