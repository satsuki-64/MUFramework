Shader "MUFramework/Grid"
{
    Properties
    {
        _Repeat("Repeat",Float) = 5
        _MaskOffset("MaskIntensity",Float) = 0.55
        _Color("Color",Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CullMode)]_Cull("Cull Mode",Int) = 0
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
            
        }
        
        LOD 100
        Cull [_Cull]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            // URP渲染管线下通常都要引用的hlsl文件
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 vertexOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertexOS : TEXCOORD1;
                float4 vertexCS : SV_POSITION;
                float fogCoord : TEXCOORD3;
            };

			// 变量定义在 CBuffer 当中
            CBUFFER_START(UnityPerMaterial)
            half _Repeat;
            float _MaskOffset;
            half4 _Color;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.vertexCS = TransformObjectToHClip(v.vertexOS);
                o.vertexOS = v.vertexOS;
                o.uv = v.uv * _Repeat;
                
                o.fogCoord = ComputeFogFactor(o.vertexCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 color;
                
                float2 uv = floor(i.uv*2)*0.5;
                float checker = frac(uv.x + uv.y)*2;
                half mask = i.vertexOS.y + _MaskOffset;
                color = checker * mask;
                color *= _Color;
                
                color.rgb = MixFog(color,i.fogCoord);
                return color;
            }
            
            ENDHLSL
        }
    }
}