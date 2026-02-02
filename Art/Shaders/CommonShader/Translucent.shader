Shader "MUFramework/Translucent"
{
    Properties
    {
        _BaseColor("BaseColor",Color) = (1,1,1,1)
        _BaseMap("BaseMap",2D) = "white"{}
        
        [Header(Translucent)]
        _NormalDestortion("Normal Destortion",Range(0,1)) = 0.5
        _Attenuation("Attenuation",Float) = 0
        _Strength("Strength",Float) = 1
        
        _Specular("Specular",Float) = 1
        _Shininess("Shininess",Float) = 1
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "AdditionalLights"="True"
        }
        LOD 100

        Pass
        {
            Name "Translucent"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            // 只需要定义逐像素方式的额外光
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ USE_FORWARD_PLUS
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogCoord : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewWS : TEXCOORD3;
            };

			// 变量定义在 CBuffer 当中
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _NormalDestortion,_Attenuation,_Strength,_Specular,_Shininess;
            CBUFFER_END
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float4 _BaseMap_ST;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv,_BaseMap);
                o.fogCoord = ComputeFogFactor(o.positionCS.z);
                o.normalWS = TransformObjectToWorldNormal(v.normal);

                float3 positionWS = TransformObjectToWorld(v.positionOS);
                o.viewWS = normalize(_WorldSpaceCameraPos - positionWS);

                return o;
            }

            half3 LightingTranslucent(Varyings i,float3 lightDir,half3 color)
            {
                half3 L = lightDir;
                float3 N = normalize(i.normalWS);
                half3 V = i.viewWS;
                half3 H = L + N * _NormalDestortion;
                half _LdotV = dot(-H,V);
                half3 I = pow(saturate(_LdotV),_Attenuation) * _Strength;
                
                return I;
            }
            
            half4 frag (Varyings i) : SV_Target
            {
                half4 color;
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                color = baseMap * _BaseColor;
                color.rgb = MixFog(color.rgb,i.fogCoord);

                // --------- 简单的漫反射效果 ----------
                float3 N = normalize(i.normalWS);
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;
                half NdotL = max(0.2,dot(N,L));
                color = NdotL;

                // --------- 透射 -----------
                half3 I = LightingTranslucent(i,L,color);
                I *= mainLight.color;
                
                color.rgb *= I;
                
                // --------- 高光 -----------
                float3 V = i.viewWS;
                float3 H = normalize(L + V);
                half NdotH = saturate(dot(N,H));
                half specular = _Specular * pow(NdotH,_Shininess) * mainLight.color;
                color += specular;

                // --------- 额外光的透射 ----------
                #if defined(_ADDITIONAL_LIGHTS)
                    return 1;  
                    uint pixelLightCount = GetAdditionalLightsCount();

                    #if USE_FORWARD_PLUS
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

                        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                        #ifdef _LIGHT_LAYERS
                        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                        #endif
                        {
                            lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData);
                        }
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                        #ifdef _LIGHT_LAYERS
                        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                        #endif
                        {
                            lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData);
                        }
                    LIGHT_LOOP_END
                #endif
                
                return color;
            }
            
            ENDHLSL
        }
    }
}