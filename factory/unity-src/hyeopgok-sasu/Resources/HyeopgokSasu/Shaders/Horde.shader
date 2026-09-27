// Original flat-shaded army material. Vertex alpha is a *team mask*, not opacity.
// alpha 0: white/shaded team cloth; alpha 1: fixed skin, wood and metal palette.
Shader "Mgf/HyeopgokHorde"
{
    Properties
    {
        _Color ("Team tint", Color) = (1,1,1,1)
        _Flash ("Impact flash", Range(0,1)) = 0
        _Unlit ("Unlit particles", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            fixed4 _Color;
            half _Flash;
            half _Unlit;
            half4 _HyeopgokNightTint;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed3 color : COLOR0;
                float3 worldPos : TEXCOORD0;
                half3 worldNormal : TEXCOORD1;
                SHADOW_COORDS(2)
            };
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld,v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                // Blender's FBX COLOR channel stores authored sRGB palette values.
                // Unlike texture sampling, COLOR interpolators get no automatic decode.
                fixed3 authored = v.color.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                    authored = GammaToLinearSpace(authored);
                #endif
                fixed3 baseColor = lerp(_Color.rgb * authored, authored, v.color.a);
                o.color = baseColor;
                TRANSFER_SHADOW(o);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_LIGHT_ATTENUATION(attenuation,i,i.worldPos);
                half3 normal = normalize(i.worldNormal);
                half diffuse = saturate(dot(normal,normalize(_WorldSpaceLightPos0.xyz)));
                // Cool skylight on side facets, warm key on the upper planes.
                // Keep one forward pass and the existing instancing/team-mask contract.
                half sky = saturate(normal.y * .5 + .5);
                half3 ambient = lerp(half3(.20,.25,.29), half3(.43,.48,.48), sky);
                half3 lit = (ambient + .73 * diffuse * attenuation * _LightColor0.rgb) * _HyeopgokNightTint.rgb;
                half3 light = lerp(lit, half3(1,1,1), _Unlit);
                return fixed4(lerp(i.color*light,fixed3(1,.97,.77),_Flash),1);
            }
            ENDCG
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 3.5
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct shadowInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct shadowOutput { V2F_SHADOW_CASTER; };
            shadowOutput vertShadow(shadowInput v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                shadowOutput o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 fragShadow(shadowOutput i) : SV_Target { SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
}
