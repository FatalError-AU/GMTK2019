using System;
using System.Linq;
using Enemy;
using InspectorGadgets.Attributes;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Controls.Action;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        public static Rewired.Player Input { get; private set; }

        public Transform test;
        
        public Transform cameraAxis;

        public float playerSpeed = 25F;
        public float sprintSpeed = 45F;
        public float pickupRadius = 4F;

        public float maxHealth = 100F;
        
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
        public Image uiDamageIndicator;
        [LabelledCollection(typeof(CompassDirection))]
        public Image[] uiHitmarks;
        [LabelledCollection(typeof(CompassDirection))]
        public float[] uiHitmarksAngles;
        public float uiHitmarkFalloff = .4F;

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

        private float health = 1F;
        private float damageIndicatorVelocity;

        private float[] hitmarkOpacity;
        
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
            
            hitmarkOpacity = new float[uiHitmarksAngles.Length];
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
                sprinting = false;
                
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

        public void Damage(float damage, Transform damageSource)
        {
            if (damageSource)
            {
                float a = -Vector3.SignedAngle(test.transform.position - transform.position, transform.forward, Vector3.up);
                float[] differences = uiHitmarksAngles.Select(x => Mathf.Abs(x - a)).ToArray();
                CompassDirection direction = (CompassDirection) Array.IndexOf(differences, differences.Min());
                if (direction == CompassDirection.South2)
                    direction = CompassDirection.South;

                hitmarkOpacity[(int)direction] = 1F;
            }
            
            health -= damage / maxHealth;

            if (health <= 0F)
            {
                //Dead
            }
        }
        
        private void LateUpdate()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Z))
                Damage(10F, test);
            
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

            Color c = uiDamageIndicator.color;
            c.a = Mathf.SmoothDamp(c.a, 1F - health, ref damageIndicatorVelocity, .25F);
            uiDamageIndicator.color = c;

            for (int i = 0; i < hitmarkOpacity.Length; i++)
            {
                if (hitmarkOpacity[i] > 0F)
                    hitmarkOpacity[i] -= Time.deltaTime * uiHitmarkFalloff;

                if (uiHitmarks[i])
                {
                    Color col = uiHitmarks[i].color;
                    col.a = hitmarkOpacity[i];
                    uiHitmarks[i].color = col;
                }
            }
        }

        private enum CompassDirection
        {
            North,
            NorthEast,
            East,
            SouthEast,
            South,
            South2,
            SouthWest,
            West,
            NorthWest
        }
    }
}