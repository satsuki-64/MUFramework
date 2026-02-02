Shader "MUFramework/Sequence"
{
    Properties
    {
        _BaseColor("BaseColor",Color) = (1,1,1,1)
        [NoScaleOffset]_BaseMap("BaseMap",2D) = "white"{}
        _Sequence("Row(X) Cloum(Y) Speed(UV) Speed(V)",Vector) = (4,4,1,1)
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcFactor("SrcFactor",Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]_DstFactor("DstFactor",Int) = 0
        [Enum(Biliboard,1,VerticalBillboard,0)]_BillboardType("BillboardType",Int) = 0
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "Queue" = "Transparent"
        }
        LOD 100
        Blend [_SrcFactor] [_DstFactor]

        Pass
        {
            Name "SimplesUnlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float3 testVertex : TEXCOORD1;
            };

			// 变量定义在 CBuffer 当中
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _Sequence;
            int _BillboardType;
            CBUFFER_END
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float4 _BaseMap_ST;

            Varyings vert (Attributes v)
            {
                Varyings o;
                
                // 当前是在计算模型的片元，因此当前 mul 是将其转换到片元的本地空间
                // 现在得到的 viewDir 向量，就是从物体到相机的向量，也可以立即为我们自己定义的变换后的坐标系下的 Z 方向基向量
                // 把相机从世界空间换转为本地空间，而本地空间就是以模型为原点的空间，此时相机转换后的位置就是需要的位置
                float3 _cameraPositionOS = mul(GetWorldToObjectMatrix(),float4(_WorldSpaceCameraPos,1)).xyz;
                float3 viewDir = normalize(v.positionOS.xyz-_cameraPositionOS);
                viewDir.y *= _BillboardType;
                
                // 假设向上的向量为世界坐标系下的上向量
                float3 upDir = float3(0,1,0);
                // 利用叉积(左手法则)计算出向右的向量
                float3 rightDir = normalize(cross(upDir,viewDir));
                // 再利用叉积计算出精确的向上向量
                upDir = normalize(cross(viewDir,rightDir));
                
                // float3x3 M_rotationToCamera = float3x3(
                //     rightDir,
                //     upDir,
                //     viewDir
                //     );
                
                // float3 newVertex = mul(v.positionOS.xyz,M_rotationToCamera);
                float3 newVertex = rightDir*v.positionOS.x + upDir*v.positionOS.y + viewDir*v.positionOS.z;
                o.testVertex = newVertex;
                
                // 在转换到齐次裁剪空间之前，就需要对物体进行旋转
                o.positionCS = TransformObjectToHClip(v.positionOS);
                // o.positionCS = TransformObjectToHClip(newVertex);
                
                // 设置初始 UV 的位置为左上角
                o.uv = float2(
                    v.uv.x/_Sequence.y,
                    v.uv.y/_Sequence.x
                    );

                // U 方向的走格
                o.uv.x += frac(floor(_Time.y*_Sequence.z)/_Sequence.y);

                // V 方向的走格
                o.uv.y -= frac(floor(_Time.y*_Sequence.z/_Sequence.y)/_Sequence.x);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 color;
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                color = baseMap * _BaseColor;
                
                return color;
            }
            
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue" = "Transparent"
        }
        LOD 100
        Blend [_SrcFactor] [_DstFactor]

        Pass
        {
            Name "SimplesUnlit"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _BaseMap; float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _Sequence;

            Varyings vert (Attributes v)
            {
                Varyings o;
                
                o.positionCS = UnityObjectToClipPos(v.positionOS);
                o.uv = float2(
                    v.uv.x/_Sequence.y,
                    v.uv.y/_Sequence.x
                    );
                o.uv.x += frac(floor(_Time.y*_Sequence.z)/_Sequence.y);
                o.uv.y -= frac(floor(_Time.y*_Sequence.z/_Sequence.y)/_Sequence.x);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 color;
                half4 baseMap = tex2D(_BaseMap,i.uv);
                color = baseMap * _BaseColor;
                
                return color;
            }
            
            ENDCG
        }
    }
}
