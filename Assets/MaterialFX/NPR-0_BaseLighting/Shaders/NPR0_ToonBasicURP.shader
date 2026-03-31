Shader "Custom/NPR-0/ToonBasicURP"
{
    Properties
    {
        [MainTexture]_BaseMap("Base Map", 2D) = "white" {}
        [MainColor]_BaseColor("Base Color", Color) = (1,1,1,1)

        _ShadeColor("Shade Color", Color) = (0.50,0.53,0.60,1)
        _ShadeThreshold("Shade Threshold", Range(0,1)) = 0.62
        _ShadeSoftness("Shade Softness", Range(0.001,0.3)) = 0.03
        _ShadowStrength("Shadow Strength", Range(0,1)) = 1.0
        _AmbientStrength("Ambient Strength", Range(0,1)) = 0.16

        _SpecColor("Spec Color", Color) = (1,1,1,1)
        _SpecThreshold("Spec Threshold", Range(0,1)) = 0.88
        _SpecSoftness("Spec Softness", Range(0.001,0.2)) = 0.02

        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(0.5,8)) = 3.8
        _RimStrength("Rim Strength", Range(0,2)) = 0.42
        _AdditionalLightStrength("Additional Light Strength", Range(0,2)) = 0.2

        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        [NoScaleOffset]_NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _ShadeColor;
            float _ShadeThreshold;
            float _ShadeSoftness;
            float _ShadowStrength;
            float _AmbientStrength;
            float4 _SpecColor;
            float _SpecThreshold;
            float _SpecSoftness;
            float4 _RimColor;
            float _RimPower;
            float _RimStrength;
            float _AdditionalLightStrength;
            float _Cutoff;
            float _NormalScale;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs vni = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = vpi.positionCS;
                OUT.positionWS = vpi.positionWS;
                OUT.normalWS = NormalizeNormalPerVertex(vni.normalWS);
                OUT.tangentWS = NormalizeNormalPerVertex(vni.tangentWS);
                OUT.bitangentWS = NormalizeNormalPerVertex(vni.bitangentWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = GetShadowCoord(vpi);
                return OUT;
            }

            float3 GetNormalWS(Varyings IN)
            {
                float3 n = normalize(IN.normalWS);
                float3 t = normalize(IN.tangentWS);
                float3 b = normalize(IN.bitangentWS);
                float3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv), _NormalScale);
                return normalize(nTS.x * t + nTS.y * b + nTS.z * n);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                clip(baseSample.a - _Cutoff);
                #endif

                float3 normalWS = GetNormalWS(IN);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                Light mainLight = GetMainLight(IN.shadowCoord);
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                float diffuseBand = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, ndotl);

                float shadow = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);
                float3 toonRamp = lerp(_ShadeColor.rgb, 1.0, diffuseBand) * shadow;

                float3 ambient = SampleSH(normalWS) * _AmbientStrength;

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float ndoth = saturate(dot(normalWS, halfDir));
                float spec = smoothstep(_SpecThreshold - _SpecSoftness, _SpecThreshold + _SpecSoftness, ndoth);

                float rim = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower) * _RimStrength;
                rim *= diffuseBand;

                float3 litColor = baseSample.rgb * (toonRamp * mainLight.color + ambient);
                litColor += _SpecColor.rgb * spec * mainLight.color;
                litColor += _RimColor.rgb * rim;

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light l = GetAdditionalLight(i, IN.positionWS);
                    float nDotLAdd = saturate(dot(normalWS, l.direction));
                    float bandAdd = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, nDotLAdd);
                    float3 rampAdd = lerp(_ShadeColor.rgb, 1.0.xxx, bandAdd) * l.distanceAttenuation * l.shadowAttenuation;
                    litColor += baseSample.rgb * rampAdd * l.color * _AdditionalLightStrength;
                }
                #endif

                return half4(saturate(litColor), baseSample.a);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}
