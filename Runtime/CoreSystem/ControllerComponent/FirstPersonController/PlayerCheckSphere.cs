using MUFramework.Utilities;
using UnityEngine;
using System.Collections.Generic;

namespace MUFramework.CoreSystem.FirstPersonController
{
    public class PlayerCheckSphere : MonoBehaviour
    {
        [HideInInspector]
        public FirstPersonController controller;
        
        // 使用HashSet来跟踪所有接触的Ground物体，避免多个Ground物体接触时的问题
        private HashSet<Collider> groundColliders = new HashSet<Collider>();
        
        private void OnTriggerEnter(Collider other)
        {
            if (controller != null && IsGroundObject(other))
            {
                groundColliders.Add(other);
                UpdateGroundedStatus();
                // Log.Debug("Ground Enter!!!", LogModule.GamePlay);
            }
        }
        
        private void OnTriggerStay(Collider other)
        {
            // 确保Stay时也更新状态，处理物体层变化等情况
            if (controller != null && IsGroundObject(other) && !groundColliders.Contains(other))
            {
                groundColliders.Add(other);
                UpdateGroundedStatus();
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (controller != null && groundColliders.Contains(other))
            {
                groundColliders.Remove(other);
                UpdateGroundedStatus();
                // Log.Debug("Ground Exit!!!", LogModule.GamePlay);
            }
        }
        
        private bool IsGroundObject(Collider other)
        {
            // 检查物体的Layer是否为Ground
            // 假设Ground层的索引为您项目中设置的对应索引
            return other.gameObject.layer == LayerMask.NameToLayer("Scene");
        }
        
        private void UpdateGroundedStatus()
        {
            bool wasGrounded = controller.movementController.isGrounded;
            bool isNowGrounded = groundColliders.Count > 0;
            
            controller.movementController.isGrounded = isNowGrounded;
            
            // 只在状态变化时打印日志，避免刷屏
            if (wasGrounded != isNowGrounded)
            {
                // Log.Debug($"状态变化: {wasGrounded} -> {isNowGrounded}", LogModule.GamePlay);
            }
        }
        
        // 可选：在脚本禁用或销毁时清理状态
        private void OnDisable()
        {
            if (controller != null)
            {
                groundColliders.Clear();
                controller.movementController.isGrounded = false;
            }
        }
    }
}