using IGDC.InputManager;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Cinemachine;

namespace IGDC.PlayerController
{
    public class Player_Controller : MonoBehaviour
    {
        [SerializeField] private float animBlendSpeed = 8.9f;

        [SerializeField] private Transform cameraFollow;
        [SerializeField] private Transform cameraMain;
        [SerializeField] private Transform headPosition;

        [SerializeField] private float upperLimit = -40f;
        [SerializeField] private float bottomLimit = 70f;
        [SerializeField] private float mouseSensitivity = 21.9f;

        [SerializeField, Range(10, 500)] private float jumpFactor = 260f;
        [SerializeField] private float disToGround = 0.8f;
        [SerializeField] private LayerMask groundLayer;

        [SerializeField] private float airResistance = 0.8f;

        [SerializeField] private CinemachineVirtualCamera firstPersonVcam;
        [SerializeField] private CinemachineVirtualCamera thirdPersonVcam;

        private Rigidbody rigidBody;
        private Input_Manager inputManager;
        private Animator animator;

        private bool hasAnimator;
        private int xVelHash;
        private int yVelHash;

        private int jumpHash;
        private int fallingHash;
        private int groundedHash;
        private int zVelHash;
        private int crouchHash;

        private bool isGrounded;

        private const float walkSpeed = 2f;
        private const float runSpeed = 6f;

        private Vector2 currentVelocity;

        private float xRoation;


        private void Start()
        {
            hasAnimator = TryGetComponent<Animator>(out animator);
            rigidBody = GetComponent<Rigidbody>();
            inputManager = GetComponent<Input_Manager>();

            xVelHash = Animator.StringToHash("x_velocity");
            yVelHash = Animator.StringToHash("y_velocity");

            jumpHash = Animator.StringToHash("Jump");
            fallingHash = Animator.StringToHash("Falling");
            groundedHash = Animator.StringToHash("Grounded");
            zVelHash = Animator.StringToHash("z_Velocity");

            crouchHash = Animator.StringToHash("Crouch");


        }

        private void Update()
        {
            changePOV();

        }

        private void FixedUpdate()
        {
            detectGround();
            move();
            handleJump();
            handleCrouch();

        }

        private void LateUpdate()
        {
            cameraMovement();

        }

        private void move()
        {
            if (!hasAnimator) return;

            float targetSpeed = inputManager.Run ? runSpeed : walkSpeed;

            if (inputManager.Crouch)
            {
                targetSpeed = 1.5f;
            }

            if (inputManager.Move == Vector2.zero)
            {
                targetSpeed = 0f;

            }

            if (isGrounded)
            {
                currentVelocity.x = Mathf.Lerp(currentVelocity.x, inputManager.Move.x * targetSpeed, animBlendSpeed * Time.fixedDeltaTime);
                currentVelocity.y = Mathf.Lerp(currentVelocity.y, inputManager.Move.y * targetSpeed, animBlendSpeed * Time.fixedDeltaTime);

                var xVelDifference = currentVelocity.x - rigidBody.velocity.x;
                var zVelDifference = currentVelocity.y - rigidBody.velocity.z;

                rigidBody.AddForce(transform.TransformVector(new Vector3(xVelDifference, 0, zVelDifference)), ForceMode.VelocityChange);

            }
            else
            {
                rigidBody.AddForce(transform.TransformVector(new Vector3(currentVelocity.x * airResistance, 0, currentVelocity.y * airResistance)), ForceMode.VelocityChange);
            }
           

            animator.SetFloat(xVelHash, currentVelocity.x);
            animator.SetFloat(yVelHash, currentVelocity.y);
        }


        private void cameraMovement()
        {
            if (!hasAnimator) return;

            var mouseX = inputManager.Look.x;
            var mouseY = inputManager.Look.y;

            cameraFollow.position = headPosition.position;
            cameraMain.position = cameraFollow.position; // Set Main Camera Position to Virtual Camera Parent
            

            xRoation -= mouseY * mouseSensitivity * Time.smoothDeltaTime;
            xRoation = Mathf.Clamp(xRoation, upperLimit, bottomLimit);

            cameraFollow.localRotation = Quaternion.Euler(xRoation, 0, 0);
            //transform.Rotate(Vector3.up, mouseX * mouseSensitivity * Time.deltaTime);
            rigidBody.MoveRotation(rigidBody.rotation * Quaternion.Euler(0, mouseX * mouseSensitivity * Time.smoothDeltaTime, 0));

        }

        private void handleJump()
        {
            if (!hasAnimator) return;
            if (!inputManager.Jump) return;
            if (!isGrounded) return;

            animator.SetTrigger(jumpHash);

        }

        public void jumpAddForce()
        {

            rigidBody.AddForce(-rigidBody.velocity.y * Vector3.up, ForceMode.VelocityChange);
            rigidBody.AddForce(Vector3.up * jumpFactor, ForceMode.Impulse);
            animator.ResetTrigger(jumpHash);

        }

        private void detectGround()
        {
            if (!hasAnimator) return;

            RaycastHit hitInfo;
            if (Physics.Raycast(rigidBody.worldCenterOfMass, Vector3.down, out hitInfo, disToGround + 0.1f, groundLayer))
            {
                // Grounded
                // Collided with Something

                isGrounded = true;
                setAnimationGrounding();
                return;

            }

            // Falling Currently

            isGrounded = false;
            animator.SetFloat(zVelHash, rigidBody.velocity.y);
            setAnimationGrounding();
            Debug.Log("Grounded");
            return;

        }

        private void setAnimationGrounding()
        {
            animator.SetBool(fallingHash, !isGrounded);
            animator.SetBool(groundedHash, isGrounded);
        }

        private void changePOV()
        {
            if (!hasAnimator) return;
            if (!inputManager.camChanged) return;
            
            if ( firstPersonVcam.Priority > thirdPersonVcam.Priority )
            {
                thirdPersonVcam.Priority += 30;

            }
            else
            {
                thirdPersonVcam.Priority -= 30;
            }


        }

        private void handleCrouch()
        {
            if (!hasAnimator) return;
            animator.SetBool(crouchHash, inputManager.Crouch);

        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 direction = transform.TransformDirection(Vector3.down) * (disToGround + 0.1f);
            Gizmos.DrawRay(transform.position, direction);
        }

    }

}
