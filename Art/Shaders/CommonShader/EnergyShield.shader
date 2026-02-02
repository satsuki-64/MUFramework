Shader "MUFramework/EnergyShield"
{
    Properties
    {
        [Header(Base)]
        _BaseMap("BaseMap",2D) = "white"{}
        _BaseMapInstensity("BaseMapInstensity",Float) = 0.1
        // PowerSlider 用于控制 Range 的非线性的调整，而是指数调整，比如0~7占据了滑杆百分之80的空间
        [PowerSlider(3)]_FresnelInstensity("FresnelInstensity",Range(0,15)) = 8
        _FresnelColor("FresnelColor",Color) = (1,1,1,1)
        
        [Space]
        [Header(HighLight)]
        _HighLightFade("HighLightFade",Float) = 3
        _HighLightColor("HighLightColor",Color) = (1,1,1,1)
        
        [Space]
        [Header(Distort)]
        _Tiling("Distort Tiling",Float) = 4
        _Distort("Distort Instensity",Range(0,1)) = 0.4
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            //"IgnoreProjector" = "True"
        }
        LOD 100
        Blend SrcColor OneMinusSrcColor

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
                half3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float fogCoord : TEXCOORD1;
                float3 positionVS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                half3 viewWS : TEXCOORD4; // 世界坐标下，相机到物体表面的坐标
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half _BaseMapInstensity;
                half _HighLightFade;
                half4 _HighLightColor;
                half _FresnelInstensity;
                half4 _FresnelColor;
                half _Tiling;
                half _Distort;
            CBUFFER_END
            
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            TEXTURE2D(_CameraDepthTexture);SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_CameraOpaqueTexture);SAMPLER(sampler_CameraOpaqueTexture);

            Varyings vert (Attributes v)
            {
                Varyings o;
                
                // 为了得到观察空间坐标，先将其转化为世界空间
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                
                // 将顶点从世界空间转化为视图空间
                o.positionVS = TransformWorldToView(positionWS);
                
                // 将顶点从视图空间转化为齐次裁剪空间
                o.positionCS = TransformObjectToHClip(v.positionOS);
                // o.positionCS = TransformWViewToHClip(o.positionVS);
                
                // uv 的 zw 用来存储 baseMap 的 UV
                o.uv.zw = TRANSFORM_TEX(v.uv,_BaseMap);
                o.uv.xy = v.uv;
                
                o.fogCoord = ComputeFogFactor(o.positionCS.z);
                
                // 世界空间下的法线
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                
                // 世界坐标下，相机到物体表面的向量
                o.viewWS = _WorldSpaceCameraPos - positionWS;
                
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 完整颜色
                half4 color;

                // --------------- 蜂窝纹理 ----------------
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv.zw);
                color = baseMap * float(_BaseMapInstensity * abs(sin(_Time.y))) * _FresnelColor;
                color.rgb = MixFog(color.rgb,i.fogCoord);

                // --------------- 交界处高亮 ------------------
                // 屏幕空间 UV
                float2 screenUV = i.positionCS.xy / _ScreenParams.xy;
                // 采样深度图
                half4 depthMap = SAMPLE_TEXTURE2D(_CameraDepthTexture,sampler_CameraDepthTexture,screenUV);
                // 获取当前片段对应深度图中像素在观察空间下的Z值
                half depth = LinearEyeDepth(depthMap.r,_ZBufferParams);
                // 假设当前能量罩和其他物体相交，则当前能量罩上被相交 的部分的表面片元对应的深度值应该是更大的
                half4 hightLight = depth + i.positionVS.z;
                // 叠加高亮颜色和计算衰减
                hightLight *= _HighLightFade;
                half4 hightLightInvers = 1 - hightLight;
                hightLightInvers *= _HighLightColor;
                hightLightInvers = saturate(hightLightInvers);
                color += hightLightInvers;

                // --------------- 外发光 --------------------
                half3 N = normalize(i.normalWS);
                half3 V = normalize(i.viewWS);
                half fresnel = pow(max(0,1-abs(dot(N,V))),_FresnelInstensity);
                half4 fresnelColor = _FresnelColor * fresnel;
                color += fresnelColor;

                // --------------- 蜂窝纹理扭曲 ----------------
                // 当前帧的抓屏
                // 扭曲的UV，在屏幕空间的UV和baseMap作为扭曲UV之间插值
                half4 baseMap01 = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv.zw + float2(0,_Time.y));
                float2 distortUV = lerp(screenUV,baseMap01.rr,_Distort);
                half4 opaqueTex = SAMPLE_TEXTURE2D(_CameraOpaqueTexture,sampler_CameraOpaqueTexture,distortUV);
                half4 distort = half4(opaqueTex.xyz,1);
                half flowMask = frac(i.uv.y * _Tiling + _Time.y);
                distort *= flowMask;
                color += distort;
                
                return color; 
            }
            
            ENDHLSL
        }
    }
}