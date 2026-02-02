Shader "Unlit/CustomMaterialGUI"
{
    Properties
    {
        _BaseColor("BaseColor",Color) = (1,1,1,1)
        _BaseMap("BaseMap",2D) = "white"{}
        _FloatTest("FloatTest",Range(0,1)) = 1
        _VectorTest("VectorTest",Vector) = (0,0,0,0)
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
            #pragma multi_compile _ _VECTORENABLED_ON
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
                float fogCoord : TEXCOORD1;
            };

			// 变量定义在 CBuffer 当中
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _FloatTest;
                half4 _VectorTest;
            CBUFFER_END
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float4 _BaseMap_ST;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv,_BaseMap);
                o.fogCoord = ComputeFogFactor(o.positionCS.z);

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 color;
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                color = baseMap * _BaseColor;
                color.rgb = MixFog(color.rgb,i.fogCoord);

                #ifdef _VECTORENABLED_ON
                    return 1;
                #else
                    return 0.5;
                #endif
                
                return _FloatTest * _VectorTest;
            }
            
            ENDHLSL
        }
    }
    
    CustomEditor "Shader.CustomMaterialGUI"
}