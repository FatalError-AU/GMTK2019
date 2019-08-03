using Enemy;
using Rewired;
using TMPro;
using UnityEngine;
using static Controls.Action;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        public static Rewired.Player Input { get; private set; }

        public Transform cameraAxis;

        public float playerSpeed = 25F;
        public float sprintSpeed = 45F;
        public float pickupRadius = 4F;

        [Header("Camera")]
        public float cameraSpeed = 25F;
        public float cameraPitchBoundHigh;
        public float cameraPitchBoundLow;

        [Header("Gun")]
        public int magazineSize = 10;
        public float cooldown = .25F;
        public float gunDamage;
        public float reloadLength = 1F;
        public float recoil = 1F;
        public float recoilWithLantern = 2F;
        public float recoilFalloff = .25F;
        public float maxRecoil = 15F;

        [Header("Anchors")]
        public Transform lanternAnchor;
        public Transform gunAnchor;
        public Transform sightsAnchor;
        public Transform cameraPosition;

        [Header("UI")]
        public TextMeshProUGUI uiAmmoCount;

        private float pitch;
        private float yaw;

        private float yVelocity;

        private int bullets;
        private float gunCooldown;

        private CharacterController controller;

        private Transform lantern;
        private Rigidbody lanternRb;
        private bool lanternHeld;

        private float reloadTimer;
        private bool reloading;

        private float recoilValue;
        private float targetRecoil;
        private float recoilVelocity;

        private bool sprinting;

        private Vector3 cameraVelocity;
        private float cameraAngularVelocity;

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
            bullets = magazineSize;
        }

        private void Update()
        {
            Vector3 movement = Input.GetAxis2D(MOVE_HORIZONTAL, MOVE_VERTICAL).RemapXZ();
            movement = transform.rotation * movement;

            if (movement.magnitude > 1F)
                movement.Normalize();

            bool isAiming = Input.GetButton(AIM) && !lanternHeld;            

            yVelocity -= Utility.GRAVITY * Time.deltaTime;

            if (movement.sqrMagnitude > Mathf.Epsilon)
            {
                if (Input.GetButtonDown(SPRINT))
                    sprinting = !sprinting;
            }
            else
                sprinting = false;
            
            controller.Move(((sprinting ? sprintSpeed : playerSpeed) * movement + yVelocity * Vector3.up) * Time.deltaTime);

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
            else if (!isAiming && Vector3.Distance(transform.position, lantern.transform.position) < pickupRadius && (Input.GetButtonDown(INTERACT) || Input.GetButtonDown(DROP)))
            {
                lanternHeld = true;
                lantern.SetParent(lanternAnchor);
                lantern.localPosition = Vector3.zero;
                lantern.localRotation = Quaternion.identity;

                lanternRb.isKinematic = true;
            }

            cameraPosition.position = Vector3.SmoothDamp(cameraPosition.position, isAiming ? sightsAnchor.position : cameraPosition.parent.position, ref cameraVelocity, .25F);

            cameraPosition.localEulerAngles = new Vector3(cameraPosition.localEulerAngles.x, Mathf.SmoothDampAngle(cameraPosition.localEulerAngles.y, isAiming ? sightsAnchor.parent.localEulerAngles.y + sightsAnchor.localEulerAngles.y : cameraPosition.parent.localEulerAngles.y, ref cameraAngularVelocity, .25F), 0F);
            
            if (reloading)
            {
                reloadTimer -= Time.deltaTime;
                if (reloadTimer <= 0F)
                {
                    reloading = false;
                    bullets = magazineSize;
                }
                return;
            }

            if (gunCooldown > 0F)
                gunCooldown -= Time.deltaTime;
            else if (bullets > 0 && Input.GetButtonDown(SHOOT))
            {
                bullets--;
                gunCooldown = cooldown;
                targetRecoil += lanternHeld ? recoilWithLantern : recoil;

                if (Physics.Raycast(gunAnchor.position, gunAnchor.forward, out RaycastHit hit, 100F))
                {
                    EnemyHealth health = hit.collider.GetComponent<EnemyHealth>();
                    if (health)
                        health.Damage(gunDamage);
                }
            } else if (!lanternHeld && (Input.GetButtonDown(RELOAD) && bullets < magazineSize || bullets <= 0))
            {
                reloadTimer = reloadLength;
                reloading = true;
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

            targetRecoil -= recoilFalloff * Time.deltaTime;
            targetRecoil = Mathf.Clamp(targetRecoil, 0F, maxRecoil);
            recoilValue = Mathf.SmoothDampAngle(recoilValue, targetRecoil, ref recoilVelocity, .1F);
            
            transform.localEulerAngles = transform.localEulerAngles.Set(yaw, Utility.Axis.Y);
            cameraAxis.transform.localEulerAngles = cameraAxis.transform.localEulerAngles.Set(pitch - recoilValue, Utility.Axis.X);

            uiAmmoCount.text = bullets.ToString();
        }
    }
}