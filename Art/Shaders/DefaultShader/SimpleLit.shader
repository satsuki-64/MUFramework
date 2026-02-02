// 此 Shader 针对低端设备，不支持 PBR
Shader "Lit/Simple Lit 模板"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map (RGB) Smoothness / Alpha (A)", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        _Cutoff("Alpha Clipping", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _SpecGlossMap("Specular Map", 2D) = "white" {}
        _SmoothnessSource("Smoothness Source", Float) = 0.0
        _SpecularHighlights("Specular Highlights", Float) = 1.0

        [HideInInspector] _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        [NoScaleOffset]_EmissionMap("Emission Map", 2D) = "white" {}

        // Blending state
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
        [HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        // Editmode props
        _QueueOffset("Queue offset", Float) = 0.0

        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Shininess("Smoothness", Float) = 0.0
        [HideInInspector] _GlossinessSource("GlossinessSource", Float) = 0.0
        [HideInInspector] _SpecSource("SpecularHighlights", Float) = 0.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "SimpleLit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            
            // 灯光模式表示的是，此 Pass 在 URP 下的光照处理
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            // -------------------------------------
            // Render State Commands
            // Use same blending / depth states as Standard shader
            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _ _SPECGLOSSMAP _SPECULAR_COLOR
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            //--------------------------------------
            // Defines
            #define BUMP_SCALE_NOT_SUPPORTED 1

            // -------------------------------------
            // 顶点着色器和片段着色器的实现在 SimpleLitInput 和 SimpleLitForwardPass 文件当中
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitForwardPass.hlsl"

            struct Attributes1
            {
                float4 positionOS    : POSITION;
                float3 normalOS      : NORMAL;
                float4 tangentOS     : TANGENT;
                float2 texcoord      : TEXCOORD0;
                float2 staticLightmapUV    : TEXCOORD1;
                float2 dynamicLightmapUV    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings1
            {
                float2 uv                       : TEXCOORD0;

                float3 positionWS                  : TEXCOORD1;    // xyz: posWS

                #ifdef _NORMALMAP
                    half4 normalWS                 : TEXCOORD2;    // xyz: normal, w: viewDir.x
                    half4 tangentWS                : TEXCOORD3;    // xyz: tangent, w: viewDir.y
                    half4 bitangentWS              : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
                #else
                    half3  normalWS                : TEXCOORD2;
                #endif

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    half4 fogFactorAndVertexLight  : TEXCOORD5; // x: fogFactor, yzw: vertex light
                #else
                    half  fogFactor                 : TEXCOORD5;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord             : TEXCOORD6;
                #endif

                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);

                #ifdef DYNAMICLIGHTMAP_ON
                    float2  dynamicLightmapUV : TEXCOORD8; // Dynamic lightmap UVs
                #endif

                #ifdef USE_APV_PROBE_OCCLUSION
                    float4 probeOcclusion : TEXCOORD9;
                #endif

                float4 positionCS                  : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            float4 GetShadowCoord1(VertexPositionInputs vertexInput)
            {
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
                    return ComputeScreenPos(vertexInput.positionCS);
                #else
                    // 其只用到了顶点在世界空间下的坐标
                    return TransformWorldToShadowCoord(vertexInput.positionWS);
                #endif
            }
            
            Varyings1 vert(Attributes1 input)
            {
                Varyings1 output = (Varyings1)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // 计算顶点在世界空间下的坐标、观察空间下的坐标、齐次裁剪空间下的坐标、NDC下的坐标
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                // 计算顶点在世界空间下的法线、切线、副切线
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                #if defined(_FOG_FRAGMENT)
                    half fogFactor = 0;
                #else
                    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                #endif

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionWS.xyz = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;

                #ifdef _NORMALMAP
                    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
                    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
                    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
                #else
                    output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
                #endif

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);

                #ifdef DYNAMICLIGHTMAP_ON
                    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif

                OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
                    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                #else
                    output.fogFactor = fogFactor;
                #endif
                
                // 如果当前材质球上没有禁止接收阴影，并且管线上设置了主灯的阴影透射，同时阴影的级联阴影没有开启的情况下，当前的阴影在顶点者色器当中被计算
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = GetShadowCoord1(vertexInput);
                #endif

                return output;
            }
            
            // 如果仅仅只是想得到主灯的相关信息、不需要阴影相关信息时，可以调用这个 GetMainLight()
            Light GetMainLight1()
            {
                Light light;
                
                // 灯光的方向
                light.direction = half3(_MainLightPosition.xyz);
                
                #if USE_FORWARD_PLUS
                    #if defined(LIGHTMAP_ON) && defined(LIGHTMAP_SHADOW_MIXING)
                        light.distanceAttenuation = _MainLightColor.a;
                    #else
                        light.distanceAttenuation = 1.0;
                    #endif
                #else
                    light.distanceAttenuation = unity_LightData.z; // unity_LightData.z is 1 when not culled by the culling mask, otherwise 0.
                #endif
                
                // 在这里先对阴影的值初始化为 1
                light.shadowAttenuation = 1.0;
                
                // 获得主平行光的颜色
                light.color = _MainLightColor.rgb;

                // 获得当前灯光的 Layer 
                light.layerMask = _MainLightLayerMask;

                return light;
            }
            
            // 获取主光源阴影采样数据
            ShadowSamplingData GetMainLightShadowSamplingData1()
            {
                ShadowSamplingData shadowSamplingData;

                // 阴影偏移量：用于低质量软阴影的过滤采样
                // 这些偏移值在 SampleShadowmapFiltered 函数中使用，通过在不同位置进行多次采样来模拟软阴影效果
                // _MainLightShadowOffset0 和 _MainLightShadowOffset1 是 Unity 设置的全局变量
                shadowSamplingData.shadowOffset0 = half4(_MainLightShadowOffset0);
                shadowSamplingData.shadowOffset1 = half4(_MainLightShadowOffset1);

                // 阴影贴图尺寸：包含阴影贴图的宽度、高度和对应的倒数
                // 用于将世界空间坐标正确转换到阴影贴图的UV坐标
                // _MainLightShadowmapSize 通常包含四个分量：(xy: 1/width and 1/height, zw: width and height)
                shadowSamplingData.shadowmapSize = _MainLightShadowmapSize;

                // 软阴影质量：控制软阴影的采样质量和性能开销
                // 从_MainLightShadowParams的y分量获取，通常对应不同的软阴影滤波级别[2](@ref)
                // 不同的质量级别会影响采样次数和滤波核大小，需要在效果和性能间权衡[5](@ref)
                shadowSamplingData.softShadowQuality = half(_MainLightShadowParams.y);

                return shadowSamplingData;
            }

            // 采样 ShadowMap - 默认采样透视投影
            real SampleShadowmap1
            (
                    TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap),
                    float4 shadowCoord, 
                    ShadowSamplingData samplingData,
                    half4 shadowParams,
                    bool isPerspectiveProjection = true
            )
            {
                // ========== 透视投影处理 ==========
                // 根据投影类型决定是否进行透视除法：将裁剪空间坐标归一化到NDC空间[-1,1]或[0,1]
                if (isPerspectiveProjection)
                    shadowCoord.xyz /= shadowCoord.w; // 透视除法：将齐次坐标转换为三维坐标
                // SampleShadowmapFiltered();
                // attenuation 表示最终的阴影值，0=完全阴影, 1=无阴影
                real attenuation;

                // 阴影强度参数，用于控制阴影黑暗程度
                real shadowStrength = shadowParams.x; 

                // ========== 多质量级别软阴影采样 ==========
                // 根据不同的软阴影质量设置，采用不同的采样策略
                #if defined(_SHADOWS_SOFT_LOW)
                    // 低质量软阴影：使用简单的滤波方案（如2x2 PCF），性能最佳
                    attenuation = SampleShadowmapFilteredLowQuality(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
                
                #elif defined(_SHADOWS_SOFT_MEDIUM)
                    // 中等质量软阴影：平衡性能与效果（如4x4 PCF或优化滤波）
                    attenuation = SampleShadowmapFilteredMediumQuality(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
                
                #elif defined(_SHADOWS_SOFT_HIGH)
                    // 高质量软阴影：使用更复杂的滤波算法（如5x5 PCF或方差阴影），效果最好但性能开销最大
                    attenuation = SampleShadowmapFilteredHighQuality(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
                
                #elif defined(_SHADOWS_SOFT)
                    // 通用软阴影处理：根据shadowParams.y参数动态选择软阴影质量
                    if (shadowParams.y > SOFT_SHADOW_QUALITY_OFF) // 检查是否启用软阴影
                    {
                        // 使用可配置的滤波采样（可能根据硬件能力自适应）
                        attenuation = SampleShadowmapFiltered(TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), shadowCoord, samplingData);
                    }
                    else
                    {
                        // 硬阴影：单次采样，无滤波（性能最好，阴影边缘锐利）
                        attenuation = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz));
                    }
                
                #else
                    // 无软阴影：直接采样阴影贴图，使用硬件进行深度比较
                    // 这个 SAMPLE_TEXTURE2D_SHADOW方法 在不同平台有不同的定义，比如在 D3D11 下，其定义为：
                    // #define SAMPLE_TEXTURE2D_SHADOW(textureName, samplerName, coord3) textureName.SampleCmpLevelZero(samplerName, (coord3).xy, (coord3).z)
                    // 可以看到，在 SAMPLE_TEXTURE2D_SHADOW 当中使用了 textureName.SampleCmpLevelZero 方法
                    // SampleCmpLevelZero 方法表示比较然后采样，其使用 shadowCoord.xy 作为UV来对 shadowMap 进行采样然后采样得到的深度值、再去和shadowCoord.Z值进行比较
                    // 然后根据比较的结果，判断当前是否在阴影当中
                    attenuation = real(SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz));
                
                #endif

                // ========== 阴影强度应用 ==========
                // 将原始衰减值根据阴影强度进行插值：控制阴影的明暗程度
                // 由白色过渡到指定颜色，也可以直接通过 lerp 来实现
                // 例如：shadowStrength=0.5时，将衰减从[0,1]映射到[0.5,1]，使阴影变淡
                // attenuation = attenuation * shadowStrength;
                attenuation = LerpWhiteTo(attenuation, shadowStrength);

                // ========== 阴影边界检查 ==========
                // 检查当前阴影坐标是否超出光源的视锥体范围
                // 如果阴影坐标的z值是小于等于0、大于等于1，则阴影部分其原本的颜色，即无阴影
                // 超出范围的像素应返回1.0（无阴影），因为这些位置不受该光源的阴影影响
                // TODO: 此处可考虑使用分支指令来优化性能（在某些平台上）
                return BEYOND_SHADOW_FAR(shadowCoord) ? 1.0 : attenuation;
            }
            
            // 计算主光源的实时阴影衰减值
            half MainLightRealtimeShadow1(float4 shadowCoord, half4 shadowParams, ShadowSamplingData shadowSamplingData)
            {
                // 如果没有定义 MAIN_LIGHT_CALCULATE_SHADOWS 关键字，表示不计算主光源阴影，直接返回1.0，表示无阴影遮挡
                #if !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    return half(1.0);
                #endif

                // SSAO 阴影处理：当使用屏幕空间阴影且非透明表面时
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
                    // 采样屏幕空间阴影贴图 - 这种技术不需要传统的阴影映射
                    return SampleScreenSpaceShadowmap(shadowCoord);
                #else
                    // 传统阴影映射路径：使用标准阴影贴图采样
                    // TEXTURE2D_ARGS 宏展开阴影纹理和采样器参数
                    // 最后一个参数false表示不启用额外的功能开关
                    return SampleShadowmap1(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_LinearClampCompare), 
                                          shadowCoord, shadowSamplingData, shadowParams, false);
                #endif
            }
            
            // 计算主灯光的阴影 - 负责混合主光源的实时阴影与烘焙阴影的核心函数
            half MainLightShadow1(float4 shadowCoord, float3 positionWS, half4 shadowMask, half4 occlusionProbeChannels)
            {
                // 获取主光源的阴影参数（如强度、软硬阴影类型等）
                // x: shadowStrength 阴影强度, y: >= 1.0 if soft shadows, 0.0 otherwise 如果是软阴影则为 1，否则为 0
                // z: main light fade scale, w: main light fade bias
                half4 shadowParams = half4(_MainLightShadowParams);
                
                // 采样实时阴影贴图：将像素的阴影坐标与 ShadowMap 中的深度值进行比较
                // 返回的 realtimeShadow 值（0到1）表示该点被阴影遮挡的程度，0表示完全遮挡
                half realtimeShadow = MainLightRealtimeShadow1(shadowCoord, shadowParams, GetMainLightShadowSamplingData1());

                // 处理烘焙阴影：根据灯光烘焙模式（如Shadowmask或Subtractive）获取烘焙阴影值
                #ifdef CALCULATE_BAKED_SHADOWS
                    // 从阴影遮罩纹理或光照探针中提取烘焙阴影信息
                    half bakedShadow = BakedShadow(shadowMask, occlusionProbeChannels, shadowParams);
                #else
                    // 如果未启用烘焙阴影计算，则默认视为无阴影遮挡（值为1.0）
                    half bakedShadow = half(1.0);
                #endif

                // 计算阴影淡出因子：根据像素与世界空间中原点的距离，使阴影在指定范围内平滑淡出
                #ifdef MAIN_LIGHT_CALCULATE_SHADOWS
                    half shadowFade = GetMainLightShadowFade(positionWS);
                #else
                    // 如果未启用主光源阴影计算，则淡出因子为1.0（无淡出效果）
                    half shadowFade = half(1.0);
                #endif

                // 混合实时阴影和烘焙阴影：根据阴影淡出因子和当前烘焙模式策略进行混合
                return MixRealtimeAndBakedShadows(realtimeShadow, bakedShadow, shadowFade);
            }
            
            
            Light GetMainLight1(float4 shadowCoord, float3 positionWS, half4 shadowMask)
            {
                // 得到场景中的主灯光
                Light light = GetMainLight1();
                
                // 真正的计算主灯的阴影信息，在这个代码当中
                light.shadowAttenuation = MainLightShadow1(shadowCoord, positionWS, shadowMask, _MainLightOcclusionProbes);

                #if defined(_LIGHT_COOKIES)
                    real3 cookieColor = SampleMainLightCookie(positionWS);
                    light.color *= cookieColor;
                #endif

                return light;
            }
            
            Light GetMainLight1(InputData inputData, half4 shadowMask, AmbientOcclusionFactor aoFactor)
            {
                // 获取主灯的信息
                Light light = GetMainLight1(inputData.shadowCoord, inputData.positionWS, shadowMask);

                // 如果开启了 SSAO 效果
                #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
                if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_AMBIENT_OCCLUSION))
                {
                    light.color *= aoFactor.directAmbientOcclusion;
                }
                #endif

                return light;
            }
            
            half4 UniversalFragmentBlinnPhong1(InputData inputData, SurfaceData surfaceData)
            {
                #if defined(DEBUG_DISPLAY)
                    half4 debugColor;

                    if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
                    {
                        return debugColor;
                    }
                #endif

                uint meshRenderingLayers = GetMeshRenderingLayer();
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
                Light mainLight = GetMainLight1(inputData, shadowMask, aoFactor);

                MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);

                inputData.bakedGI *= surfaceData.albedo;

                LightingData lightingData = CreateLightingData(inputData, surfaceData);
                
                #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
                #endif
                {
                    lightingData.mainLightColor += CalculateBlinnPhong(mainLight, inputData, surfaceData);
                }

                // 如果额外光采用逐像素光照，进入以下代码
                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();

                    #if USE_FORWARD_PLUS
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

                        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                        #ifdef _LIGHT_LAYERS
                        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                        #endif
                        {
                            lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData);
                        }
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                    #ifdef _LIGHT_LAYERS
                        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                    #endif
                        {
                            lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData);
                        }
                    LIGHT_LOOP_END
                #endif

                // 如果额外光采用逐顶点光照，则进入以下计算
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    lightingData.vertexLightingColor += inputData.vertexLighting * surfaceData.albedo;
                #endif

                return CalculateFinalColor(lightingData, surfaceData.alpha);
            }
            
            void InitializeInputData1(Varyings1 input, half3 normalTS, out InputData inputData)
            {
                inputData = (InputData)0;

                inputData.positionWS = input.positionWS;
                #if defined(DEBUG_DISPLAY)
                    inputData.positionCS = input.positionCS;
                #endif

                #ifdef _NORMALMAP
                    half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
                    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
                    inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
                #else
                    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
                    inputData.normalWS = input.normalWS;
                #endif

                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                viewDirWS = SafeNormalize(viewDirWS);

                inputData.viewDirectionWS = viewDirWS;

                // 如果材质球上没有禁止接收阴影，并且管线上设置了主灯的阴影透射，同时阴影的级联阴影没有开启的情况下
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    // 则当前的阴影在顶点着色器当中被计算
                    inputData.shadowCoord = input.shadowCoord;
                // 如果不满足上述情况，但是定义了 主灯计算阴影，则在片段着色器当中执行 TransformWorldToShadowCoord 
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                #else
                    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactor);
                    inputData.vertexLighting = half3(0, 0, 0);
                #endif

                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                #if defined(DEBUG_DISPLAY)
                    #if defined(DYNAMICLIGHTMAP_ON)
                        inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
                    #endif
                
                    #if defined(LIGHTMAP_ON)
                        inputData.staticLightmapUV = input.staticLightmapUV;
                    #else
                        inputData.vertexSH = input.vertexSH;
                    #endif
                
                    #if defined(USE_APV_PROBE_OCCLUSION)
                        inputData.probeOcclusion = input.probeOcclusion;
                    #endif
                #endif
            }

            void InitializeBakedGIData1(Varyings1 input, inout InputData inputData)
            {
                #if defined(DYNAMICLIGHTMAP_ON)
                    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
                    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
                #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
                    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
                        GetAbsolutePositionWS(inputData.positionWS),
                        inputData.normalWS,
                        inputData.viewDirectionWS,
                        input.positionCS.xy,
                        input.probeOcclusion,
                        inputData.shadowMask);
                #else
                    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
                #endif
            }
            
            void frag(
                Varyings1 input
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out float4 outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData;
                InitializeSimpleLitSurfaceData(input.uv, surfaceData);

                #ifdef LOD_FADE_CROSSFADE
                    LODFadeCrossFade(input.positionCS);
                #endif

                InputData inputData;
                InitializeInputData1(input, surfaceData.normalTS, inputData);
                // outColor = inputData.shadowCoord;
                SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

                #if defined(_DBUFFER)
                    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
                #endif

                InitializeBakedGIData1(input, inputData);

                half4 color = UniversalFragmentBlinnPhong1(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

                outColor = color;

                #ifdef _WRITE_RENDERING_LAYERS
                    uint renderingLayers = GetMeshRenderingLayer();
                    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
                #endif
            }
            
            ENDHLSL
        }
        
        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            
            float3 ApplyShadowBias1(float3 positionWS, float3 normalWS, float3 lightDirection)
            {
                float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
                float scale = invNdotL * _ShadowBias.y;

                // normal bias is negative since we want to apply an inset normal offset
                positionWS = lightDirection * _ShadowBias.xxx + positionWS;
                positionWS = normalWS * scale.xxx + positionWS;
                return positionWS;
            }
            
            float4 GetShadowPositionHClip1(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias1(positionWS, normalWS, lightDirectionWS));
                positionCS = ApplyShadowClamping(positionCS);
                return positionCS;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                #if defined(_ALPHATEST_ON)
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                #endif

                output.positionCS = GetShadowPositionHClip1(input);
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(_ALPHATEST_ON)
                    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
                #endif

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                return 0;
            }
            
            ENDHLSL
        }

        // 延迟渲染当中的 GBuffer
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite[_ZWrite]
            ZTest LEqual
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex LitPassVertexSimple
            #pragma fragment LitPassFragmentSimple

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            //#pragma shader_feature _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _ _SPECGLOSSMAP _SPECULAR_COLOR
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            //--------------------------------------
            // Defines
            #define BUMP_SCALE_NOT_SUPPORTED 1

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitGBufferPass.hlsl"
            ENDHLSL
        }

        // DepthOnly
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // -------------------------------------
            // ZWrite 必须打开，不然不会进行深度写入、无法得到具有空间信息的效果
            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // 渲染 _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            // Universal Pipeline keywords
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitDepthNormalsPass.hlsl"
            ENDHLSL
        }

        // 不用于正常渲染，仅用于光照贴图烘培
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }

            // -------------------------------------
            // Render State Commands
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaSimple

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _SPECGLOSSMAP
            #pragma shader_feature EDITOR_VISUALIZATION

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitMetaPass.hlsl"

            ENDHLSL
        }

        // 2D 半透明
        Pass
        {
            Name "Universal2D"
            Tags
            {
                "LightMode" = "Universal2D"
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
            }

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Universal2D.hlsl"
            ENDHLSL
        }

        // MotionVectors
        Pass
        {
            Name "MotionVectors"
            Tags { "LightMode" = "MotionVectors" }
            ColorMask RG

            HLSLPROGRAM
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma shader_feature_local_vertex _ADD_PRECOMPUTED_VELOCITY

            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl"
            ENDHLSL
        }

        // XRMotionVectors
        Pass
        {
            Name "XRMotionVectors"
            Tags { "LightMode" = "XRMotionVectors" }
            ColorMask RGBA

            // Stencil write for obj motion pixels
            Stencil
            {
                WriteMask 1
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma shader_feature_local_vertex _ADD_PRECOMPUTED_VELOCITY
            #define APLICATION_SPACE_WARP_MOTION 1
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl"
            ENDHLSL
        }
    }

    Fallback  "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.SimpleLitShader"
}
