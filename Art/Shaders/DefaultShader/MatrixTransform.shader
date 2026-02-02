Shader "Unlit/MatrixTransform"
{
    Properties
    {
        [Header(Transform)]
        _Translate("Translate(XYZ)",Vector) = (0,0,0,0)
        _Scale("Scale(XYZ) Global(W)",Vector) = (1,1,1,1)
        _Angle("Angle",Float) = 0
        _Rotation("Rotation(XYZ)",Vector) = (0,0,0,0)
        
        [Header(View)]
        _ViewPos("View Pos",Vector) = (0,0,0,0)
        _ViewTarget("View Target",Vector) = (0,0,0,0)
        
        [Header(Clip)]
        [Enum(OthoGraphic,0,Perspective,1)]_CameraType("CameraType",Float) = 1
        _CameraParam("CameraParam Size(X),Near(Y),Far(Z),Ratio(W)",Vector) = (0,0,0,1.77)
        
        [Header(Other)]
        _MainTex("MainTex",2D) = "white"{}
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP渲染管线下通常都要引用的hlsl文件
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
            };

			// 变量定义在 CBuffer 当中
            CBUFFER_START(UnityPerMaterial)
            half4 _Translate;
            half4 _Scale;
            half _Angle;
            half4 _Rotation;
            half4 _ViewPos;
            half4 _ViewTarget;
            half4 _CameraParam;
            half _CameraType;
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.uv = v.uv;

                // 平移矩阵
                float4x4 T = float4x4(
                    1,0,0,_Translate.x,
                    0,1,0,_Translate.y,
                    0,0,1,_Translate.z,
                    0,0,0,1
                    );

                // 应用平移矩阵
                v.positionOS = mul(T,v.positionOS);

                // 缩放变换
                float4x4 M_Scale = float4x4(
                    _Scale.x*_Scale.w,0,0,0,
                    0,_Scale.y*_Scale.w,0,0,
                    0,0,_Scale.z*_Scale.w,0,
                    0,0,0,1
                    );  
                
                v.positionOS = mul(M_Scale,v.positionOS);

                // 二维旋转变换 - 沿着Z轴旋转
                float2x2 M_rotation = float2x2(
                    cos(_Angle),sin(_Angle),
                    -sin(_Angle),-cos(_Angle)
                    );
                v.positionOS.xy = mul(M_rotation,v.positionOS.xy);

                // 三维旋转变换 - 沿着X、Y、Z轴旋转
                float4x4 M_rotationX = float4x4(
                    1,0,0,0,
                    0,cos(_Rotation.x),sin(_Rotation.x),0,
                    0,-sin(_Rotation.x),cos(_Rotation.x),0,
                    0,0,0,1
                    );
                float4x4 M_rotationY = float4x4(
                    cos(_Rotation.y),0,sin(_Rotation.y),0,
                    0,1,0,0,
                    -sin(_Rotation.y),0,cos(_Rotation.y),0,
                    0,0,0,1
                    );
                float4x4 M_rotationZ = float4x4(
                    cos(_Rotation.z),sin(_Rotation.z),0,0,
                    -sin(_Rotation.z),cos(_Rotation.z),0,0,
                    0,0,1,0,
                    0,0,0,1
                    );
                v.positionOS = mul(M_rotationZ,mul(M_rotationY,mul(M_rotationX,v.positionOS)));

                // 视图空间变换
                // P_view = [W_view] * P_world = [V_world]^{-1} * P_world = [V_world]^{T} * P_world
                // 1. 构建相机的 右方向（X）、上方向（Y）、前方向（Z）
                float3 ViewZ = normalize(_ViewPos - _ViewTarget);
                float3 ViewY = float3(0,1,0);
                float3 ViewX = cross(ViewZ,ViewY);
                ViewY = cross(ViewX,ViewZ);
                // 以下是转置之前的
                // float3x3 V_world = float3x3(
                //     ViewX.x,ViewY.x,ViewZ.x,
                //     ViewX.y,ViewY.y,ViewZ.y,
                //     ViewX.z,ViewY.z,ViewZ.z
                //     );
                // 以下是转置之后，并且补充维度
                float4x4 V_world_T = float4x4(
                    ViewX.x,ViewX.y,ViewX.z,0,
                    ViewY.x,ViewY.y,ViewY.z,0,
                    ViewZ.x,ViewZ.y,ViewZ.z,0,
                    0,0,0,1
                );
                // 将相机变换为原点的矩阵
                float4x4 V_viewTranslate = float4x4(
                    1,0,0,-_ViewPos.x,
                    0,1,0,-_ViewPos.y,
                    0,0,1,-_ViewPos.z,
                    0,0,0,1
                    );
                float4x4 M_view = mul(V_world_T,V_viewTranslate);

                // 模型本地空间 -> 模型世界空间
                float3 positionWS = TransformObjectToWorld(v.positionOS);

                // 模型世界空间 -> 模型观察空间
                // Unity提供：float3 positionVS = TransformWorldToView(positionWS);
                float3 positionVS = mul(M_view,float4(positionWS,1));
                
                // 模型观察空间 -> 齐次裁剪空间
                // o.positionCS = TransformWViewToHClip(positionVS);
                // 相机参数
                // H：相机的宽度，等于 Clip 面的 Size 的两倍
                float h = _CameraParam.x * 2;
                float r = _CameraParam.w;
                float w = h*r;
                float n = _CameraParam.y;
                float f = _CameraParam.z;

                // 正交矩阵
                float4x4 M_clipOrth;

                // 投影矩阵
                float4x4 M_clipPerspective;
                
                if (UNITY_NEAR_CLIP_VALUE == -1)
                {
                    // 正交相机投影矩阵：P_clip = [V_clip] * P_view; (OpenGL下，-1~1的范围)
                    M_clipOrth = float4x4( 
                        2/w,0,0,0,
                        0,2/h,0,0,
                        0,0,2/(n-f),(n+f)/(n-f),
                        0,0,0,1
                        );

                    M_clipPerspective = float4x4(
                        2*n/w,0,0,0,
                        0,2*n/h,0,0,
                        0,0,(n+f)/(n-f),(2*n*f)/(n-f),
                        0,0,-1,0
                        );
                }

                if (UNITY_NEAR_CLIP_VALUE == 1)
                {
                    // 正交相机投影矩阵：P_clip = [V_clip] * P_view;
                    // DirectX下，范围为 1->0，即反向方向
                    // 并且DX平台和OpenGL的Y值是反过来的，所以Y取负值
                    M_clipOrth = float4x4(
                        2/w,0,0,0,
                        0,-2/h,0,0,
                        0,0,1/(f-n),f/(f-n),
                        0,0,0,1
                        );

                    // 注意再DX平台下，Y值要反过来，因此是 -2*n/h
                    M_clipPerspective = float4x4(
                        2*n/w,0,0,0,
                        0,-2*n/h,0,0,
                        0,0,n/(f-n),n*f/(f-n),
                        0,0,-1,0
                        );
                }

                float4x4 M_clip = _CameraType ? M_clipPerspective : M_clipOrth;
                
                // 正交投影：o.positionCS = mul(M_clipOrth,float4(positionVS,1));
                o.positionCS = mul(M_clip,float4(positionVS,1));
                
                // o.vertexCS = TransformObjectToHClip(v.positionOS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                // return _CameraType ? half4(1,0,0,0) : half4(0,1,0,0);
                return 1;
            }
            
            ENDHLSL
        }
    }
}