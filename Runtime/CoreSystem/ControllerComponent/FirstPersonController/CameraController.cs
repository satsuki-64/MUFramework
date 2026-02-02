using MUFramework.Utilities;
using UnityEngine;

namespace MUFramework.CoreSystem.FirstPersonController
{
    [System.Serializable]
    public class CameraController
    {
        [Header("Camera Setting")]
        // 视角限制
        public float minVerticalAngle = -89f;
        public float maxVerticalAngle = 89f;
        public float mouseSensitivity = 2f;
        private Camera camera;
        private Transform cameraTransform;
        private float xRotation = 0f;
        private float yRotation = 0f;
        
        [Space]
        [Header("Bob Setting")]
        public bool IsBobing = true;
        // 头部晃动参数
        [Tooltip("晃动频率")]
        public float bobFrequencyWalking = 8f;
        public float bobFrequencyRunning = 12f;
        [Tooltip("晃动幅度大小")]
        public float bobAmplitudeWalking = 0.3f;
        public float bobAmplitudeRunning = 0.6f;
        public float bobSmoothness = 2f;
        private float bobTimer = 0f;
        private Vector3 originalCameraPosition;
        private Vector3 currentBobOffset = Vector3.zero;
        
        [Space]
        [Header("Camera Height Setting")]
        public float standingHeight = 1.7f;
        public float crouchingHeight = 1.0f;
        public float proneHeight = 0.3f;
        public float slideHeight = 0.5f;
        public float heightChangeSpeed = 10f;
        private float targetHeight;
        private float currentHeight;
        
        [Space]
        [Header("FOV Setting")]
        [Tooltip("默认视野角度")]
        public float defaultFOV = 60f;
        [Tooltip("奔跑时的视野角度")]
        public float runningFOV = 70f;
        [Tooltip("滑铲时的视野角度")]
        public float SlideFOV = 80f;
        [Tooltip("滑铲时的视野角度")]
        public float CrouchingFOV = 58f;
        [Tooltip("滑铲时的视野角度")]
        public float ProneFOV = 55f;
        [Tooltip("FOV 变化速度")]
        public float fovChangeSpeed = 55f;
        [Tooltip("进入奔跑状态一定时间后再开始FOV变化")]
        public float fovChangeTimeWithRunning = 0.2f;
        [Tooltip("FOV 变化曲线 - 控制变化过程")]
        public AnimationCurve fovChangeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField]
        private float targetFOV;
        [SerializeField]
        private float currentFOV;
        private bool IsRunning = false;
        private float currentIsRunningFovChangeTime = 0f;
        
        [Space]
        [Header("Lean Setting")]
        [Tooltip("Q/E最大倾斜角度")]
        public float leanAngle = 15f;
        public float leanAngleWalking = 1.5f;
        public float leanAngleRunning = 2f;
        [Tooltip("水平偏移量")]
        public float leanOffsetAmount = 0.5f;
        public float leanOffsetAmountMove = 0.3f;
        public float leanOffsetAmountWalking = 0.2f;
        [Tooltip("倾斜平滑速度")]
        public float leanSmoothness = 3f;
        private KeyCode leanLeftKey = KeyCode.Q;
        private KeyCode leanRightKey = KeyCode.E;
        private float currentLeanAngle = 0f;
        private float targetLeanAngle = 0f;
        private float currentLeanOffset = 0f; // 当前水平偏移量
        private float targetLeanOffset = 0f; // 目标水平偏移量
        
        // 状态跟踪
        private PlayerState previousState;
        private bool wasRunning = false;
        
        public CameraController(Camera camera)
        {
            this.camera = camera;
            this.cameraTransform = camera.transform;
            
            // 初始化相机位置和参数
            originalCameraPosition = cameraTransform.localPosition;
            currentHeight = standingHeight;
            targetHeight = standingHeight;
            
            // 初始化 FOV
            currentFOV = defaultFOV;
            targetFOV = defaultFOV;
            camera.fieldOfView = currentFOV;
            
            // 锁定鼠标
            LockCursor();
        }

        #region 鼠标视角控制

        /// <summary>
        /// 处理鼠标控制角色旋转
        /// </summary>
        private void HandleMouseLook()
        {
            // 获取鼠标输入
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            // 垂直视角旋转（上下看）
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);
            
            // 水平视角旋转（左右看）- 旋转父物体来保持移动方向正确
            yRotation += mouseX;
            
