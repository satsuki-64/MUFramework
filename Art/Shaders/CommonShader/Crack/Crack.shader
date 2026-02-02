Shader "MUFramework/Crack"
{
    Properties
    {
        _Color("Color",Color) = (1,1,1,1)
        _CrackInstensity("CrackInstensity",Float) = 8
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
            Stencil
            {
                // 当前Shader的材质只是为了用于遮挡模型的部分区域，因此Ref值可以任意设置
                Ref 1
                
                // 不管如何，模板测试总是不能通过
                Comp Equal
            }
            
            Tags
            {
                // 在第一个负责颜色绘制的Pass当中，将渲染模式设置为 UniversalForward
                "LightMode" = "UniversalForward"
            }
            
            ZTest Always
            Name "Crack02"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 positionOS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _CrackInstensity;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.positionOS = v.positionOS;
                
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 color;
                half mask = abs(i.positionOS.y)/_CrackInstensity;
                float t = abs(sin(_Time.y*0.3))+0.3;
                color = lerp(0,_Color * t,mask);
                
                return color;
            }
            
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                // 在第二个负责模板测试的Pass当中，将模式设置为SRPDefaultUnlit
                "LightMode" = "SRPDefaultUnlit"
            }
            
            Stencil
            {
                // 当前Shader的材质只是为了用于遮挡模型的部分区域，因此Ref值可以任意设置
                Ref 1
                // 不管如何，模板测试总是不能通过
                Comp Never
                // 如果当前模板测试失败，虽然不能对当前模型进行渲染，但是对模板值进行更新了，将当前模型所处像素的模板值更新为1
                Fail Replace
            }
            
            Name "Crack01"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                
                // 在第二个Pass当中，将其Y值压为0
                v.positionOS.y = 0;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return 0;
            }
            
            ENDHLSL
        }
    }
}
