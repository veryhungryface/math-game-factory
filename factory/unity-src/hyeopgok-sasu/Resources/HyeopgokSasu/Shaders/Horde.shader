// Blender AO is baked in COLOR.a; UV0=(teamMask,1) marks baked models.
// Procedural FX without UV retain legacy COLOR.a team-mask compatibility.
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
            #pragma multi_compile_fog
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
                float2 bake : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed3 color : COLOR0;
                float3 worldPos : TEXCOORD0;
                half3 worldNormal : TEXCOORD1;
                SHADOW_COORDS(2)
                UNITY_FOG_COORDS(3)
                half paintedArmor : TEXCOORD4;
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
                half baked=step(.5,v.bake.y);
                half teamMask=lerp(v.color.a,v.bake.x,baked);
                half ao=lerp(1,v.color.a,baked);
                o.paintedArmor=(1-teamMask)*baked;
                fixed3 baseColor = lerp(_Color.rgb * authored, authored, teamMask);
                o.color = baseColor * ao;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o,o.pos);
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
                half3 ambient = lerp(half3(.080,.170,.140), half3(.300,.410,.400), sky);
                half3 lit = (ambient + .85 * diffuse * attenuation * _LightColor0.rgb) * _HyeopgokNightTint.rgb;
                half3 halfVector=normalize(normalize(_WorldSpaceLightPos0.xyz)+normalize(_WorldSpaceCameraPos-i.worldPos));
                half armorGleam=pow(saturate(dot(normal,halfVector)),18)*.045*i.paintedArmor*attenuation;
                half3 light = lerp(lit, half3(1,1,1), _Unlit);
                fixed4 result=fixed4(lerp(i.color*light+armorGleam,fixed3(1,.985,.94),_Flash),1);
                UNITY_APPLY_FOG(i.fogCoord,result);
                return result;
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
