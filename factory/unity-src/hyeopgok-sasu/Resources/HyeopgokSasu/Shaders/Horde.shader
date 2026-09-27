// Blender AO is baked in COLOR.a; UV0=(teamMask,1) marks baked models.
// Procedural FX without UV retain legacy COLOR.a team-mask compatibility.
Shader "Mgf/HyeopgokHorde"
{
    Properties
    {
        _Color ("Team tint", Color) = (1,1,1,1)
        _Flash ("Impact flash", Range(0,1)) = 0
        _Unlit ("Unlit particles", Range(0,1)) = 0
        _Rim ("Hero pearl rim", Range(0,1)) = 0
        _Metallic ("Coin metal glint", Range(0,1)) = 0
        _TeamRed ("Troop red", Color) = (.63,.001,.004,1)
        _TeamBlue ("Troop blue", Color) = (.004,.17,.67,1)
        _Team ("Per instance team", Float) = -1
        _InstancePhase ("Per instance gait phase", Float) = 0
        _Attack ("Per instance attack gate", Float) = 0
        _HitTime ("Per soldier hit time", Float) = -1000
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
            half _Rim;
            half _Metallic;
            fixed4 _TeamRed;
            fixed4 _TeamBlue;
            UNITY_INSTANCING_BUFFER_START(HitProperties)
                UNITY_DEFINE_INSTANCED_PROP(float, _HitTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _Team)
                UNITY_DEFINE_INSTANCED_PROP(float, _InstancePhase)
                UNITY_DEFINE_INSTANCED_PROP(float, _Attack)
            UNITY_INSTANCING_BUFFER_END(HitProperties)
            half4 _HyeopgokNightTint;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float2 bake : TEXCOORD0;
                float2 motion : TEXCOORD1;
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
                half hitFlash : TEXCOORD5;
            };
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float team=UNITY_ACCESS_INSTANCED_PROP(HitProperties, _Team);
                half troop=step(-.5,team);
                float phase=UNITY_ACCESS_INSTANCED_PROP(HitProperties, _InstancePhase);
                half attacking=saturate(UNITY_ACCESS_INSTANCED_PROP(HitProperties, _Attack));
                half stride=sin(_Time.y*11.5+phase+v.motion.y*6.2832);
                half attackPulse=attacking*pow(saturate(sin(_Time.y*13.5+phase)),2);
                half upper=saturate(v.vertex.y*.82+.16);
                half leftLeg=saturate(1-abs(v.motion.x-.25)*8);
                half rightLeg=saturate(1-abs(v.motion.x-.50)*8);
                half weapon=saturate(1-abs(v.motion.x-.75)*8);
                // Every instance shares a rigid mesh; gait phase and attack lean are
                // GPU-only. UV1's rigid-part mask separates opposing legs and gives
                // the weapon arm extra attack travel without animator objects.
                v.vertex.y+=troop*(abs(stride)*.026-upper*attackPulse*.045);
                v.vertex.z+=troop*((leftLeg-rightLeg)*stride*.042+upper*stride*.012+upper*attackPulse*.085+weapon*attackPulse*.105);
                v.vertex.x+=troop*weapon*attackPulse*.038;
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
                fixed3 teamColor=lerp(_TeamRed.rgb,_TeamBlue.rgb,saturate(team));
                fixed3 baseColor = lerp(_Color.rgb * authored, authored, teamMask);
                fixed3 troopColor=lerp(teamColor*authored,authored,teamMask);
                baseColor=lerp(baseColor,troopColor,troop);
                o.color = baseColor * ao;
                float sinceHit = _Time.y - UNITY_ACCESS_INSTANCED_PROP(HitProperties, _HitTime);
                o.hitFlash = saturate(1-sinceHit/.06) * step(0,sinceHit);
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
                half3 lit = (ambient + .85 * diffuse * _LightColor0.rgb) * _HyeopgokNightTint.rgb;
                half3 halfVector=normalize(normalize(_WorldSpaceLightPos0.xyz)+normalize(_WorldSpaceCameraPos-i.worldPos));
                half armorGleam=pow(saturate(dot(normal,halfVector)),18)*.045*i.paintedArmor*attenuation;
                half3 coolShade=half3(.0252,.1441,.1022); // linear #2C6A5A
                half3 shaded=lerp(i.color*.62,coolShade,.23)*_HyeopgokNightTint.rgb;
                half3 litColor=lerp(shaded,i.color*lit+armorGleam,attenuation);
                litColor=lerp(litColor,i.color,_Unlit);
                half metalGleam=pow(saturate(dot(normal,halfVector)),24)*_Metallic*attenuation;
                litColor+=half3(1,.78,.22)*metalGleam*.72;
                half rim=pow(1-saturate(dot(normal,normalize(_WorldSpaceCameraPos-i.worldPos))),3)*_Rim;
                litColor+=half3(.82,.94,1)*rim;
                fixed4 result=fixed4(lerp(litColor,fixed3(1,.995,1),max(_Flash,i.hitFlash)),1);
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
