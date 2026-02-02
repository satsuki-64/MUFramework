Shader "MUFramework/GroundDisappear"
{
    Properties
    {
        _BaseColor("BaseColor",Color) = (1,1,1,1)
        _BaseMap("BaseMap",2D) = "white"{}
        _DisappearPos1("DisappearPos1",Vector) = (0,0,0,0)
        _DisappearPos2("DisappearPos2",Vector) = (0,0,0,0)
        _Radius("Radius",Range(0,10)) = 1
        _Fade("Fade",Range(0,3)) = 1
        _FadeColor("FadeColor",Color) = (1,1,1,1)
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
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogCoord : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 worldNormal : TEXCOORD3;
            };

			// 变量定义在 CBuffer 当中
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _DisappearPos1;
                half4 _DisappearPos2;
                half _Radius;
                half _Fade;
                half4 _FadeColor;
            CBUFFER_END
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float4 _BaseMap_ST;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv,_BaseMap);
                o.fogCoord = ComputeFogFactor(o.positionCS.z);
                o.worldNormal = TransformObjectToWorldNormal(v.normalOS);

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 color = 0;
                
                // Lambert 光照
                half NdotL = dot(i.worldNormal,_MainLightPosition);
                half diffuse = (NdotL * 0.5 + 0.5) * 0.4;
                color += diffuse;
                
                float distance0 = distance(_DisappearPos1,i.positionWS);
                float fadeRange = _Radius - _Fade;
                
                // 将半径以外的区域剔除掉
                clip(_Radius-distance0);
                // 在 FadeRange 区域内，叠加光滑过渡的颜色
                
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                color += (baseMap * _BaseColor);

                float fadeInstensity = (saturate(distance0 - fadeRange));
                color += (_FadeColor * fadeInstensity);
                color.rgb = MixFog(color.rgb,i.fogCoord);
                
                return color;
            }
            
            ENDHLSL
        }
    }
}