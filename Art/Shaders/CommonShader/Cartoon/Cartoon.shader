Shader "MUFramework/Cartoon"
{
    Properties
    {
        _BaseColor("BaseColor",Color) = (1,1,1,1)
        _BaseMap("BaseMap",2D) = "white"{}
        
        [Header(Outline)]
        _OutlineWidth("OutlineWidth",Range(0,1)) = 0.4
        _OutlineColor("OutlineColor",Color) = (0,0,0,1)
        _UnifomWidth("UnifomWidth",Range(0,1)) = 0.5
        
        [Header(Color)]
        [IntRange]_StepColor("StepColor",Range(1,10)) = 5
        _ShadowInstensity("ShadowInstensity",Range(0,1)) = 0.221
        _Instensity("Instensity",Range(0,5)) = 2.43
        
        [Header(SpecularAndFresnel)]
        _Specular("高光强度（X） 扩散度（Y） 高光柔和度（Z） 高光透明度（W）",Vector) = (1,30,0.4,0.5)
        _Fresnel("外发光强度（X）扩散度（Y）过渡柔和度（Z）透明度（W）",Vector) = (20,1.8,0.1,0.5)
        _FresnelColor("FresnelColor",Color) = (0.2,0.5,0.2,1)
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            
            Name "CartoonLit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            // 1. 定义阴影相关的宏
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
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
                float3 normalWS : TEXCOORD1;
                half3 viewWS : TEXCOORD2;

                // 2. 定义阴影坐标
                float4 shadowCoord  : TEXCOORD3;
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _StepColor;
                half _Instensity;
                half _ShadowInstensity;
                half4 _Specular;
                half4 _Fresnel;
                half4 _FresnelColor;
            CBUFFER_END
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float4 _BaseMap_ST;
            TEXTURE2D(_ShadowRampTex);SAMPLER(sampler_ShadowRampTex);

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv,_BaseMap);
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                float3 positionWS = TransformObjectToWorld(v.positionOS);
                o.viewWS = normalize(_WorldSpaceCameraPos - positionWS);

                // 3. 得到阴影坐标
                o.shadowCoord = TransformWorldToShadowCoord(positionWS);

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 color;
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                color = baseMap * _BaseColor;
                //return color;
                
                // Lambert 光照求出模型表面0~1的明暗面，然后再利用硬边明暗面
                // 4. 在片段着色器当中，使用传入阴影采样坐标的 i.shadowCoord 的 GetMainLight 函数重载
                Light light = GetMainLight(i.shadowCoord);
                half3 L = light.direction;
                half3 N = normalize(i.normalWS);
                half3 V = normalize(i.viewWS);
                half NdotL = dot(N,L) * 0.5 + 0.5; // 从 -1~1 变换到 0~1

                // 明暗色阶
                half4 level;
                {
                    // 多阶上色
                    // level= ceil(NdotL * _StepColor) / _StepColor;

                    // 使用贴图实现上色
                    half4 shadowRampMap = SAMPLE_TEXTURE2D(_ShadowRampTex,sampler_ShadowRampTex,NdotL);
                    level = shadowRampMap;
                }
                
                // 二分色为白色的时候，保留原图。黑色的时候，变黑
                color *= level;
                color *= _Instensity;
                if (level.r < 1 - _ShadowInstensity)
                {
                    color *= _ShadowInstensity;
                }

                color *= light.shadowAttenuation;

                // 高光
                half4 specular;
                {
                    half3 H = normalize(L + V);
                    half NdotH = saturate(dot(N,H));
                    specular = _Specular.x * pow(NdotH,_Specular.y);
                    specular = smoothstep(0.5,0.5 + _Specular.z,specular);
                    specular *= _Specular.w;
                    specular *= _MainLightColor;
                    color += specular;
                }

                // 外发光
                half4 frensel;
                {
                    half NdotV = saturate(dot(N,V));
                    frensel = 1- _Fresnel.x * pow(NdotV,_Fresnel.y);
                    frensel = smoothstep(0.5,0.5 + _Fresnel.z,frensel);
                    frensel *= _MainLightColor;
                    frensel *= _Fresnel.w;
                    color += frensel;
                }

                
                
                return color;
            }
            
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "SRPDefaultUnlit1"
            }
            
            Stencil
            {
                Ref 1
                Comp NotEqual
            }
            
            Cull Front
            
            Name "CartoonVertexOffset"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                // 这个属性可以在外部的DCC软件当中，为其赋值
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : TEXCOORD0;
            };

			// 变量定义在 CBuffer 当中
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _OutlineWidth;
                half4 _OutlineColor;
                half _UnifomWidth;
            CBUFFER_END
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float4 _BaseMap_ST;

            Varyings vert (Attributes v)
            {
                Varyings o;
                
                // 思路：描边的宽度本身不会变化，只是由于我们离的远了，所以感觉是变细了，因此只需要对描边宽度乘上一个越来越大的值做为弥补即可
                // 求出相机和顶点间的距离
                float3 positionWS = TransformObjectToWorld(v.positionOS);
                // 当 distance = 1 时，就表示正常的进大远小的表现
                float distance = length(_WorldSpaceCameraPos - positionWS);
                distance = lerp(1,distance,_UnifomWidth);
                
                float3 positionOS = v.positionOS;
                float3 width = normalize(v.normalOS) * _OutlineWidth;
                width *= distance;
                width *= v.color.a;
                positionOS += width;
                
                o.positionCS = TransformObjectToHClip(positionOS);
                
                o.color = v.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(_OutlineColor.rgb * i.color.rgb,1);
            }
            
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            
            float3 ApplyShadowBias1(float3 positionWS, float3 normalWS, float3 lightDirection)
            {
                float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
                float scale = invNdotL * _ShadowBias.y;

                // normal bias is negative since we want to apply an inset normal offset
                positionWS = lightDirection * _ShadowBias.xxx + positionWS;
                positionWS = normalWS * scale.xxx + positionWS;
                return positionWS;
            }
            
            float4 GetShadowPositionHClip1(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias1(positionWS, normalWS, lightDirectionWS));
                positionCS = ApplyShadowClamping(positionCS);
                return positionCS;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                #if defined(_ALPHATEST_ON)
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                #endif

                output.positionCS = GetShadowPositionHClip1(input);
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(_ALPHATEST_ON)
                    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
                #endif

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                return 0;
            }
            
            ENDHLSL
        }
    }
}