            // 应用旋转
            if (cameraTransform.parent != null)
            {
                // 水平旋转应用于父物体（角色）
                cameraTransform.parent.rotation = Quaternion.Euler(0f, yRotation, 0f);
                // 垂直旋转应用于相机本身
                cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }
            else
            {
                // 如果没有父物体，直接旋转相机
                cameraTransform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
            }
        }

        #endregion

        #region 相机晃动与FOV设置
        
        /// <summary>
        /// 处理 FOV 动态调整
        /// </summary>
        /// <param name="currentState">当前玩家状态</param>
        private void HandleFOVChange(PlayerState currentState)
        {
            // 根据状态确定目标 FOV
            switch (currentState)
            {
                case PlayerState.Running:
                    if (IsRunning == false)
                    {
                        IsRunning = true;
                        currentIsRunningFovChangeTime = 0f;
                    } 
                    else if (IsRunning)
                    {
                        currentIsRunningFovChangeTime += Time.deltaTime;
                    }

                    if (currentIsRunningFovChangeTime > fovChangeTimeWithRunning)
                    {
                        targetFOV = runningFOV; 
                    }
                    break;
                case PlayerState.Crouching:
                    targetFOV = CrouchingFOV;
                    break;
                case PlayerState.Prone:
                    targetFOV = ProneFOV;
                    break;
                case PlayerState.Jumping:
                    targetFOV = currentFOV;
                    break;
                case PlayerState.Falling:
                    targetFOV = currentFOV;
                    break;
                case PlayerState.Slide:
                    targetFOV = SlideFOV; 
                    break;
                default:
                    targetFOV = defaultFOV; // 其他状态使用默认 FOV
                    IsRunning = false;
                    break;
            }
            
            // currentFOV = Mathf.Clamp(currentFOV, defaultFOV, SlideFOV);
            
            if (targetFOV > currentFOV)
            {
                currentFOV += Time.deltaTime * fovChangeSpeed;
                
                if (currentFOV >= targetFOV)
                {
                    currentFOV = targetFOV;
                }
            }
            else if (targetFOV < currentFOV)
            {
                currentFOV -= Time.deltaTime * fovChangeSpeed;
                
                if (currentFOV <= targetFOV)
                {
                    currentFOV = targetFOV;
                }
            }

            // 应用 FOV 变化到相机
            camera.fieldOfView = currentFOV;
            
            // if (currentState != PlayerState.Jumping)
            // {
            //     if (targetFOV > currentFOV)
            //     {
            //         fovChangeTime += Time.deltaTime;
            //     }
            //     else if (targetFOV > defaultFOV)
            //     {
            //         
            //     }
            //     else if(targetFOV <= currentFOV)
            //     {
            //         fovChangeTime -= Time.deltaTime;
            //     }
            //
            //     if (fovChangeTime > fovChangeSpeed)
            //     {
            //         fovChangeTime = fovChangeSpeed;
            //     }
            //     else if (fovChangeTime <= 0 )
            //     {
            //         fovChangeTime = 0;
            //     }
            //     
            //     float progress = fovChangeCurve.Evaluate(fovChangeTime / fovChangeSpeed);
            //     Log.Debug($"当前Progress：{progress}", LogModule.GamePlay);
            //
            //     // 加速当中
            //     if (targetFOV > currentFOV)
            //     {
            //         currentFOV = Mathf.Lerp(currentFOV, targetFOV, progress);
            //     }
            //     // 减速当中
            //     else if (targetFOV <= currentFOV)
            //     {
            //         currentFOV = Mathf.Lerp(currentFOV, targetFOV, 1 - progress);
            //     }
            //     
            //     // 应用 FOV 变化到相机
            //     camera.fieldOfView = currentFOV;
            // }
        }

        /// <summary>
        /// 处理相机摇晃
        /// </summary>
        /// <param name="currentState"></param>
        private void HandleHeadBob(PlayerState currentState)
        {
            float bobFrequency = 0;
            float bobAmplitude = 0;

            switch (currentState)
            {
                case PlayerState.Running:
                    bobFrequency = bobFrequencyRunning;
                    bobAmplitude = bobAmplitudeRunning;
                    break;
                case PlayerState.Walking:
                    bobFrequency = bobFrequencyWalking;
                    bobAmplitude = bobAmplitudeWalking;
                    break;
            }

            // 只有在奔跑且移动时才有头部晃动
            if (currentState == PlayerState.Running || currentState == PlayerState.Walking) 
            {
                // 计算晃动偏移
                bobTimer += Time.deltaTime * bobFrequency;
                float horizontalBob = Mathf.Sin(bobTimer) * bobAmplitude;
                float verticalBob = (Mathf.Cos(bobTimer * 2) * bobAmplitude) * 0.5f;
                
                Vector3 targetBobOffset = new Vector3(horizontalBob * 0.5f, verticalBob, horizontalBob);
                currentBobOffset = Vector3.Lerp(currentBobOffset, targetBobOffset, 
                    bobSmoothness * Time.deltaTime);
                
                wasRunning = true;
            }
            else if (wasRunning)
            {
                // 平滑停止头部晃动
                currentBobOffset = Vector3.zero;
                wasRunning = false;
            }
        }
        
        #endregion

        #region 相机倾斜
        
        /// <summary>
        /// 处理角色倾斜功能
        /// </summary>
        /// <param name="currentState">当前玩家状态</param>
        private void HandleLeaning(PlayerState currentState)
        {
            // 只有在Idle状态且按下Shift键时才允许倾斜
            if (currentState == PlayerState.Idle && (Input.GetKey(leanLeftKey) || Input.GetKey(leanRightKey)) 
                                                 && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                // 检测 Q 键或 E 键的按下事件
                if (Input.GetKey(leanLeftKey))
                {
                    targetLeanAngle = leanAngle; // 向左倾斜
                    targetLeanOffset = -leanOffsetAmount; // 向左偏移
                }
                else if (Input.GetKey(leanRightKey))
                {
                    targetLeanAngle = -leanAngle; // 向右倾斜
                    targetLeanOffset = leanOffsetAmount; // 向右偏移
                }
            }
            else if (currentState == PlayerState.Running && Input.GetKey(KeyCode.W) && (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)))
            {
                // 检测 Q 键或 E 键的按下事件
                if (Input.GetKey(KeyCode.A))
                {
                    targetLeanAngle = leanAngleRunning; // 向左倾斜
                    targetLeanOffset = -leanOffsetAmountMove; // 向左偏移
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    targetLeanAngle = -leanAngleRunning; // 向右倾斜
                    targetLeanOffset = leanOffsetAmountMove; // 向右偏移
                }
            }
            else if (currentState == PlayerState.Walking && Input.GetKey(KeyCode.W) && (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)))
            {
                // 检测 Q 键或 E 键的按下事件
                if (Input.GetKey(KeyCode.A))
                {
                    targetLeanAngle = leanAngleWalking; // 向左倾斜
                    targetLeanOffset = -leanOffsetAmountWalking; // 向左偏移
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    targetLeanAngle = -leanAngleWalking; // 向右倾斜
                    targetLeanOffset = leanOffsetAmountWalking; // 向右偏移
                }
            }
            else
            {
                targetLeanAngle = 0f;
                targetLeanOffset = 0f;
            }

            // 使用Lerp平滑过渡倾斜角度和偏移量
            currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLeanAngle, leanSmoothness * Time.deltaTime);
            currentLeanOffset = Mathf.Lerp(currentLeanOffset, targetLeanOffset, leanSmoothness * Time.deltaTime);
        }

        /// <summary>
        /// 应用倾斜效果到相机旋转和位置
        /// </summary>
        private void ApplyLeanEffect()
        {
            // 应用旋转效果：获取相机当前的本地旋转，在Z轴应用倾斜角度
            Vector3 localRotation = cameraTransform.localEulerAngles;
            localRotation.z = currentLeanAngle;
            cameraTransform.localEulerAngles = localRotation;
        }

        #endregion
        
        #region 相机最终计算与更新
        
        /// <summary>
        /// 得到当前相机的高度
        /// </summary>
        /// <param name="currentState">角色状态</param>
        private void HandlePostureChange(PlayerState currentState)
        {
            // 根据状态确定目标高度
            switch (currentState)
            {
                case PlayerState.Crouching:
                    targetHeight = crouchingHeight;
                    break;
                case PlayerState.Prone:
                    targetHeight = proneHeight;
                    break;
                case PlayerState.Slide:
                    targetHeight = slideHeight; // 滑铲时使用蹲下高度
                    break;
                default:
                    targetHeight = standingHeight;
                    break;
            }
            
            // 平滑过渡到目标高度
            if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
            {
                currentHeight = Mathf.Lerp(currentHeight, targetHeight, 
                    heightChangeSpeed * Time.deltaTime);
            }
            else
            {
                currentHeight = targetHeight;
            }
        }
        
        /// <summary>
        /// 更新相机位置（包含倾斜偏移）
        /// </summary>
        private void UpdateCameraPosition()
        {
            // 计算倾斜偏移向量：使用相机的右方向（X轴）乘以偏移量
            Vector3 leanOffset = Vector3.right * currentLeanOffset;
    
            // 计算最终相机位置：原始位置 + 高度调整 + 倾斜偏移 (+ 头部晃动)
            Vector3 targetPosition = originalCameraPosition + 
                                     Vector3.up * (currentHeight - standingHeight) + 
                                     leanOffset;
            if (IsBobing)
            {
                targetPosition += currentBobOffset;
            }

            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, 
                targetPosition, 8f * Time.deltaTime);
        }
        
        public void UpdateCamera(PlayerState currentState)
        {
            HandleMouseLook();
            HandlePostureChange(currentState);

            if (IsBobing)
            {
                // 处理视角晃动
                HandleHeadBob(currentState);
            }
            
            // FOV 动态调整（在相机位置更新前处理）
            HandleFOVChange(currentState);
            
            // 相机倾斜处理
            HandleLeaning(currentState);
            ApplyLeanEffect();
            
            // 更新相机位置
            UpdateCameraPosition();
            
            previousState = currentState;
        }

        #endregion

        #region 其他

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        #endregion
        
    }
}