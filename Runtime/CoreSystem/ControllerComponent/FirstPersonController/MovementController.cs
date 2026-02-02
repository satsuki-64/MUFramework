using System;
using UnityEngine;

namespace MUFramework.CoreSystem.FirstPersonController
{
    [System.Serializable]
    public class MovementController
    {
        private Camera camera;
        private CharacterController controller;
        private FirstPersonController fpsController;
        private PlayerStateMachine playerStateMachine;

        public MovementController(Camera camera, CharacterController controller, FirstPersonController fpsController,PlayerStateMachine playerStateMachine)
        {
            this.camera = camera;
            this.controller = controller;
            this.fpsController = fpsController;
            this.playerStateMachine = playerStateMachine;
        }
        
        [Header("Basic Move Info")]
        public Vector3 moveDirection;
        public Vector3 velocity;
        
        public bool isGrounded;
        
        [Space]
        [Header("MoveSpeed Setting")]
        public float targetSpeed;
        public float currentSpeedWithDirection; 
        public float walkSpeed = 5f;
        public float runSpeed = 8f;
        public float crouchSpeed = 3f;
        public float proneSpeed = 1.5f;
        // 加速度曲线参数
        public float acceleration = 100f;
        private Vector3 currentVelocity;
        
        [Space]
        [Header("Jump And Gravity Setting")]
        public float gravity = -30f;
        public float maxGravityVelocity = -15f;
        public float jumpHeight = 1.3f;
        public float JumpTime = 0.5f;
        public bool isJump = false;
        private float currentJumpTime = 0f;
        private float lastFrameSpeed = 0;
        private PlayerState lastFrameState =  PlayerState.Idle;
        
        [Space]
        [Header("Sliding Setting")]
        [Tooltip("一次滑铲的持续时间")]
        public float OneSlidingTime = 1.5f;
        [Tooltip("滑铲的衰减加速度")]
        public float decelerationSlide = 30f;
        public float SlidingSpeed = 35f;
        public float SlidingSpeedDecelerationPow = 4;
        public float MinSlidingRate = 0.2f;
        private bool IsSlide = false;
        private float tempTime = 0f;

        #region 重力与跳跃处理
        
        /// <summary>
        /// 重力与跳跃处理
        /// </summary>
        private void HandleGravity()
        {
            if (fpsController.playerState == PlayerState.Jumping)
            {
                if (isJump == true)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    currentJumpTime = 0;
                    isJump = false;
                }

                currentJumpTime += Time.deltaTime;
                
                // 应用重力（在空中时）
                velocity.y += gravity * Time.deltaTime;

                if (currentJumpTime > JumpTime) 
                {
                    fpsController.playerState = PlayerState.Falling;
                    currentJumpTime = 0;
                }
            }
            else
            {
                // 地面检测和重力应用
                if (isGrounded)
                {
                    // 如果是从空中落地，将状态改为Idle
                    if (fpsController.playerState == PlayerState.Falling)
                    {
                        fpsController.playerState = PlayerState.Idle;
                        playerStateMachine.PlayerInput(ref fpsController.playerState);
                    }

                    velocity.y = -2f;
                }
                else
                {
                    // 应用重力（在空中时）
                    velocity.y += gravity * Time.deltaTime;

                    if (velocity.y < -3)
                    {
                        fpsController.playerState = PlayerState.Falling;
                    }

                    // 限制最大下坠速度
                    if (velocity.y <= maxGravityVelocity)
                    {
                        velocity.y = maxGravityVelocity;
                    }
                }
            }
        }
        
        #endregion

        #region 移动处理

        /// <summary>
        /// 移动处理
        /// </summary>
        private void HandleMovement()
        {
            // 获取输入
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            // 根据摄像机方向计算移动方向
            // 相机前向方向
            Vector3 cameraForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            // 相机右方向
            Vector3 cameraRight = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
            
            // 综合计算得到目标方向
            if (fpsController.playerState == PlayerState.Slide)
            {
                // 滑铲时不能往反方向滑铲
                if (vertical < 0)
                {
                    vertical = 0;
                }
            }

            Vector3 targetDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

            if (fpsController.playerState == PlayerState.Slide)
            {
                targetDirection = cameraForward;
            }

            // 如果没有输入，目标速度为0
            if (targetDirection.magnitude < 0.1f)
            {
                targetDirection = Vector3.zero;
            }
            
            if (fpsController.playerState == PlayerState.Running)
            {
                if (vertical < 0)
                {
                    fpsController.playerState = PlayerState.Walking;   
                }
            }
            
            
            // 如果当前为跳跃或者下坠，直接使用上一帧的速度
            if (fpsController.playerState == PlayerState.Jumping || fpsController.playerState == PlayerState.Falling)
            {
                targetSpeed = lastFrameSpeed;
            }
            else
            {
                // 正常情况时，使用当前状态下的定义速度
                targetSpeed = GetTargetSpeed();
                lastFrameSpeed =  targetSpeed;
            }

            float accelerationTemp = acceleration;
            
            // 滑铲处理
            if (fpsController.playerState == PlayerState.Slide)
            {
                if (tempTime < OneSlidingTime)
                {
                    tempTime += Time.deltaTime;
                    accelerationTemp = decelerationSlide;
                    
                    // 更改滑铲时的速度，在 OneSlidingTime 内逐渐减小
                    targetSpeed = MathF.Max(Mathf.Pow(
                        (1 - tempTime / OneSlidingTime),
                        SlidingSpeedDecelerationPow), MinSlidingRate) * targetSpeed;
                }
                else
                {
                    fpsController.playerState = PlayerState.Idle;
                    tempTime = 0;
                }
            }

            // 移动向量 = 方向 * 速度
            Vector3 targetVelocity = targetDirection * targetSpeed;
            currentSpeedWithDirection = MathF.Abs(targetVelocity.magnitude);
            
            // 应用加速度曲线计算当前速度
            if (targetVelocity.magnitude > 0.1f || fpsController.playerState == PlayerState.Slide)
            {
                // 加速过程
                currentVelocity = Vector3.MoveTowards
                (
                    currentVelocity,
                    targetVelocity,
                    accelerationTemp * Time.deltaTime
                );
            }
            else
            {
                // 如果当前速度很小，则立刻减速
                currentVelocity = Vector3.zero;
            }
            
            moveDirection = currentVelocity;
        }
        
        private float GetTargetSpeed()
        {
            return fpsController.playerState switch
            {
                PlayerState.Running => runSpeed,
                PlayerState.Crouching => crouchSpeed,
                PlayerState.Prone => proneSpeed,
                PlayerState.Slide => SlidingSpeed, // 滑铲时速度更快
                _ => walkSpeed
            };
        }
        
        #endregion

        #region 应用运动状态
        
        private void ApplyFinalMovement()
        {
            // 应用水平移动
            Vector3 horizontalMovement = moveDirection * Time.deltaTime;
            
            // 组合水平和垂直移动
            Vector3 finalMovement = horizontalMovement + Vector3.up * velocity.y * Time.deltaTime;
            
            // 使用 CharacterController 移动
            controller.Move(finalMovement);
        }
        
        public void UpdateMovement()
        {
            HandleGravity();
            HandleMovement();
            ApplyFinalMovement();
        }
        
        #endregion
        
    }
}