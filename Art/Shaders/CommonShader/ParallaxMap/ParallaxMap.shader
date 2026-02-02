Shader "MUFramework/ParallaxMap"
{
    Properties
    {
        _BaseColor("BaseColor",Color) = (1,1,1,1)
        [MainTexture]_BaseMap("BaseMap",2D) = "white"{}
        [Normal]_NormalMap("NormalMap",2D) = "bump"{}
        [KeywordEnum(Default,Limit,Steep,Relief,POM)]_ParallaxType("ParallaxType",Int) = 0
        _ParallaxMap("ParallaxMap",2D) = "white"{}
        _ParallaxStrength("ParallaxStrength",Range(0,0.5)) = 0
        _ParallaxAmount("ParallaxAmount",Int) = 20
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
            Name "SimplesUnlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature _ _PARALLAXTYPE_DEFAULT _PARALLAXTYPE_LIMIT _PARALLAXTYPE_STEEP _PARALLAXTYPE_RELIEF _PARALLAXTYPE_POM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half3 normalOS : NORMAL;
                half4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogCoord : TEXCOORD1;
                half4 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half4 bitangentWS : TEXCOORD4;
                half3 positionWS : TEXCOORD5;
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _ParallaxStrength;
                half _ParallaxAmount;
            CBUFFER_END
            
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float4 _BaseMap_ST;
            TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
            TEXTURE2D(_ParallaxMap);SAMPLER(sampler_ParallaxMap);

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv,_BaseMap);
                o.fogCoord = ComputeFogFactor(o.positionCS.z);
                
                o.normalWS.xyz = TransformObjectToWorldNormal(v.normalOS);
                o.tangentWS.xyz = TransformObjectToWorldDir(v.tangentOS.xyz);
                half sign = v.tangentOS.w * GetOddNegativeScale();
                o.bitangentWS.xyz = cross(o.normalWS,o.tangentWS) * sign;
                o.positionWS = TransformObjectToWorld(v.positionOS);
                
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 无论哪种视差映射，都需要偏移量以及切线空间下的视角向量
                float2 offset = 0;
                half3 V = normalize(i.positionWS - _WorldSpaceCameraPos.xyz);
                V = mul(half3x3(i.tangentWS.xyz,i.bitangentWS.xyz,i.normalWS.xyz),V); // 将 V 变换到切线空间下
                
                #if _PARALLAXTYPE_DEFAULT
                    // 基本视差映射：T = V.xy * (H/V.z)
                    half parallaxMap = 1 - SAMPLE_TEXTURE2D(_ParallaxMap,sampler_ParallaxMap,i.uv).r;
                    half H = parallaxMap;
                    offset = V.xy * (H/-V.z) * _ParallaxStrength;
                #elif _PARALLAXTYPE_LIMIT
                    // 带偏移上限的视差映射：T = V.xy * H
                    half parallaxMap = 1 - SAMPLE_TEXTURE2D(_ParallaxMap,sampler_ParallaxMap,i.uv).r;
                    half H = parallaxMap;
                    offset = V.xy * H * _ParallaxStrength;
                #elif _PARALLAXTYPE_STEEP
                    // ------------ 陡峭视差映射 ------------------
                    half currentDepth = 0;
                    half parallaxDepth = 0;
                    half heightStep = 1/_ParallaxAmount;
                    half2 offsetTemp = V.xy/(-V.z) * _ParallaxStrength;
                    
                    for (int index = 0; index < _ParallaxAmount; index ++ )
                    {
                        parallaxDepth = 1 - SAMPLE_TEXTURE2D_LOD(_ParallaxMap,sampler_ParallaxMap,i.uv + offset,0).r;
                        // 如果当前迭代的深度值 > 深度图中的深度值，则退出循环
                        if (currentDepth > parallaxDepth)
                        {
                            break;
                        }
                        // 每次循环结束后，当前深度值往前步进 0.1 
                        currentDepth += heightStep;
                    
                        offset = offsetTemp * currentDepth;
                    }
                #elif _PARALLAXTYPE_RELIEF
                    // ------------ 浮雕视差映射 ------------------
                    half currentDepth = 0;
                    half parallaxDepth = 0;
                    half heightStep = 1/_ParallaxAmount;
                    half2 offsetTemp = V.xy/(-V.z) * _ParallaxStrength;
                    
                    for (int index = 0; index < _ParallaxAmount; index ++ )
                    {
                        parallaxDepth = 1 - SAMPLE_TEXTURE2D_LOD(_ParallaxMap,sampler_ParallaxMap,i.uv + offset,0).r;
                        // 如果当前迭代的深度值 > 深度图中的深度值，则退出循环
                        if (currentDepth > parallaxDepth)
                        {
                            break;
                        }
                        // 每次循环结束后，当前深度值往前步进 0.1 
                        currentDepth += heightStep;
                    
                        offset = offsetTemp * currentDepth;
                    }

                    for (int index = 0; index < 5; index++)
                    {
                        heightStep /= 2;
                        
                        if (currentDepth > parallaxDepth)
                        {
                            currentDepth -= heightStep;
                            offset = offsetTemp * currentDepth;
                            parallaxDepth = 1 - SAMPLE_TEXTURE2D_LOD(_ParallaxMap,sampler_ParallaxMap,i.uv + offset,0).r;
                        }
                        else
                        {
                            currentDepth += heightStep;
                            offset = offsetTemp * currentDepth;
                            parallaxDepth = 1 - SAMPLE_TEXTURE2D_LOD(_ParallaxMap,sampler_ParallaxMap,i.uv + offset,0).r;
                        }
                    }
                #elif _PARALLAXTYPE_POM
                    // ------------ 视差遮蔽映射 ------------------
                    half currentDepth = 0;
                    half parallaxDepth = 0; // 当前深度值
                    half preParallaxDepth = 0; // 上一次的深度值
                    half heightStep = 1/_ParallaxAmount;
                    half2 offsetTemp = V.xy/(-V.z) * _ParallaxStrength;
                    
                    for (int index = 0; index < _ParallaxAmount; index ++ )
                    {
                        parallaxDepth = 1 - SAMPLE_TEXTURE2D_LOD(_ParallaxMap,sampler_ParallaxMap,i.uv + offset,0).r;
                        // 如果当前迭代的深度值 > 深度图中的深度值，则退出循环
                        if (currentDepth > parallaxDepth)
                        {
                            preParallaxDepth = parallaxDepth;
                            break;
                        }
                        // 每次循环结束后，当前深度值往前步进 0.1 
                        currentDepth += heightStep;
                    
                        offset = offsetTemp * currentDepth;
                    }
                    half preDepth = currentDepth - heightStep;
                    half A_C = preDepth - preParallaxDepth;
                    half D_B = parallaxDepth - currentDepth;
                    half t = A_C / (D_B + A_C);
                    half height = lerp(preDepth,currentDepth,t);
                    offset = offsetTemp * height;
                #endif
                
                i.uv += offset; // 使用偏移之后的 UV 进行采样
                
                half4 color;
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                color = baseMap * _BaseColor;
                color.rgb = MixFog(color.rgb,i.fogCoord);

                half3 normalMap = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv));
                half3 normalWS = mul(normalMap,half3x3(i.tangentWS.xyz,i.bitangentWS.xyz,i.normalWS.xyz));

                Light mainLight = GetMainLight();
                half3 L = mainLight.direction;
                half NdotL = saturate(dot(normalWS,L));
                color *= NdotL;
                
                return color;
            }
            
            ENDHLSL
        }
    }
}