// MGF 킷 기본 PBR 재질 (Built-in RP, Standard 조명 + 림 라이트). Resources 에 있어 빌드에서 제거되지 않는다.
// Shader.Find("Standard") 는 WebGL 빌드에서 스트리핑돼 분홍/빈 화면이 된다 — 반드시 MgfLook.Lit() 을 써라.
Shader "Mgf/Lit" {
 Properties {
  _Color("Color", Color) = (1,1,1,1)
  _MainTex("Albedo", 2D) = "white" {}
  _Smoothness("Smoothness", Range(0,1)) = 0.3
  _Metallic("Metallic", Range(0,1)) = 0
  _EmissionColor("Emission", Color) = (0,0,0,0)
  _RimColor("Rim", Color) = (1,1,1,0.18)
  _RimPower("Rim power", Range(0.5,8)) = 3
 }
 SubShader {
  Tags { "RenderType"="Opaque" "Queue"="Geometry" }
  LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows nolightmap nodynlightmap nodirlightmap nometa exclude_path:deferred exclude_path:prepass
  #pragma target 3.0
  sampler2D _MainTex;
  fixed4 _Color; half _Smoothness; half _Metallic; fixed4 _EmissionColor; fixed4 _RimColor; half _RimPower;
  struct Input { float2 uv_MainTex; float3 viewDir; };
  void surf(Input IN, inout SurfaceOutputStandard o) {
   fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
   o.Albedo = c.rgb;
   o.Metallic = _Metallic;
   o.Smoothness = _Smoothness;
   half rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), _RimPower);
   o.Emission = _EmissionColor.rgb + _RimColor.rgb * _RimColor.a * rim;
   o.Alpha = 1;
  }
  ENDCG
 }
 Fallback "VertexLit"
}
