using UnityEngine;
using System.Collections;
using MUFramework.Utilities;

namespace MUFramework.CoreSystem.RTSController
{
    [RequireComponent(typeof(Camera))]
    public class RTSController : MonoBehaviour
    {
        [Header("Move Setting")]
        [SerializeField] private float movementSpeed = 10f;
        [SerializeField] private float sprintMultiplier = 2f;
        [SerializeField] private float edgeMovementSpeed = 15f;
        [SerializeField] private float edgeThreshold = 20f;
        [SerializeField] private float verticalMoveSpeed = 8f;
        [SerializeField] private float movementSmoothTime = 0.1f;
        [SerializeField] private float verticalSmoothTime = 0.2f;
        private Vector3 targetPosition;
        private Vector3 positionVelocity = Vector3.zero;
        private float targetVerticalPosition;
        private float verticalVelocity = 0f;
        private bool isRotating = false;
        [Tooltip("控制边缘移动的开关")] 
        private bool edgeMovementEnabled = true; 
        
        [Space]
        [Header("Rotate Setting")]
        [SerializeField] private float rotateSpeed = 80f;
        [SerializeField] private float minVerticalAngle = 10f;
        [SerializeField] private float maxVerticalAngle = 80f;
        [SerializeField] private float rotationSmoothTime = 0.2f;
        private Quaternion targetRotation;
        private Vector3 rotationVelocity;
        
        [Space]
        [Header("Focus Setting")]
        [SerializeField] private float focusSmoothTime = 0.5f;
        [SerializeField] private float focusHeightOffset = 5f;
        [SerializeField] private Transform lockedTarget;
        private Vector3 focusVelocity;
        private bool isLockedOnTarget = false;
        
        [Space]
        [Header("Obstacle Setting")] 
        public LayerMask obstacleMask = 1;
        public float maxHeight = 50f;
        public float minHeight = 0f;
        [Tooltip("相机和物体检测避障的最大距离")]
        [SerializeField] private float obstacleCheckDistance = 5f;
        [SerializeField] private float obstacleCheckDownDistance = 10f;
        [SerializeField] private float obstacleAvoidSpeed = 5f;
        [SerializeField] private float maxObstacleOffset = 2f;
        [SerializeField] private Vector3 obstacleOffset = Vector3.zero;
        
        private Camera cam;
        private Vector3 defaultEulerAngles;
        
        private void Start()
        {
            if (cam == null)
            {
                cam = gameObject.GetComponent<Camera>();

                if (cam == null)
                {
                    Log.Warning($"{gameObject.name} 控制器的Camera错误");
                }
            }
            
            defaultEulerAngles = transform.eulerAngles;
            targetRotation = transform.rotation;
            targetPosition = transform.position;
            targetVerticalPosition = transform.position.y;
        }

        private void Update()
        {
            HandleObstacleAvoidance();
            
            if (!isLockedOnTarget)
            {
                HandleRotationInput();
                HandleMovementInput();
            }
            else
            {
               // HandleTargetFollowing();
            }
            
            // HandleFocus();
            ApplySmoothMovement();
            ApplySmoothRotation();
        }

        #region 避障处理

        /// <summary>
        /// 障碍物回避系统
        /// </summary>
        private void HandleObstacleAvoidance()
        {
            obstacleOffset = Vector3.zero;
            bool hasObstacle = false;

            // 1. 优先检测下方障碍物（地面碰撞）
            RaycastHit downwardHit;
            Vector3 downwardRayStart = transform.position;
    
            if (Physics.Raycast(downwardRayStart, Vector3.down, out downwardHit, 
                    obstacleCheckDownDistance, obstacleMask))
            {
                // 检测到下方障碍物，需要向上避障
                float avoidDistance = Mathf.Abs(obstacleCheckDownDistance - downwardHit.distance);
                Vector3 upwardAvoid = Vector3.up * avoidDistance * obstacleAvoidSpeed * Time.deltaTime;
        
                targetVerticalPosition += upwardAvoid.y;
                hasObstacle = true;
                
                Log.Debug($"当前向上避障：{avoidDistance}", LogModule.GamePlay);
            }
            
            // 2. 多方向水平障碍物检测
            Vector3[] checkDirections = new Vector3[]
            {
                Vector3.forward, Vector3.back,
                Vector3.right, Vector3.left,
                (Vector3.forward + Vector3.right).normalized,
                (Vector3.forward + Vector3.left).normalized,
                (Vector3.back + Vector3.right).normalized,
                (Vector3.back + Vector3.left).normalized
            };
            foreach (Vector3 worldDirection in checkDirections)
            {
                // 获得当前相机物体的多方向
                Vector3 cameraOrientedDirection = transform.TransformDirection(worldDirection);
                
                RaycastHit hit;

                if (Physics.Raycast(transform.position, cameraOrientedDirection, out hit, 
                        obstacleCheckDistance, obstacleMask))
                {
                    // 水平方向避障
                    float avoidDistance = obstacleCheckDistance - hit.distance;
                    Log.Debug($"当前避障距离：{avoidDistance}", LogModule.GamePlay);
                    Vector3 avoidDirection = hit.normal;
                    
                    // 计算当前用于避障的偏移量大小
                    obstacleOffset += avoidDirection * avoidDistance * obstacleAvoidSpeed * Time.deltaTime;
                    hasObstacle = true;
                }
            }

            // 3. 应用水平方向避障
            if (hasObstacle)
            {
                targetPosition += obstacleOffset;
            }
        }

