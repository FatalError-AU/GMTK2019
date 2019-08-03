using Rewired;
using UnityEngine;
using static Controls.Action;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        public static Rewired.Player Input { get; private set; }

        public Transform cameraAxis;

        public float playerSpeed = 25F;
        public float cameraSpeed = 25F;

        public float cameraPitchBoundHigh;
        public float cameraPitchBoundLow;
        public float pickupRadius = 4F;

        public Transform lanternAnchor;

        private float pitch;
        private float yaw;

        private float yVelocity;

        private CharacterController controller;

        private Transform lantern;
        private Rigidbody lanternRb;
        private bool lanternHeld;

        private void Awake()
        {
            Input = ReInput.players.GetPlayer(Controls.Player.MAIN_PLAYER);

            controller = GetComponent<CharacterController>();

            lantern = GameObject.FindWithTag("Lantern").transform;
            lanternRb = lantern.GetComponent<Rigidbody>();
        }

        private void Start()
        {
            pitch = cameraAxis.transform.localEulerAngles.x;
            yaw = transform.localEulerAngles.y;
        }

        private void Update()
        {
            Vector3 movement = Input.GetAxis2D(MOVE_HORIZONTAL, MOVE_VERTICAL).RemapXZ();
            movement = transform.rotation * movement;

            if (movement.magnitude > 1F)
                movement.Normalize();


            yVelocity -= Utility.GRAVITY * Time.deltaTime;
            controller.Move((playerSpeed * movement + yVelocity * Vector3.up) * Time.deltaTime);

            if (controller.isGrounded)
                yVelocity = 0F;

            if (lanternHeld)
            {
                if (Input.GetButtonDown(DROP))
                {
                    lanternHeld = false;
                    lantern.SetParent(null);
                    lanternRb.isKinematic = false;
                }
                else if (Input.GetButtonDown(INTERACT))
                {
                    lanternHeld = false;
                    lantern.SetParent(null);
                    lanternRb.isKinematic = false;
                    lantern.localRotation = Quaternion.identity;

                    lanternRb.velocity = cameraAxis.forward * 25F;
                }
            }
            else if (Vector3.Distance(transform.position, lantern.transform.position) < pickupRadius && (Input.GetButtonDown(INTERACT) || Input.GetButtonDown(DROP)))
            {
                lanternHeld = true;
                lantern.SetParent(lanternAnchor);
                lantern.localPosition = Vector3.zero;
                lantern.localRotation = Quaternion.identity;

                lanternRb.isKinematic = true;
            }
        }

        private void LateUpdate()
        {
            Vector2 camera = Input.GetAxis2D(LOOK_VERTICAL, LOOK_HORIZONTAL);

            yaw += camera.y * cameraSpeed * Time.deltaTime;
            pitch -= camera.x * cameraSpeed * Time.deltaTime;

            if (pitch > cameraPitchBoundHigh)
                pitch = cameraPitchBoundHigh;
            else if (pitch < cameraPitchBoundLow)
                pitch = cameraPitchBoundLow;

            transform.localEulerAngles = transform.localEulerAngles.Set(yaw, Utility.Axis.Y);
            cameraAxis.transform.localEulerAngles = cameraAxis.transform.localEulerAngles.Set(pitch, Utility.Axis.X);
        }
    }
}