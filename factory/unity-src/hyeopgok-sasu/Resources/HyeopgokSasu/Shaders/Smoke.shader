Shader "Mgf/HyeopgokSmoke" {
 Properties { _Color("Soft smoke",Color)=(.93,.96,1,.3) }
 SubShader {
  Tags {"RenderType"="Transparent" "Queue"="Transparent-5"}
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off Cull Back
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.5
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   fixed4 _Color;
   struct Input{float4 vertex:POSITION;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Interpolated{float4 pos:SV_POSITION;fixed3 color:COLOR;};
   Interpolated vert(Input v){UNITY_SETUP_INSTANCE_ID(v);Interpolated o;o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color.rgb;return o;}
   fixed4 frag(Interpolated i):SV_Target{return fixed4(i.color*_Color.rgb,_Color.a);}
   ENDCG
  }
 }
}
