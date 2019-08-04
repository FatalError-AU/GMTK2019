using System;
using System.Linq;
using Animancer;
using Animation;
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
        public static PlayerController Instance { get; private set; }
        public static Rewired.Player Input { get; private set; }

        public Transform cameraAxis;

        public float adsMovementSpeed = 3F;
        public float playerSpeed = 5F;
        public float sprintSpeed = 10F;
        public float pickupRadius = 4F;

        public float maxHealth = 100F;
        public float healthRecovery = 10F;

        [Header("Camera")]
        public float cameraSpeed = 25F;
        public float gamepadCameraSpeed = 120F;
        public float cameraPitchBoundHigh;
        public float cameraPitchBoundLow;

        [Header("Gun")]
        public int magazineSize = 10;
        public float cooldown = .25F;
        public float gunDamage;
        public float recoil = 1F;
        public float recoilWithLantern = 2F;
        public float recoilFalloff = .25F;
        public float maxRecoil = 15F;
        public float adsSpeed = .05F;

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
        
        [Header("Crosshair")]
        public CanvasGroup uiCrosshair;
        public float crosshairAodSize = 64F;
        public float crosshairNeutralSize = 120F;
        public float crosshairMovingSize = 170F;

        [Header("Animation")]
        [AnimationCollection.Validate(typeof(ArmsAnimation))]
        public AnimationCollection armsAnimations;
        public AvatarMask lanternMask;
        public AvatarMask gunSliderMask;

        private EventfulAnimancerComponent animancer;
        private LinearMixerState movementMixer;
        private AnimancerState fire;
        private AnimancerState reload;
        private AnimancerState reloadEmpty;
        private AnimancerState gunSliderBack;

        private LinearMixerState lanternMixer;
        private AnimancerState lanternThrow;

        private float pitch;
        private float yaw;

        private float yVelocity;

        private int bullets;
        private float gunCooldown;

        private CharacterController controller;

        private Transform lantern;
        private Rigidbody lanternRb;
        private ConfigurableJoint lanternJoint;
        public bool LanternHeld { get; private set; }

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
        private float mixerVelocity;

        private bool lanternLock;

        private float crosshairVelocity;
        private Vector2 crosshairZoomVelocity;
        
        private void Awake()
        {
            Instance = this;
            AnimancerPlayable.maxLayerCount = 5;

            Input = ReInput.players.GetPlayer(Controls.Player.MAIN_PLAYER);

            controller = GetComponent<CharacterController>();

            lantern = GameObject.FindWithTag("Lantern").transform;
            lanternRb = lantern.GetComponentInChildren<Rigidbody>();

            animancer = GetComponentInChildren<EventfulAnimancerComponent>();
            animancer.onEvent = new AnimationEventReceiver(null, OnEvent);

            movementMixer = new LinearMixerState(animancer);
            movementMixer.Initialise(
                    armsAnimations.GetClip(ArmsAnimation.Idle),
                    armsAnimations.GetClip(ArmsAnimation.Walk),
                    armsAnimations.GetClip(ArmsAnimation.Sprint),
                    0F, playerSpeed, sprintSpeed
            );
            movementMixer.Play();
            movementMixer.Layer.SetWeight(1F);

            lanternMixer = new LinearMixerState(animancer.GetLayer(2));
            lanternMixer.Initialise(armsAnimations.GetClip(ArmsAnimation.LanternIdle),
                    armsAnimations.GetClip(ArmsAnimation.LanternWalk),
                    0F, playerSpeed
            );
            lanternMixer.Layer.SetMask(lanternMask);
            lanternMixer.Play();
            animancer.GetLayer(2).SetWeight(0F);

            fire = animancer.CreateState(armsAnimations.GetClip(ArmsAnimation.Fire), 1);
            reload = animancer.CreateState(armsAnimations.GetClip(ArmsAnimation.Reload), 4);
            reloadEmpty = animancer.CreateState(armsAnimations.GetClip(ArmsAnimation.ReloadEmpty), 4);
            gunSliderBack = animancer.CreateState(armsAnimations.GetClip(ArmsAnimation.SliderBack), 3);
            gunSliderBack.Play();
            gunSliderBack.Layer.SetMask(gunSliderMask);
            gunSliderBack.Layer.SetWeight(0F);

            lanternThrow = animancer.CreateState(armsAnimations.GetClip(ArmsAnimation.LanternThrow), 2);
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
            health += healthRecovery * Time.deltaTime / maxHealth;
            health = Mathf.Clamp01(health);
            
            Vector3 movement = Input.GetAxis2D(MOVE_HORIZONTAL, MOVE_VERTICAL).RemapXZ();
            movement = transform.rotation * movement;

            if (movement.magnitude > 1F)
                movement.Normalize();

            if (movement.z <= Mathf.Epsilon)
                sprinting = false;

            bool isAiming = Input.GetButton(AIM) && !LanternHeld;

            yVelocity -= Utility.GRAVITY * Time.deltaTime;

            if (movement.sqrMagnitude > Mathf.Epsilon)
            {
                if (Input.GetButtonDown(SPRINT))
                    sprinting = !sprinting;
            }
            else
                sprinting = false;

            controller.Move(((isAiming ? adsMovementSpeed : sprinting ? sprintSpeed : playerSpeed) * movement + yVelocity * Vector3.up) * Time.deltaTime);
            movementMixer.Parameter = Mathf.SmoothDamp(movementMixer.Parameter, controller.velocity.Remove(Utility.Axis.Y).magnitude, ref mixerVelocity, .1F);
            lanternMixer.Parameter = Mathf.SmoothDamp(lanternMixer.Parameter, controller.velocity.Remove(Utility.Axis.Y).magnitude, ref mixerVelocity, .1F);

            uiCrosshair.alpha = Mathf.SmoothDamp(uiCrosshair.alpha, isAiming || LanternHeld ? 0F : 1F, ref crosshairVelocity, adsSpeed);
            float crosshairZoom = isAiming ? crosshairAodSize : controller.velocity.magnitude > playerSpeed / 10F ? crosshairMovingSize : crosshairNeutralSize;
            RectTransform crosshairTransform = uiCrosshair.transform as RectTransform;
            
            if(crosshairTransform != null)
                crosshairTransform.sizeDelta = Vector2.SmoothDamp(crosshairTransform.sizeDelta, Vector2.one * crosshairZoom, ref crosshairZoomVelocity, adsSpeed);
            
            if (controller.isGrounded)
                yVelocity = 0F;

            if (lanternLock)
                return;

            if (LanternHeld)
            {
                bool drop = Input.GetButtonDown(DROP);
                bool yeet = Input.GetButtonDown(INTERACT);

                if (drop || yeet)
                {
                    lanternLock = true;

                    if (yeet)
                        animancer.CrossFadeFromStart(lanternThrow, .05F).OnEnd = () =>
                        {
                            lanternThrow.OnEnd = null;
                            lanternMixer.Play();
                            lanternThrow.Layer.StartFade(0F, .25F);
                        };
                    else
                        Drop(false);
                }
            }
            else if (!isAiming && Vector3.Distance(transform.position, lantern.transform.position) < pickupRadius && (Input.GetButtonDown(INTERACT) || Input.GetButtonDown(DROP)))
            {
                LanternHeld = true;
                lantern.SetParent(lanternAnchor);
                lantern.localPosition = Vector3.zero;
                lantern.localRotation = Quaternion.identity;
                lantern.localScale = Vector3.one;

                lanternRb.isKinematic = true;
                animancer.CrossFade(lanternMixer, .25F);
            }

            cameraPosition.position = Vector3.SmoothDamp(cameraPosition.position, isAiming ? sightsAnchor.position : cameraPosition.parent.position, ref cameraVelocity, adsSpeed);
            cameraPosition.localEulerAngles = new Vector3(cameraPosition.localEulerAngles.x, Mathf.SmoothDampAngle(cameraPosition.localEulerAngles.y, isAiming ? sightsAnchor.parent.localEulerAngles.y + sightsAnchor.localEulerAngles.y : cameraPosition.parent.localEulerAngles.y, ref cameraAngularVelocity, adsSpeed), 0F);

            if (reloading)
                return;

            if (gunCooldown > 0F)
                gunCooldown -= Time.deltaTime;
            else if (bullets > 0 && Input.GetButtonDown(SHOOT))
            {
                sprinting = false;

                bullets--;
                gunCooldown = cooldown;
                targetRecoil += LanternHeld ? recoilWithLantern : recoil;

                if (bullets <= 0)
                    gunSliderBack.Layer.StartFade(1F, .1F);

                animancer.CrossFadeFromStart(fire, .03F).OnEnd = () => { fire.Stop(); };

                if (Physics.Raycast(gunAnchor.position, gunAnchor.forward, out RaycastHit hit, 100F))
                {
                    EnemyHealth health = hit.collider.GetComponent<EnemyHealth>();
                    if (health)
                        health.Damage(gunDamage);
                }
            }
            else if (!LanternHeld && (Input.GetButtonDown(RELOAD) && bullets < magazineSize || bullets <= 0))
            {
                animancer.CrossFadeFromStart(bullets <= 0 ? reloadEmpty : reload, .05F).OnEnd = () =>
                {
                    reload.OnEnd = null;
                    reloadEmpty.OnEnd = null;

                    reloading = false;
                    bullets = magazineSize;
                    reload.Layer.StartFade(0F, .05F);

                    gunSliderBack.Layer.SetWeight(0F);
                };
                reloading = true;
            }
        }

        public void Damage(float damage, Transform damageSource)
        {
            if (damageSource)
            {
                float a = -Vector3.SignedAngle(damageSource.position - transform.position, transform.forward, Vector3.up);
                float[] differences = uiHitmarksAngles.Select(x => Mathf.Abs(x - a)).ToArray();
                CompassDirection direction = (CompassDirection) Array.IndexOf(differences, differences.Min());
                if (direction == CompassDirection.South2)
                    direction = CompassDirection.South;

                hitmarkOpacity[(int) direction] = 1F;
            }

            health -= damage / maxHealth;

            if (health <= 0F)
            {
                //Dead
            }
        }

        private void LateUpdate()
        {
            Vector2 camera = Input.GetAxis2D(LOOK_VERTICAL, LOOK_HORIZONTAL);

            bool isGamepad = Input.controllers.GetLastActiveController()?.type == ControllerType.Joystick;

            yaw += camera.y * (isGamepad ? gamepadCameraSpeed : cameraSpeed) * Time.deltaTime;
            pitch -= camera.x * (isGamepad ? gamepadCameraSpeed : cameraSpeed) * Time.deltaTime;

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

        private void OnEvent(AnimationEvent e)
        {
            if ("LanternThrow".Equals(e.stringParameter))
            {
                Drop(true);
            }
        }

        private void Drop(bool yeet)
        {
            sprinting = false;
            
            lanternLock = false;
            LanternHeld = false;
            lantern.SetParent(null);
            lantern.localScale = Vector3.one;
            
            lanternRb.isKinematic = false;
            
            if(yeet)
                lanternRb.velocity = cameraAxis.forward * 25F;
            else
            {
                lanternThrow.Layer.StartFade(0F, .25F);
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

        [AnimationCollection.EnumAttribute("Arms Animations")]
        public enum ArmsAnimation
        {
            Idle,
            Walk,
            Sprint,
            Fire,
            Reload,
            ReloadEmpty,
            SliderBack,

            [Header("Lantern")]
            LanternIdle,
            LanternWalk,
            LanternThrow
        }
    }
}