        #endregion
        
        
        #region 旋转与运动处理
        
        /// <summary>
        /// 旋转输入处理
        /// </summary>
        private void HandleRotationInput()
        {
            if (Input.GetMouseButtonDown(2))
            {
                isRotating = true;
                edgeMovementEnabled = false; // 禁用边缘移动
            }
            
            if (Input.GetMouseButton(2))
            {
                float mouseX = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
                float mouseY = Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;

                Vector3 newEuler = targetRotation.eulerAngles;
                newEuler.y += mouseX;
                newEuler.x -= mouseY;
                newEuler.x = Mathf.Clamp(newEuler.x, minVerticalAngle, maxVerticalAngle);
                
                targetRotation = Quaternion.Euler(newEuler);
            }
            
            if (Input.GetMouseButtonUp(2))
            {
                isRotating = false;
                StartCoroutine(EnableObstacleAvoidanceAfterDelay(0.5f));
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                targetRotation = Quaternion.Euler(defaultEulerAngles);
            }
        }

        /// <summary>
        /// 延迟重新启用避障系统
        /// </summary>
        private IEnumerator EnableObstacleAvoidanceAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            edgeMovementEnabled = true; // 重新启用边缘移动
        }

        /// <summary>
        /// 修改后的移动输入处理 - 修复边缘移动在旋转时的禁用问题
        /// </summary>
        private void HandleMovementInput()
        {
            Vector3 movement = Vector3.zero;
            float currentSpeed = movementSpeed;

            // Shift 加速功能
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                currentSpeed *= sprintMultiplier;
            }

            // WASD 移动
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            if (horizontal != 0 || vertical != 0)
            {
                Vector3 cameraForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 cameraRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                
                Vector3 rightMovement = cameraRight * horizontal * currentSpeed * Time.deltaTime;
                Vector3 forwardMovement = cameraForward * vertical * currentSpeed * Time.deltaTime;
                
                movement += rightMovement + forwardMovement;
            }

            // 屏幕边缘移动，只在非旋转状态下可用
            if (edgeMovementEnabled && !isRotating)
            {
                Vector3 mousePosition = Input.mousePosition;
                Vector3 cameraForwardFlat = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 cameraRightFlat = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                
                if (mousePosition.x >= Screen.width - edgeThreshold)
                    movement += cameraRightFlat * edgeMovementSpeed * Time.deltaTime;
                if (mousePosition.x <= edgeThreshold)
                    movement -= cameraRightFlat * edgeMovementSpeed * Time.deltaTime;
                if (mousePosition.y >= Screen.height - edgeThreshold)
                    movement += cameraForwardFlat * edgeMovementSpeed * Time.deltaTime;
                if (mousePosition.y <= edgeThreshold)
                    movement -= cameraForwardFlat * edgeMovementSpeed * Time.deltaTime;
            }

            // 应用水平移动
            if (movement != Vector3.zero)
            {
                targetPosition += movement;
            }

            // 鼠标滚轮垂直移动，只限制最高高度
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                targetVerticalPosition += scroll * verticalMoveSpeed;
                
                // 限制最低和最高点
                targetVerticalPosition = Mathf.Clamp(targetVerticalPosition, minHeight, maxHeight);
            }
        }
        
        private void ApplySmoothMovement()
        {
            Vector3 newPosition = Vector3.SmoothDamp(transform.position, targetPosition, 
                ref positionVelocity, movementSmoothTime);
            
            float newY = Mathf.SmoothDamp(transform.position.y, targetVerticalPosition, 
                ref verticalVelocity, verticalSmoothTime);
            newPosition.y = newY;
            
            transform.position = newPosition;
        }

        private void ApplySmoothRotation()
        {
            if (!isLockedOnTarget)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    Time.deltaTime / rotationSmoothTime);
            }
        }
        
        #endregion


        #region 物体跟随处理

        private void HandleFocus()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                GameObject selectedObject = GetSelectedObject();
                if (selectedObject != null)
                {
                    Vector3 targetFocusPosition = selectedObject.transform.position;
                    targetFocusPosition.y = targetVerticalPosition;
                    StartCoroutine(SmoothFocus(targetFocusPosition));
                }
            }

            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.F))
            {
                GameObject selectedObject = GetSelectedObject();
                if (selectedObject != null)
                {
                    lockedTarget = selectedObject.transform;
                    isLockedOnTarget = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isLockedOnTarget = false;
                lockedTarget = null;
            }
        }

        private void HandleTargetFollowing()
        {
            if (lockedTarget != null)
            {
                Vector3 targetFollowPosition = lockedTarget.position;
                targetFollowPosition.y = targetVerticalPosition;
                
                targetPosition = Vector3.SmoothDamp(targetPosition, targetFollowPosition, 
                    ref focusVelocity, focusSmoothTime);
            }
        }

        private GameObject GetSelectedObject()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit))
                {
                    return hit.collider.gameObject;
                }
            }
            return null;
        }

        private IEnumerator SmoothFocus(Vector3 targetPosition)
        {
            float elapsedTime = 0f;
            Vector3 startPosition = this.targetPosition;
            
            while (elapsedTime < focusSmoothTime)
            {
                this.targetPosition = Vector3.Lerp(startPosition, targetPosition, 
                    elapsedTime / focusSmoothTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            this.targetPosition = targetPosition;
        }

        #endregion
        
        
        
    }
}