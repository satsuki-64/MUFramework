Shader "MUFramework/Water"
{
    Properties
    {
        [Header(Base)]
        _Speed("Speed",Range(0,0.1)) = 0.015
        _Alpha("Alpha",Range(0,1)) = 0.8
        
        [Header(Color)]
        _WaterDepth("WaterDepth",Float) = 0.8
        _SpecularInstensity("SpecularInstensity",Float) = 38.8
        _SpecularSmoothness("SpecularSmoothness",Float) = 6.85
        _NormalTex("NormalTex",2D) = "white"{}
        _NormalInstensity("NormalInstensity",Range(0,1)) = 0.8
        
        [Header(Foam)]
        _FoamTex("FoamTex",2D) = "white"{}
        _FoamRange("FoamRange",Range(0,3)) = 2.5
        _FoamColor("FoamColor",Color) = (1,1,1,1)
        _FoamNoise("FoamNoise",Range(0,1)) = 0.6
        
        [Header(Distort)]
        _Distort("Distort",Range(0,0.1)) = 0.01
        _DistortTex("DistortTex",2D) = "white"{}
        
        [Header(Reflection)]
        _ReflectionTex("ReflectionTex",Cube) = "white"{}
        _ReflectionInstensity("ReflectionInstensity",Range(0,2.5)) = 0.5
        
        [Header(Caustic)]
        _CausticTex("CausticTex",2D) = "white"{}
        _CausticScale("CausticScale",Range(0,1)) = 0.15
        _CausticScaleY("CausticScaleY",Range(0,1)) = 0.2
        _CausticInstensity("CausticInstensity",Range(0,1)) = 0.1
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
        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Water"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0; // xy：foamUV
                float fogCoord : TEXCOORD1;
                float3 positonVS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float3 normalWS : TEXCOORD4;
                float4 normalUV : TEXCOORD5;
            };

            CBUFFER_START(UnityPerMaterial)
                half _Alpha;
                half _WaterDepth;
                half4 _SpecularColor;
                half _SpecularInstensity;
                half _SpecularSmoothness;
                half _FoamRange;
                half4 _FoamColor;
                half _Speed;
                half _FoamNoise;
                half _Distort;
                half _NormalInstensity;
                half _ReflectionInstensity;
                half _CausticScale;
                half _CausticScaleY;
                half _CausticInstensity;
            CBUFFER_END

            TEXTURE2D(_CameraDepthTexture);SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_FoamTex);SAMPLER(sampler_FoamTex);float4 _FoamTex_ST;
            TEXTURE2D(_CameraOpaqueTexture);SAMPLER(sampler_CameraOpaqueTexture);
            TEXTURE2D(_DistortTex);SAMPLER(sampler_DistortTex);float4 _DistortTex_ST;
            TEXTURE2D(_NormalTex);SAMPLER(sampler_NormalTex);float4 _NormalTex_ST;
            TEXTURE2D(_CausticTex);SAMPLER(sampler_CausticTex);
            TEXTURE2D(_RampTexture01);SAMPLER(sampler_RampTexture01);
            TEXTURE2D(_RampTexture02);SAMPLER(sampler_RampTexture02);
            TEXTURECUBE(_ReflectionTex);SAMPLER(sampler_ReflectionTex);

            Varyings vert (Attributes v)
            {
                Varyings o;

                // 将顶点从本地坐标转换为世界坐标，然后再转换为观察空间
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionWS = positionWS;
                o.positonVS = TransformWorldToView(positionWS);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);

                // UV 采样
                float speed = _Time.y * _Speed;
                o.uv.xy = o.positionWS.xz * _FoamTex_ST.xy + speed;
                o.uv.zw = TRANSFORM_TEX(v.uv,_FoamTex) + speed;
                o.normalUV.xy = TRANSFORM_TEX(v.uv,_NormalTex) + speed * float2(1,1); 
                o.normalUV.zw = TRANSFORM_TEX(v.uv,_NormalTex) + speed * float2(-1.07,1.3);
                o.uv.zw = TRANSFORM_TEX(v.uv,_DistortTex) + speed;
                
                // 法线与雾效
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fogCoord = ComputeFogFactor(o.positionCS.z);

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float4 Color;
                
                // 屏幕空间下的 UV 坐标
                float2 screenUV = i.positionCS.xy / _ScreenParams.xy;
                
                // --------------------- 水的深度 ------------------------
                // 1. 获取场景中物体的深度
                half depthTex = SAMPLE_TEXTURE2D(_CameraDepthTexture,sampler_CameraDepthTexture,screenUV).r;
                half depthScene = LinearEyeDepth(depthTex,_ZBufferParams); // 物体的转换到观察空间下的Z值
                 
                // 2. 计算物体本身的观察空间下的Z值，即物体在水下的深度值
                half depthWater = depthScene + i.positonVS.z;
                depthWater *= _WaterDepth;

                // --------------------- 水的颜色 ------------------------
                half4 waterColor = SAMPLE_TEXTURE2D(_RampTexture01,sampler_RampTexture01,depthWater);

                // --------------------- 水的高光 ------------------------
                // 1. 采样法线图，使得水面看起来凹凸起伏
                half4 normalTex01 = SAMPLE_TEXTURE2D(_NormalTex,sampler_NormalTex,i.normalUV.xy);
                half4 normalTex02 = SAMPLE_TEXTURE2D(_NormalTex,sampler_NormalTex,i.normalUV.zw);
                half4 normalTex = normalTex01 * normalTex02;
                // 2. 使用 Blinne-Phone 模型计算高光
                half3 N = lerp(normalize(i.normalWS),normalTex.xyz,_NormalInstensity);
                Light light = GetMainLight();
                half4 lightColor = float4(light.color,0.5);
                half3 L = light.direction; // 光照的方向
                half3 V = normalize(_WorldSpaceCameraPos.xyz - i.positionWS.xyz); // 顶点指向摄像机的方向
                half3 H = normalize(L + V);
                half NdotH = dot(N,H);
                // 3. 计算高光颜色
                half4 specular = lightColor * _SpecularInstensity * pow(saturate(NdotH),_SpecularSmoothness);

                // --------------------- 水的反射 ------------------------
                half3 reflectionUV = reflect(V,N);
                half4 encodedColor = SAMPLE_TEXTURECUBE(_ReflectionTex,sampler_ReflectionTex,reflectionUV);
                half fresnel = pow(1-saturate(dot(N,V)),1/_ReflectionInstensity);
                half4 reflection = encodedColor * fresnel;
                
                // --------------------- 水的泡沫 ------------------------
                half foamRange = depthWater* _FoamRange;
                half foamTex = SAMPLE_TEXTURE2D(_FoamTex,sampler_FoamTex,i.uv.xy).r;
                foamTex *= pow(abs(foamTex),_FoamNoise);
                half foamMask = step(foamRange,foamTex);
                half4 foam = foamMask * _FoamColor;

                
                // --------------------- 水的扭区 ------------------------
                // 1. 计算扭区 UV，_Distort=0 时使用屏幕空间 UV
                half4 distortValue = SAMPLE_TEXTURE2D(_DistortTex,sampler_DistortTex,i.uv.zw);
                float2 distortUV = screenUV + distortValue.xy*_Distort;
                float2 opaqueUV;
                // 2. 由于只使用抓屏、无法区分哪些地方该扭区、哪些地方不该扭曲，因此需要利用上当前的深度值信息，通过深度值、计算出来哪些部分在水面以下
                half depthDistortTex = SAMPLE_TEXTURE2D(_CameraDepthTexture,sampler_CameraDepthTexture,distortUV).r;
                half depthDistortScene = LinearEyeDepth(depthDistortTex,_ZBufferParams);
                half depthDistortWater = depthDistortScene + i.positonVS.z;
                // 3. 如果 depthDistortWater < 0 则代表当前面片对应的物体的“水深”小于0，代表其位于水面之上，因此其不进行扭区
                if (depthDistortWater < 0)
                {
                    opaqueUV = screenUV;
                }
                else
                {
                    opaqueUV = distortUV;
                }
                half4 opaqueTex = SAMPLE_TEXTURE2D(_CameraOpaqueTexture,sampler_CameraOpaqueTexture,opaqueUV);
                
                // --------------------- 水的焦散 ------------------------
                // 1. 观察空间下的深度点
                float4 depthVS = 1;
                depthVS.xy = i.positonVS.xy * depthDistortScene / - i.positonVS.z;
                depthVS.z = depthDistortScene;
                float3 depthWS = mul(unity_CameraToWorld,depthVS).xyz;
                // 2. 计算U和V方向上流动的UV
                float2 causticUV01 = depthWS.xz * _CausticScale + depthWS.y * _CausticScaleY + _Time.y * _Speed;
                float2 causticUV02 = depthWS.xz * _CausticScale * 0.86 + depthWS.y * _CausticScaleY + _Time.y * _Speed * float2(-1.62,1.21);
                // 3. 计算焦散颜色，并使用 min 增强焦散的偏移效果
                half4 causticTex01 = SAMPLE_TEXTURE2D(_CausticTex,sampler_CausticTex,causticUV01);
                half4 causticTex02 = SAMPLE_TEXTURE2D(_CausticTex,sampler_CausticTex,causticUV02);
                half4 caustic = min(causticTex01,causticTex02) * _CausticInstensity;
                
                // --------------------- 效果叠加 ------------------------
                Color = opaqueTex * waterColor;
                Color += caustic;
                Color += specular;
                Color += reflection;
                Color += foam;
                Color.a = _Alpha;
                return Color;
            }
            
            ENDHLSL
        }
    }
}