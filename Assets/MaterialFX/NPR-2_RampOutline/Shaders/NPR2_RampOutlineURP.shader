Shader "Custom/NPR-2/RampOutlineURP"
{
    Properties
    {
        [MainTexture]_BaseMap("Base Map", 2D) = "white" {}
        [MainColor]_BaseColor("Base Color", Color) = (1,1,1,1)

        [NoScaleOffset]_RampMap("Ramp Map", 2D) = "gray" {}
        _RampOffset("Ramp Offset", Range(-1,1)) = 0
        _RampContrast("Ramp Contrast", Range(0.1,3)) = 1.0
        _RampStrength("Ramp Strength", Range(0,2)) = 1.0
        _ShadowStrength("Shadow Strength", Range(0,1)) = 1.0
        _AmbientStrength("Ambient Strength", Range(0,1)) = 0.15

        _SpecColor("Spec Color", Color) = (1,1,1,1)
        _SpecThreshold("Spec Threshold", Range(0,1)) = 0.92
        _SpecSoftness("Spec Softness", Range(0.001,0.2)) = 0.02

        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(0.5,8)) = 3.8
        _RimStrength("Rim Strength", Range(0,2)) = 0.25
        _AdditionalLightStrength("Additional Light Strength", Range(0,2)) = 0.2
        _ColorSaturation("Color Saturation", Range(0,2.5)) = 1.35

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0.07,0.09,0.12,1)
        _OutlineWidth("Outline Width", Range(0,10)) = 4.0

        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        [NoScaleOffset]_NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 280

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

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
            float _RampOffset;
            float _RampContrast;
            float _RampStrength;
            float _ShadowStrength;
            float _AmbientStrength;
            float4 _SpecColor;
            float _SpecThreshold;
            float _SpecSoftness;
            float4 _RimColor;
            float _RimPower;
            float _RimStrength;
            float _AdditionalLightStrength;
            float _ColorSaturation;
            float _Cutoff;
            float _NormalScale;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RampMap); SAMPLER(sampler_RampMap);

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

            Varyings Vert(Attributes IN)
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

            float3 EvalRamp(float ndotl)
            {
                float rampU = saturate(ndotl * 0.5 + 0.5 + _RampOffset);
                rampU = saturate((rampU - 0.5) * _RampContrast + 0.5);
                float3 ramp = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(rampU, 0.5)).rgb;
                return lerp(1.0.xxx, ramp, _RampStrength);
            }

            float3 ApplySaturation(float3 color, float saturation)
            {
                float luma = dot(color, float3(0.299, 0.587, 0.114));
                return lerp(luma.xxx, color, saturation);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                clip(baseSample.a - _Cutoff);
                #endif

                float3 normalWS = GetNormalWS(IN);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                Light mainLight = GetMainLight(IN.shadowCoord);
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                float shadow = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);
                float3 rampMain = EvalRamp(ndotl) * shadow;
                float3 ambient = SampleSH(normalWS) * _AmbientStrength;

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float ndoth = saturate(dot(normalWS, halfDir));
                float spec = smoothstep(_SpecThreshold - _SpecSoftness, _SpecThreshold + _SpecSoftness, ndoth);

                float rim = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower) * _RimStrength;

                float3 litColor = baseSample.rgb * (rampMain * mainLight.color + ambient);
                litColor += _SpecColor.rgb * spec * mainLight.color;
                litColor += _RimColor.rgb * rim;

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light l = GetAdditionalLight(i, IN.positionWS);
                    float nDotLAdd = saturate(dot(normalWS, l.direction));
                    float3 rampAdd = EvalRamp(nDotLAdd) * l.distanceAttenuation * l.shadowAttenuation;
                    litColor += baseSample.rgb * rampAdd * l.color * _AdditionalLightStrength;
                }
                #endif

                litColor = ApplySaturation(litColor, _ColorSaturation);
                return half4(saturate(litColor), baseSample.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertOutline
            #pragma fragment FragOutline
            #pragma multi_compile_fragment _ _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings VertOutline(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs vni = GetVertexNormalInputs(IN.normalOS);
                float3 normalVS = TransformWorldToViewDir(normalize(vni.normalWS), true);
                float2 offset = normalize(normalVS.xy) * (_OutlineWidth * 0.004);
                float4 positionCS = vpi.positionCS;
                positionCS.xy += offset * positionCS.w;
                OUT.positionCS = positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 FragOutline(Varyings IN) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                #endif
                return _OutlineColor;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}
