Shader "MUFramework/DepthDecal"
{
    Properties
    {
        _BaseColor("BaseColor",Color) = (1,1,1,1)
        _BaseMap("BaseMap",2D) = "white"{}
        _RotateSpeed("RotateSpeed",Range(0,10)) = 1
    }
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100
       

        Pass
        {
            Blend SrcColor OneMinusSrcColor 
            Name "DepthDecal"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 positionVS : TEXCOORD2; // 顶点再观察空间下的坐标
            };

			// 变量定义在 CBuffer 当中
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half _ThreadHold;
                half _RotateSpeed;
            CBUFFER_END
            
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            TEXTURE2D(_CameraDepthTexture);SAMPLER(sampler_CameraDepthTexture);

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv,_BaseMap);
                o.positionOS = v.positionOS;
                o.positionVS = TransformWorldToView(TransformObjectToWorld(v.positionOS));
                
                
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 思路：
                // 1. 通过深度图，求出像素所在的观察空间中的Z值
                // 2. 再通过当前渲染的面片，求出其在观察空间下的坐标
                // 3. 通过以上两者，求出深度图中像素的XYZ坐标
                // 4. 再将此坐标转换到面片模型的本地空间，将XY当作UV来进行纹理采样
                // 屏幕空间下的坐标
                float2 screenUV = i.positionCS.xy/_ScreenParams.xy;
                half depthMap = SAMPLE_TEXTURE2D(_CameraDepthTexture,sampler_CameraDepthTexture,screenUV);
                half depthZ = LinearEyeDepth(depthMap,_ZBufferParams);
                
                // 深度图上的每个点，在观察空间下的坐标
                // 构建深度图上的像素在观察空间下的坐标
                float4 depthVS = 1; // depthVS.w = 1;
                depthVS.z = depthZ;
                depthVS.xy = i.positionVS.xy * depthZ / (-i.positionVS.z);

                // 求出深度图上的像素在世界空间下的坐标
                float3 depthWS = mul(unity_CameraToWorld,depthVS);
                float3 depthOS = mul(unity_WorldToObject,float4(depthWS,1));

                float angle = _Time.y * 0.1 * _RotateSpeed;
                float4x4 M_rotationY = float4x4(
	                cos(angle),0,sin(angle),0,
	                0,1,0,0,
	                -sin(angle),0,cos(angle),0,
	                0,0,0,1
	            );
	            depthOS = mul(M_rotationY,depthOS);
                
                float2 worldUV = depthOS.xz + 0.5;
                
                half4 color;
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,worldUV);
                color = baseMap * _BaseColor * 2;
                
                return color;
            }
            
            ENDHLSL
        }
    }
}