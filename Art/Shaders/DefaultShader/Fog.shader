Shader "Unlit/Fog"
{
    Properties
    {
        _Color("Color",Color) = (1,0,0,1)
        _Fresnel("Pow(X) Offset(Y) Top(Z)",Vector) = (1,1,1,1)
        _OffsetRange("RepeatX(x) IntensityX(y) RepeatZ(z) IntensityZ(w)",Vector) = (0,0,0,0)
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        Blend One One
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 1. 添加支持雾效的变体
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 vertexOS : POSITION;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                // 模型在世界空间下的法线坐标
                half3 normalWS : TEXCOORD0;

                // 世界空间
                float3 positionWS : TEXCOORD1;

                // 模型空间
                float4 vertexOS : TEXCOORD2;

                // 裁剪空间
                float4 vertexCS : SV_POSITION;

                // 2. 雾效坐标
                float fogCoord : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Fresnel;
                float4 _OffsetRange;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;

                // 获取模型的本地空间坐标i
                o.vertexOS = v.vertexOS;

                // 以下代码的含义，是当前模型的位置越高，其偏移范围越大
                v.vertexOS.x += sin((_Time.y + v.vertexOS.y)*_OffsetRange.x) * _OffsetRange.y;
                v.vertexOS.z += sin((_Time.y + v.vertexOS.y)*_OffsetRange.z) * _OffsetRange.w;
                
                // 从模型空间变换到世界空间的法线
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                
                // 从模型空间变换到裁剪空间
                o.vertexCS = TransformObjectToHClip(v.vertexOS);

                // 从模型空间变换到世界空间
                o.positionWS = TransformObjectToWorld(v.vertexOS);

                // 3. 计算雾因子
                o.fogCoord = ComputeFogFactor(o.vertexCS.z);
                return o;
            }

            // 注意：这里的返回值一定要是half4类型！不要写成half3类型
            half4 frag (Varyings i) : SV_Target
            {
                half3 N = i.normalWS;
                half3 V = normalize(_WorldSpaceCameraPos - i.positionWS);
                float f = (1 - max(0,dot(N,V)));
                half fresnel = pow(f,_Fresnel.x);
                float4 color = half4(fresnel * _Color.rgb, fresnel * _Color.a);  

                // 创建出从上到下的黑白遮罩
                half mask = max(0,i.vertexOS.y + i.vertexOS.z + _Fresnel.y);
                float4 ColorNew = lerp(color, _Color, mask*_Fresnel.z);

                // 4. 混合雾效颜色和原颜色
                ColorNew.rgb = MixFog(ColorNew,i.fogCoord);
                
                return ColorNew * mask;
            }
            
            ENDHLSL
        }
    }
}