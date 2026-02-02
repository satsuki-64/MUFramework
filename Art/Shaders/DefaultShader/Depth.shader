Shader "Unlit/Depth"
{
    Properties
    {
        _BaseColor("BaseColor",Color) = (1,1,1,1)
        _BaseMap("BaseMap",2D) = "white"{}
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
            #define REQUIRE_OPAQUE_TEXTURE
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

			// 变量定义在 CBuffer 当中
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float4 _BaseMap_ST;
            // 采样深度图
            TEXTURE2D(_CameraDepthTexture);SAMPLER(sampler_CameraDepthTexture);
            // TEXTURE2D(_CameraOpaqueTexture);SAMPLER(sampler_CameraOpaqueTexture);
            
            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv,_BaseMap);
                o.screenPos = ComputeScreenPos(o.positionCS);
                
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 方法一：使用 ComputeScreenPos 的计算结果
                float2 uv = i.screenPos.xy/i.screenPos.w;
                
                // 方法二：直接使用内置参数
                float2 uv2 = i.positionCS / _ScreenParams.xy;

                // 采样深度图
                half4 depthMap = SAMPLE_TEXTURE2D(_CameraDepthTexture,sampler_CameraDepthTexture,uv2);
                half depth = Linear01Depth(depthMap,_ZBufferParams) * 100;

                // 采样抓取图 - 方法一
                // half4 opaqueMap = SAMPLE_TEXTURE2D(_CameraOpaqueTexture,sampler_CameraOpaqueTexture,uv2);
                
                // 采样抓取图 - 方法二
                half4 opaqueMap2 = float4(SampleSceneColor(uv2),1);
                
                return opaqueMap2;
            }
            
            ENDHLSL
        }
    }
}