using System;
using MUFramework.Utilities;
using UnityEngine;

namespace MUFramework.CoreSystem.FirstPersonController
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public Camera camera;
        public PlayerState playerState = PlayerState.Idle;
        public PlayerCheckSphere playerCheckSphere;
        public MovementController movementController;
        public CameraController cameraController;
        private PlayerStateMachine playerStateMachine;
        private CharacterController characterController;
        
        private void Awake()
        {
            if (characterController == null)
            {
                gameObject.AddComponent<CharacterController>();
                characterController = GetComponent<CharacterController>();
            }
            
            playerStateMachine = new PlayerStateMachine();
            movementController = new MovementController(camera, characterController,this,playerStateMachine);
            playerStateMachine.movementController = movementController;
            cameraController = new CameraController(camera);

            if (playerCheckSphere == null)
            {
                Log.Warning($"请检查{gameObject.name}身上的PlayerCheckSphere是否设置正确！", LogModule.GamePlay);
            }
            else
            {
                playerCheckSphere.controller = this;
            }
        }

        public void Update()
        {
            playerStateMachine.PlayerInput(ref playerState);
            movementController.UpdateMovement();
            cameraController.UpdateCamera(playerState);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                cameraController.LockCursor();
            }
            else
            {
                cameraController.UnlockCursor();
            }
        }
    }
}