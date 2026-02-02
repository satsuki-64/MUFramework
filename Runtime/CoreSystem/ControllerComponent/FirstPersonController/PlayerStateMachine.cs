using UnityEngine;

namespace MUFramework.CoreSystem.FirstPersonController
{
    [System.Serializable]
    public enum PlayerState
    {
        Death = 0,
        Idle = 1,
        Walking = 2,
        Running = 3,
        Jumping = 4,
        Falling = 5,    // 坠落
        Crouching = 6,  // 半蹲
        Prone = 7,      // 趴下
        Slide = 8,       // 滑铲
    }

    public class PlayerStateMachine
    {
        public MovementController movementController;
        
        public void PlayerInput(ref PlayerState playerState)
        {
            // 跳跃
            if (playerState == PlayerState.Idle || playerState == PlayerState.Walking ||  playerState == PlayerState.Running)
            {
                if (Input.GetKeyDown(KeyCode.Space) && playerState != PlayerState.Jumping && movementController.isGrounded)
                {
                    playerState = PlayerState.Jumping;
                    movementController.isJump = true;
                }
            }
            
            // 如果当前按下 WASD。并且不处于下坠、滑铲、跳跃时
            if (BasicMoveImput() && playerState != PlayerState.Falling && playerState != PlayerState.Slide && playerState != PlayerState.Jumping)
            {
                playerState = PlayerState.Walking;

                if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && movementController.isGrounded == true)
                {
                    playerState = PlayerState.Running;
                }

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.C))
                {
                    playerState = PlayerState.Crouching;
                }

                if (Input.GetKey(KeyCode.Z))
                {
                    playerState = PlayerState.Prone;
                }
                
            }
            // 如果当前没按下 WASD
            else if (playerState != PlayerState.Slide && playerState != PlayerState.Jumping && playerState != PlayerState.Falling)
            {
                playerState = PlayerState.Idle;
                
                // 可以趴下或者下蹲
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.C))
                {
                    playerState = PlayerState.Crouching;
                }
                if (Input.GetKey(KeyCode.Z))
                {
                    playerState = PlayerState.Prone;
                }
            }

            SlideInput(ref playerState);
        }

        private bool BasicMoveImput()
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||  Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SlideInput(ref PlayerState playerState)
        {
            // 如果当前在奔跑状态，则可以进入一次滑铲
            if (playerState == PlayerState.Running &&
                (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            {
                playerState = PlayerState.Slide;
            }

            if (Input.GetKeyDown(KeyCode.S) && playerState == PlayerState.Slide)
            {
                playerState = PlayerState.Walking;
            }
        }
    }
}