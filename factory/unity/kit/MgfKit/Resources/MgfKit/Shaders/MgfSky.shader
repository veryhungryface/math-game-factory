// MGF 킷 그라디언트 하늘 (스카이박스). MgfLook.Sky(top, horizon, bottom).
Shader "Mgf/Sky" {
 Properties {
  _Top("Top", Color) = (0.35,0.55,0.9,1)
  _Horizon("Horizon", Color) = (0.85,0.9,1,1)
  _Bottom("Bottom", Color) = (0.6,0.65,0.7,1)
  _Sharp("Horizon sharpness", Range(0.2,8)) = 1.6
 }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _Top, _Horizon, _Bottom; half _Sharp;
   struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
   v2f vert(float4 v:POSITION){ v2f o; o.pos=UnityObjectToClipPos(v); o.dir=v.xyz; return o; }
   fixed4 frag(v2f i):SV_Target{
    float y = normalize(i.dir).y;
    fixed3 up = lerp(_Horizon.rgb, _Top.rgb, pow(saturate(y), 1.0/_Sharp));
    fixed3 dn = lerp(_Horizon.rgb, _Bottom.rgb, pow(saturate(-y), 1.0/_Sharp));
    return fixed4(y >= 0 ? up : dn, 1);
   }
   ENDCG
  }
 }
}
