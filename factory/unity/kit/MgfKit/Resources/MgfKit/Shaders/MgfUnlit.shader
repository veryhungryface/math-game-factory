// MGF 킷 무조명 불투명 재질 (색 × 텍스처).
Shader "Mgf/Unlit" {
 Properties { _Color("Color", Color) = (1,1,1,1) _MainTex("Texture", 2D) = "white" {} }
 SubShader {
  Tags { "RenderType"="Opaque" "Queue"="Geometry" }
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
   struct a2v { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
   v2f vert(a2v v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); return o; }
   fixed4 frag(v2f i):SV_Target{ return tex2D(_MainTex,i.uv)*_Color; }
   ENDCG
  }
 }
}
