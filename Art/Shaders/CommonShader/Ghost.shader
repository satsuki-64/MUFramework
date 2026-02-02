Shader "MUFramework/Ghost"
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
            // 1. 将渲染类型改为透明，队列设置为透明队列
            "RenderType"="Transparent"
            "Queue"="Transparent" // 确保在透明阶段渲染
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        
        // 2. 推荐使用更适合半透明效果的混合模式
        // 标准的Alpha混合
        // Blend SrcAlpha OneMinusSrcAlpha
        // 如果你确实需要明亮的叠加效果，可以保留 Blend One One，但需注意其特性
        Blend One One
        // Blend One Zero
        
        // 3. 关闭深度写入，防止半透明部分出现深度排序问题
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP渲染管线下通常都要引用的hlsl文件
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 vertexOS : POSITION;
                
                // 模型在本地空间下的法线坐标
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
                
                return ColorNew * mask;
            }
            
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            // 1. 将渲染类型改为透明，队列设置为透明队列
            "RenderType"="Transparent"
            "Queue"="Transparent" // 确保在透明阶段渲染
        }
        LOD 100
        
        // 2. 推荐使用更适合半透明效果的混合模式
        // 标准的Alpha混合
        // Blend SrcAlpha OneMinusSrcAlpha
        // 如果你确实需要明亮的叠加效果，可以保留 Blend One One，但需注意其特性
        Blend One One
        // Blend One Zero
        
        // 3. 关闭深度写入，防止半透明部分出现深度排序问题
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP渲染管线下通常都要引用的hlsl文件
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 vertexOS : POSITION;
                
                // 模型在本地空间下的法线坐标
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
            };

            float4 _Color;
            float4 _Fresnel;
            float4 _OffsetRange;


            Varyings vert (Attributes v)
            {
                Varyings o;

                // 获取模型的本地空间坐标i
                o.vertexOS = v.vertexOS;

                // 以下代码的含义，是当前模型的位置越高，其偏移范围越大
                v.vertexOS.x += sin((_Time.y + v.vertexOS.y)*_OffsetRange.x) * _OffsetRange.y;
                v.vertexOS.z += sin((_Time.y + v.vertexOS.y)*_OffsetRange.z) * _OffsetRange.w;
                
                // 从模型空间变换到世界空间的法线
                o.normalWS = UnityObjectToWorldNormal(v.normalOS);
                
                // 从模型空间变换到裁剪空间
                o.vertexCS = UnityObjectToClipPos(v.vertexOS);

                // 从模型空间变换到世界空间
                o.positionWS = mul(unity_ObjectToWorld,v.vertexOS);

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
                
                return ColorNew * mask;
            }
            
            ENDCG
        }
    }
}
