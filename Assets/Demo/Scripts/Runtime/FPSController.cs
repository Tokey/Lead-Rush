// Designed by KINEMATION, 2023

using Kinemation.FPSFramework.Runtime.FPSAnimator;
using Kinemation.FPSFramework.Runtime.Layers;
using Kinemation.FPSFramework.Runtime.Recoil;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Demo.Scripts.Runtime
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class TabAttribute : PropertyAttribute
    {
        public readonly string tabName;

        public TabAttribute(string tabName)
        {
            this.tabName = tabName;
        }
    }

    public enum FPSAimState
    {
        None,
        Ready,
        Aiming,
        PointAiming
    }

    public enum FPSActionState
    {
        None,
        Reloading,
        WeaponChange
    }


    // An example-controller class
    public class FPSController : FPSAnimController
    {
        [Tab("Animation")]
        [Header("General")]
        [SerializeField] private Animator animator;

        [Header("Turn In Place")]
        [SerializeField] private float turnInPlaceAngle;
        [SerializeField] private AnimationCurve turnCurve = new AnimationCurve(new Keyframe(0f, 0f));
        [SerializeField] private float turnSpeed = 1f;

        [Header("Leaning")]
        [SerializeField] private float smoothLeanStep = 1f;
        [SerializeField, Range(0f, 1f)] private float startLean = 1f;

        [Header("Dynamic Motions")]
        [SerializeField] private IKAnimation aimMotionAsset;
        [SerializeField] private IKAnimation leanMotionAsset;
        [SerializeField] private IKAnimation crouchMotionAsset;
        [SerializeField] private IKAnimation unCrouchMotionAsset;
        [SerializeField] private IKAnimation onJumpMotionAsset;
        [SerializeField] private IKAnimation onLandedMotionAsset;
        [SerializeField] private IKAnimation onStartStopMoving;

        [SerializeField] private IKPose sprintPose;
        [SerializeField] private IKPose pronePose;

        // Animation Layers
        [SerializeField][HideInInspector] private LookLayer lookLayer;
        [SerializeField][HideInInspector] private AdsLayer adsLayer;
        [SerializeField][HideInInspector] private SwayLayer swayLayer;
        [SerializeField][HideInInspector] private LocomotionLayer locoLayer;
        [SerializeField][HideInInspector] private SlotLayer slotLayer;
        [SerializeField][HideInInspector] public WeaponCollision collisionLayer;
        // Animation Layers

        [Header("General")]
        [Tab("Controller")]
        [SerializeField] private float timeScale = 1f;
        [SerializeField, Min(0f)] private float equipDelay = 0f;

        [Header("Camera")]
        [SerializeField] public Transform mainCamera;
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private Transform firstPersonCamera;
        [SerializeField] private float sensitivity;
        [SerializeField] private Vector2 freeLookAngle;

        [Header("Movement")]
        [SerializeField] private FPSMovement movementComponent;

        [SerializeField]
        [Tab("Weapon")]
        private List<Weapon> weapons;
        private Vector2 _playerInput;

        // Used for free-look
        private Vector2 _freeLookInput;

        private int _currentWeaponIndex;
        private int _lastIndex;

        private int _bursts;
        private bool _freeLook;

        private FPSAimState aimState;
        private FPSActionState actionState;

        private static readonly int Crouching = Animator.StringToHash("Crouching");
        private static readonly int OverlayType = Animator.StringToHash("OverlayType");
        private static readonly int TurnRight = Animator.StringToHash("TurnRight");
        private static readonly int TurnLeft = Animator.StringToHash("TurnLeft");
        private static readonly int UnEquip = Animator.StringToHash("UnEquip");

        private Vector2 _controllerRecoil;
        private float _recoilStep;
        private bool _isFiring;

        private bool _isUnarmed;
        private float _lastRecoilTime;


        public GameObject reticleObject;
        private float fireTimer;

        RaycastHit hit;

        public float regularHitCooldown;
        public float killCooldown;
        public float headshotCooldown;

        //Audio
        public AudioClip weaponChangeSFX;
        public AudioClip ADSInSFX;
        public AudioClip ADSOutSFX;
        public AudioClip hitRegSFX;
        public AudioClip killSFX;
        public AudioClip headshotSFX;
        public AudioClip deathSFX;

        public AudioSource playerAudioSource;

        public long score;
        public int roundKills;
        public int roundDeaths;

        public int shotsFiredPerRound;
        public int shotsHitPerRound;
        public int headshotsHitPerRound;

        public Transform playerSpawnPoint;

        public EnemyManager enemyManager;

        public float deathTimeOut;

        public bool isPlayerReady = false;

        Vector3 oldPosition;
        public float distanceTravelledPerRound;

        public float delXCumilative = 0;
        public float delYCumilative = 0;

        public ParticleSystem muzzleFlash;

        public ParticleSystem bulletHitPE;

        public int reloadCountPerRound = 0;

        public bool isQoeDisabled;
        public bool isAcceptabilityDisabled;

        public GameManager gameManager;

        public bool isAimSpikeEnabled;
        public bool isReloadSpikeEnabled;
        public bool isMouseMovementSpikeEnabled;
        public bool isEnemySpawnSpikeEnabled;

        float mouseSpikeCooldown;
        public float mouseSpikeDelay;
        public float mouseSpikeDegreeOfMovement;
        public float aimSpikeDelay;
        float aimSpikeCooldown;

        bool enemyAimedFuse;

        public float deltaMouseX;
        public float deltaMouseY;

        public LayerMask enemyLargeColliderLayer;
        public int perRoundAimSpikeCount;
        public int perRoundReloadSpikeCount;
        public int perRoundMouseMovementSpikeCount;
        public int perRoundEnemySpawnSpikeCount;

        public PlayerTickLog playerTickLog;

        public PerShotLog perShotLog;

        public ShootingEventLog shootingEventLog;

        public Image clickToPhotonIMG;

        public RoundManager roundManager;

        public float degreeToTargetX;
        public float degreeToTargetY;

        public bool targetMarked;

        public float degreeToShootX;
        public float degreeToShootY;

        public bool targetShot;

        public float degreeToTargetXCumulative;
        public float degreeToShootXCumulative;

        public float timeToTargetEnemy;
        public float timeToHitEnemy;
        public float timeToKillEnemy;

        public float timeToTargetEnemyCumulative;
        public float timeToHitEnemyCumulative;
        public float timeToKillEnemyCumulative;

        public float enemySizeCumulative;

        public float minAnlgeToEnemyCumulative;

        public int tacticalReloadCountPerRound = 0;

        public float aimDurationPerRound;
        public float isFiringDurationPerRound;

        public float enemySpeedGlobal;

        public float reticleSizeMultiplier;

        public float enemyHealthGlobal;

        public float liveAccuracy;

        public int onHitScore;
        public int onKillScore;
        public int onMissScore;
        public int onDeathScore;

        public double spikeDurationCumulative;

        public bool aimAssistEnabled = true;
        public float aimAssistStrength = 5f;

        public GameObject currentEnemyHead;

        public GameObject muzzlePointTransformGO;

        public GameObject weaponShootPoint;

        public bool playerOnTarget;

        public Camera playerCamera;

        public float timeTillLastKill;

        public bool isFirstShotAfterSpike;
        public int shotCountAfterSpike;

        public int postSpikeFirstShotHits;
        public int postSpikeFirstShotMisses;

        float postSpikeFirstShotAccuracy;
        float elapsedTimeFromLastSpike;
        float elapsedTimeSinceLastEventExcludingSpike;
        float elapsedTimeSinceLastEventIncludingSpike;

        public float missAnglePublic;
        public bool validMissPublic;

        public float horizontalMissAnglePublic = 0f;
        public float verticalMissAnglePublic = 0f;
        public bool enemyIsInFrontPublic = false;

        public float localRadRequiredPublic;
        public float worldRadRequiredPublic;

        public float extraRadRequiredPublic;
        public float distanceFromEnemyPublic;

        public float worldRadiusPublic;

        public float onTargetDuration;

        public float timeOnTargetEachEnemy;

        public float timeOnTargetPerEvent;

        public float mouseMovedXPerEvent;
        public float mouseMovedYPerEvent;

        public int shotsFiredPerEvent;
        public int shotsHitPerEvent;
        public int shotsMissedPerEvent;

        public float durationOfEvent;
        public float durationOfEventWithSpike;
        public int spikeCountPerEvent;
        public float angularDistanceFromEnemyOnStart;

        public bool readyToInduceSpike = true;


        private void InitLayers()
        {
            InitAnimController();

            animator = GetComponentInChildren<Animator>();
            lookLayer = GetComponentInChildren<LookLayer>();
            adsLayer = GetComponentInChildren<AdsLayer>();
            locoLayer = GetComponentInChildren<LocomotionLayer>();
            swayLayer = GetComponentInChildren<SwayLayer>();
            slotLayer = GetComponentInChildren<SlotLayer>();
            collisionLayer = GetComponentInChildren<WeaponCollision>();
        }

        private bool HasActiveAction()
        {
            return actionState != FPSActionState.None;
        }

        private bool IsAiming()
        {
            return aimState is FPSAimState.Aiming or FPSAimState.PointAiming;
        }

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            moveRotation = transform.rotation;

            movementComponent = GetComponent<FPSMovement>();

            movementComponent.onStartMoving.AddListener(OnMoveStarted);
            movementComponent.onStopMoving.AddListener(OnMoveEnded);

            movementComponent.onCrouch.AddListener(OnCrouch);
            movementComponent.onUncrouch.AddListener(OnUncrouch);

            movementComponent.onJump.AddListener(OnJump);
            movementComponent.onLanded.AddListener(OnLand);

            movementComponent.onSprintStarted.AddListener(OnSprintStarted);
            movementComponent.onSprintEnded.AddListener(OnSprintEnded);

            movementComponent.onSlideStarted.AddListener(OnSlideStarted);
            movementComponent.onSlideEnded.AddListener(OnSlideEnded);

            movementComponent.onProneStarted.AddListener(() => collisionLayer.SetLayerAlpha(0f));
            movementComponent.onProneEnded.AddListener(() => collisionLayer.SetLayerAlpha(1f));

            movementComponent.slideCondition += () => !HasActiveAction();
            movementComponent.sprintCondition += () => !HasActiveAction();
            movementComponent.proneCondition += () => !HasActiveAction();

            actionState = FPSActionState.None;

            InitLayers();
            EquipWeapon();

            playerAudioSource = GetComponent<AudioSource>();
            enemyAimedFuse = false;

            playerTickLog = new PlayerTickLog();

            playerTickLog.time = new List<string>();
            playerTickLog.mouseX = new List<float>();
            playerTickLog.mouseY = new List<float>();

            playerTickLog.playerX = new List<float>();
            playerTickLog.playerY = new List<float>();
            playerTickLog.playerZ = new List<float>();

            playerTickLog.roundTimer = new List<float>();

            playerTickLog.scorePerSec = new List<float>();

            playerTickLog.playerRot = new List<Quaternion>();
            playerTickLog.enemyPos = new List<Vector3>();

            playerTickLog.isADS = new List<bool>();

            playerTickLog.frameTimeMS = new List<double>();


            playerTickLog.muzzleX = new List<float>();
            playerTickLog.muzzleY = new List<float>();
            playerTickLog.muzzleZ = new List<float>();
            playerTickLog.targetAngleToEnemy = new List<float>();
            playerTickLog.validMissContinious = new List<bool>();

            playerTickLog.playerOnTarget = new List<bool>();
            playerTickLog.onTargetDuration = new List<float>();

            playerTickLog.time.Clear();
            playerTickLog.mouseX.Clear();
            playerTickLog.mouseY.Clear();

            targetMarked = false;
            targetShot = false;

            timeTillLastKill = 0f;

            shotCountAfterSpike = 0;
            postSpikeFirstShotHits = 0;
            postSpikeFirstShotMisses = 0;


            //Shooting event log

            shootingEventLog = new ShootingEventLog();

            shootingEventLog.time = new List<string>();
            shootingEventLog.roundTimer = new List<float>();

            shootingEventLog.mouseX = new List<float>();
            shootingEventLog.mouseY = new List<float>();

            shootingEventLog.playerX = new List<float>();
            shootingEventLog.playerY = new List<float>();
            shootingEventLog.playerZ = new List<float>();

            shootingEventLog.playerRot = new List<Quaternion>();
            shootingEventLog.enemyPos = new List<Vector3>();
            shootingEventLog.muzzlePos = new List<Vector3>();
            shootingEventLog.distanceToPlayer = new List<float>();

            shootingEventLog.isEnemyDead = new List<bool>();

            shootingEventLog.mouseMovedX = new List<float>();
            shootingEventLog.mouseMovedY = new List<float>();

            shootingEventLog.shotsFiredPerEvent = new List<int>();
            shootingEventLog.shotsHitPerEvent = new List<int>();
            shootingEventLog.shotsMissedPerEvent = new List<int>();

            shootingEventLog.durationOfEventNotIncludingSpikes = new List<float>();
            shootingEventLog.durationOfEventIncludingSpikes = new List<float>();
            shootingEventLog.spikeCountPerEvent = new List<int>();
            shootingEventLog.angularDistanceFromEnemyOnStart = new List<float>();
            shootingEventLog.targetTimeOnEnemy = new List<float>();
            readyToInduceSpike = true;
        }

        private void UnequipWeapon()
        {
            DisableAim();

            actionState = FPSActionState.WeaponChange;
            GetAnimGraph().GetFirstPersonAnimator().CrossFade(UnEquip, 0.1f);
        }

        public void ResetActionState()
        {
            actionState = FPSActionState.None;
        }

        public void RefreshStagedState()
        {
        }

        public void ResetStagedState()
        {
        }

        private void EquipWeapon()
        {
            if (weapons.Count == 0) return;

            weapons[_lastIndex].gameObject.SetActive(false);
            var gun = weapons[_currentWeaponIndex];

            _bursts = gun.burstAmount;

            InitWeapon(gun);
            gun.gameObject.SetActive(true);

            animator.SetFloat(OverlayType, (float)gun.overlayType);
            actionState = FPSActionState.None;
        }

        private void EnableUnarmedState()
        {
            if (weapons.Count == 0) return;

            weapons[_currentWeaponIndex].gameObject.SetActive(false);
            animator.SetFloat(OverlayType, 0);
        }

        private void DisableAim()
        {
            if (!GetGun().canAim) return;

            aimState = FPSAimState.None;
            OnInputAim(false);

            adsLayer.SetAds(false);
            adsLayer.SetPointAim(false);
            swayLayer.SetFreeAimEnable(true);
            swayLayer.SetLayerAlpha(1f);
        }

        public void ToggleAim()
        {
            if (!GetGun().canAim) return;

            slotLayer.PlayMotion(aimMotionAsset);

            if (!IsAiming())
            {
                aimState = FPSAimState.Aiming;
                OnInputAim(true);

                adsLayer.SetAds(true);
                swayLayer.SetFreeAimEnable(false);
                swayLayer.SetLayerAlpha(0.5f);
            }
            else
            {
                DisableAim();
            }

            recoilComponent.isAiming = IsAiming();
        }

        public void StartAiming()
        {
            if (!GetGun().canAim) return;

            slotLayer.PlayMotion(aimMotionAsset);

            if (!IsAiming())
            {
                aimState = FPSAimState.Aiming;
                OnInputAim(true);

                adsLayer.SetAds(true);
                swayLayer.SetFreeAimEnable(false);
                swayLayer.SetLayerAlpha(0.5f);
            }

            recoilComponent.isAiming = IsAiming();
        }

        public void StopAiming()
        {
            slotLayer.PlayMotion(aimMotionAsset);
            DisableAim();
            recoilComponent.isAiming = IsAiming();
        }

        public void ChangeScope()
        {
            InitAimPoint(GetGun());
        }

        private void Fire()
        {
            if (HasActiveAction()) return;

            if (GetGun().currentAmmoCount <= 0)
            {
                OnFireReleased();
                return;
            }

            GetGun().currentAmmoCount--;

            GetGun().weaponAudioSource.PlayOneShot(GetGun().fireSFX);
            fireTimer = 60 / GetGun().fireRate;

            shotsFiredPerRound++;

            ParticleSystem PE = Instantiate(muzzleFlash, GetGun().shootPoint.transform.position, GetGun().shootPoint.transform.rotation);
            PE.transform.SetParent(GetGun().shootPoint.transform);

            // ====== CACHE ENEMY HEAD AT MOMENT OF FIRE ======
            var enemyHeadAtShot = currentEnemyHead;
            SphereCollider sc = null;
            Transform scT = null;

            if (enemyHeadAtShot != null)
            {
                sc = enemyHeadAtShot.GetComponent<SphereCollider>();
                scT = sc.transform;
            }

            Transform muzzleT = GetGun().shootPoint.transform;

            // ====== MISS ANGLE & HORIZ/VERT ANGLE CALCULATION (every shot) ======
            float missAngle = -999f;
            float missAngleFromCenter = -999f;
            float surfaceDistance = 0f;
            float worldRadiusRequired = 0f;
            float extraRadiusWorld = 0f;
            float localRadiusRequired = 0f;
            float worldRadius = 0f;
            float currentAccuracy = 0f;
            bool enemyInView = false;
            bool validMiss = false;
            float angularRadiusDeg = 0f;
            float horizontalMissAngle = 0f;
            float verticalMissAngle = 0f;
            bool enemyIsInFront = false;
            bool isHit = false;

            if (enemyHeadAtShot != null && sc != null && scT != null)
            {
                // Calculate all miss and angle values
                // --- Function Call ---
                CalculateMissAndAngles(
                    muzzleT,
                    scT,
                    sc,
                    playerCamera,
                    timeTillLastKill,
                    out missAngle,              // <-- NEW: surface-based miss
                    out missAngleFromCenter,    // <-- OLD: center-to-center
                    out horizontalMissAngle,
                    out verticalMissAngle,
                    out angularRadiusDeg,
                    out enemyInView,
                    out enemyIsInFront,
                    out validMiss,
                    out surfaceDistance,
                    out worldRadiusRequired,
                    out extraRadiusWorld,
                    out localRadiusRequired,
                    out worldRadius
                );


                // Optionally update accuracy
                if (shotsFiredPerRound > 0)
                    currentAccuracy = (float)shotsHitPerRound / (float)shotsFiredPerRound;
                else
                    currentAccuracy = 0f;

                // Not a valid miss if enemy is behind!
                if (!enemyIsInFront) validMiss = false;
            }
            else
            {
                missAngle = -999f;
                enemyInView = false;
                validMiss = false;
            }

            missAnglePublic = missAngle;
            validMissPublic = validMiss;

            // ====== Raycast for hit/miss check ======
            Transform muzzlePointTransform = muzzleT;
            Vector3 shotDir = muzzlePointTransform.forward;
            if (Physics.Raycast(muzzlePointTransform.position, shotDir, out hit, Mathf.Infinity, ~enemyLargeColliderLayer))
            {
                Instantiate(bulletHitPE, hit.point, Quaternion.LookRotation(hit.normal));

                if (enemyHeadAtShot != null && hit.collider != null && hit.collider.gameObject.name == "Head")
                {
                    // HEADSHOT
                    isHit = true;
                    if (!targetShot) targetShot = true;

                    bool isTPing = hit.collider.GetComponentInParent<Enemy>().isTeleporting;
                    bool isInvincible = hit.collider.GetComponentInParent<Enemy>().invincibilityEffect.activeSelf;
                    if (!isTPing && !isInvincible)
                    {
                        
                        shotsHitPerEvent++;
                        shotsFiredPerEvent++;

                        hit.collider.GetComponentInParent<Enemy>().TakeDamage(GetGun().bulletDamage * 5);
                        
                        PlayHeadshotSFX();
                        shotsHitPerRound++;
                        headshotsHitPerRound++;
                        score += onHitScore;
                    }
                    else
                    {
                        isHit = false;
                    }

                    headshotCooldown = .2f;

                    if (shotCountAfterSpike <= 0)
                    {
                        isFirstShotAfterSpike = true;
                        shotCountAfterSpike++;
                        postSpikeFirstShotHits++;
                    }
                    else
                    {
                        isFirstShotAfterSpike = false;
                        shotCountAfterSpike++;
                    }

                    if (postSpikeFirstShotHits + postSpikeFirstShotMisses <= 0)
                        postSpikeFirstShotAccuracy = 0;
                    else
                        postSpikeFirstShotAccuracy = (float)postSpikeFirstShotHits / ((float)postSpikeFirstShotHits + (float)postSpikeFirstShotMisses);

                    UpdateShotLog(
                        surfaceDistance, missAngle, isHit, worldRadiusRequired, extraRadiusWorld, localRadiusRequired,
                        worldRadius, sc != null ? sc.radius : 0, currentAccuracy, score, roundManager.attributeScalingModule.currentSpeed,
                        roundManager.attributeScalingModule.avgMouseSpeed, enemyInView, timeTillLastKill, validMiss,
                        isFirstShotAfterSpike, shotCountAfterSpike, postSpikeFirstShotHits, postSpikeFirstShotMisses, postSpikeFirstShotAccuracy, elapsedTimeFromLastSpike
                    );


                }
                else
                {
                    // MISSED ENEMY HEAD
                    isHit = false;
                    score += onMissScore;
                    if (shotCountAfterSpike <= 0)
                    {
                        isFirstShotAfterSpike = true;
                        shotCountAfterSpike++;
                        postSpikeFirstShotMisses++;
                    }
                    else
                    {
                        isFirstShotAfterSpike = false;
                        shotCountAfterSpike++;
                    }

                    if (postSpikeFirstShotHits + postSpikeFirstShotMisses <= 0)
                        postSpikeFirstShotAccuracy = 0;
                    else
                        postSpikeFirstShotAccuracy = (float)postSpikeFirstShotHits / ((float)postSpikeFirstShotHits + (float)postSpikeFirstShotMisses);

                    UpdateShotLog(
                        surfaceDistance, missAngle, false, worldRadiusRequired, extraRadiusWorld, localRadiusRequired,
                        worldRadius, sc != null ? sc.radius : 0, currentAccuracy, score, roundManager.attributeScalingModule.currentSpeed,
                        roundManager.attributeScalingModule.avgMouseSpeed, enemyInView, timeTillLastKill, validMiss,
                        isFirstShotAfterSpike, shotCountAfterSpike, postSpikeFirstShotHits, postSpikeFirstShotMisses, postSpikeFirstShotAccuracy, elapsedTimeFromLastSpike
                    );

                    shotsMissedPerEvent++;
                    shotsFiredPerEvent++;
                }
            }
            else
            {
                // COMPLETELY MISSED (ray hit nothing)
                isHit = false;
                score += onMissScore;
                if (shotCountAfterSpike <= 0)
                {
                    isFirstShotAfterSpike = true;
                    shotCountAfterSpike++;
                    postSpikeFirstShotMisses++;
                }
                else
                {
                    isFirstShotAfterSpike = false;
                    shotCountAfterSpike++;
                }

                if (postSpikeFirstShotHits + postSpikeFirstShotMisses <= 0)
                    postSpikeFirstShotAccuracy = 0;
                else
                    postSpikeFirstShotAccuracy = (float)postSpikeFirstShotHits / ((float)postSpikeFirstShotHits + (float)postSpikeFirstShotMisses);

                UpdateShotLog(
                    surfaceDistance, missAngle, false, worldRadiusRequired, extraRadiusWorld, localRadiusRequired,
                    worldRadius, sc != null ? sc.radius : 0, currentAccuracy, score, roundManager.attributeScalingModule.currentSpeed,
                    roundManager.attributeScalingModule.avgMouseSpeed, enemyInView, timeTillLastKill, validMiss,
                    isFirstShotAfterSpike, shotCountAfterSpike, postSpikeFirstShotHits, postSpikeFirstShotMisses, postSpikeFirstShotAccuracy, elapsedTimeFromLastSpike
                );
            }

            GetGun().OnFire();
            PlayAnimation(GetGun().fireClip);
            PlayCameraShake(GetGun().cameraShake);

            if (GetGun().recoilPattern != null)
            {
                float aimRatio = IsAiming() ? GetGun().recoilPattern.aimRatio : 1f;
                float hRecoil = Random.Range(GetGun().recoilPattern.horizontalVariation.x, GetGun().recoilPattern.horizontalVariation.y);
                _controllerRecoil += new Vector2(hRecoil, _recoilStep) * aimRatio;
            }

            if (recoilComponent == null || GetGun().weaponAsset.recoilData == null)
            {
                return;
            }

            recoilComponent.Play();

            if (recoilComponent.fireMode == FireMode.Burst)
            {
                if (_bursts == 0)
                {
                    OnFireReleased();
                    return;
                }
                _bursts--;
            }

            if (recoilComponent.fireMode == FireMode.Semi)
            {
                _isFiring = false;
                return;
            }

            _recoilStep += GetGun().recoilPattern.acceleration;
        }



        private void CalculateMissAngleAndRelated(
        Transform muzzleT,
        Transform enemyHeadT,
        SphereCollider sc,
        out float missAngle,
        out float angularRadiusDeg
)
        {
            Vector3 worldCenter = enemyHeadT.TransformPoint(sc.center);
            float worldRadius = sc.radius * enemyHeadT.lossyScale.x;
            Vector3 origin = muzzleT.position;
            Vector3 toCenter = worldCenter - origin;
            float distToCenter = toCenter.magnitude;
            Vector3 dirToCenter = toCenter / distToCenter;

            float centerAngle = Vector3.Angle(muzzleT.forward, dirToCenter);
            angularRadiusDeg = Mathf.Asin(worldRadius / distToCenter) * Mathf.Rad2Deg;
            missAngle = Mathf.Max(0f, centerAngle - angularRadiusDeg);
        }

        // Helper method for miss angle calculation
        void CalculateMissAndAngles(
        Transform muzzleT,
        Transform scT,
        SphereCollider sc,
        Camera playerCamera,
        float timeTillLastKill,
        out float missAngle,
        out float missAngleFromCenter,
        out float horizontalMissAngle,
        out float verticalMissAngle,
        out float angularRadiusDeg,
        out bool enemyInView,
        out bool enemyIsInFront,
        out bool validMiss,
        out float surfaceDistance,
        out float worldRadiusRequired,
        out float extraRadiusWorld,
        out float localRadiusRequired,
        out float worldRadius
    )
        {
            Vector3 muzzlePos = muzzleT.position;
            Vector3 muzzleForward = muzzleT.forward;
            Vector3 headCenterWorld = scT.TransformPoint(sc.center);
            Vector3 toHead = headCenterWorld - muzzlePos;
            float distance = toHead.magnitude;
            Vector3 dirToHead = toHead / distance;


            missAngleFromCenter = Vector3.Angle(muzzleForward, dirToHead);

            float scale = Mathf.Max(scT.lossyScale.x, scT.lossyScale.y, scT.lossyScale.z);
            worldRadius = sc.radius * scale;


            angularRadiusDeg = Mathf.Rad2Deg * Mathf.Asin(worldRadius / distance);


            missAngle = Mathf.Max(0f, missAngleFromCenter - angularRadiusDeg);

            horizontalMissAngle = Vector3.SignedAngle(muzzleForward, dirToHead, Vector3.up);
            verticalMissAngle = Vector3.SignedAngle(muzzleForward, dirToHead, muzzleT.right);

            surfaceDistance = Mathf.Max(0, distance - worldRadius);

            // ---
            worldRadiusRequired = distance * Mathf.Tan(missAngleFromCenter * Mathf.Deg2Rad);
            localRadiusRequired = worldRadiusRequired / scale;
            extraRadiusWorld = Mathf.Max(0, worldRadiusRequired - worldRadius);

            // --- Viewport and front 
            Vector3 vp = playerCamera.WorldToViewportPoint(headCenterWorld);
            enemyInView = (vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f);

            float dot = Vector3.Dot(muzzleForward.normalized, dirToHead);
            enemyIsInFront = dot > 0.1f;

            // --- Valid miss
            validMiss = enemyInView && enemyIsInFront && timeTillLastKill >= 0.3f;
        }







        private void OnFirePressed()
        {
            if (weapons.Count == 0 || HasActiveAction()) return;

            if (Mathf.Approximately(GetGun().fireRate, 0f))
            {
                return;
            }

            if (Time.unscaledTime - _lastRecoilTime < 60f / GetGun().fireRate)
            {
                return;
            }

            _lastRecoilTime = Time.unscaledTime;
            _bursts = GetGun().burstAmount - 1;

            if (GetGun().recoilPattern != null)
            {
                _recoilStep = GetGun().recoilPattern.step;
            }

            _isFiring = true;
            Fire();
        }

        public Weapon GetGun()
        {
            if (weapons.Count == 0) return null;

            return weapons[_currentWeaponIndex];
        }

        private void OnFireReleased()
        {
            if (weapons.Count == 0) return;

            if (recoilComponent != null)
            {
                recoilComponent.Stop();
            }

            _recoilStep = 0f;
            _isFiring = false;
            //CancelInvoke(nameof(Fire));
        }

        private void OnMoveStarted()
        {
            if (slotLayer != null)
            {
                slotLayer.PlayMotion(onStartStopMoving);
            }

            if (movementComponent.PoseState == FPSPoseState.Prone)
            {
                locoLayer.BlendInIkPose(pronePose);
            }
        }

        private void OnMoveEnded()
        {
            if (slotLayer != null)
            {
                slotLayer.PlayMotion(onStartStopMoving);
            }

            if (movementComponent.PoseState == FPSPoseState.Prone)
            {
                locoLayer.BlendOutIkPose();
            }
        }

        private void OnSlideStarted()
        {
            lookLayer.SetLayerAlpha(0.3f);
            GetAnimGraph().GetBaseAnimator().CrossFade("Sliding", 0.1f);
        }

        private void OnSlideEnded()
        {
            lookLayer.SetLayerAlpha(1f);
        }

        private void OnSprintStarted()
        {
            OnFireReleased();
            DisableAim();

            lookLayer.SetLayerAlpha(0.5f);
            collisionLayer.SetLayerAlpha(0f);

            if (GetGun().overlayType == Runtime.OverlayType.Rifle)
            {
                locoLayer.BlendInIkPose(sprintPose);

            }

            aimState = FPSAimState.None;

            if (recoilComponent != null)
            {
                recoilComponent.Stop();
            }
        }

        private void OnSprintEnded()
        {
            lookLayer.SetLayerAlpha(1f);
            adsLayer.SetLayerAlpha(1f);
            locoLayer.BlendOutIkPose();
            collisionLayer.SetLayerAlpha(1f);
        }

        private void OnCrouch()
        {
            lookLayer.SetPelvisWeight(0f);
            animator.SetBool(Crouching, true);
            slotLayer.PlayMotion(crouchMotionAsset);

            GetAnimGraph().GetFirstPersonAnimator().SetFloat("MovementPlayRate", .7f);
        }

        private void OnUncrouch()
        {
            lookLayer.SetPelvisWeight(1f);
            animator.SetBool(Crouching, false);
            slotLayer.PlayMotion(unCrouchMotionAsset);

            GetAnimGraph().GetFirstPersonAnimator().SetFloat("MovementPlayRate", 1f);
        }

        private void OnJump()
        {
            slotLayer.PlayMotion(onJumpMotionAsset);
        }

        private void OnLand()
        {
            slotLayer.PlayMotion(onLandedMotionAsset);
        }

        private void TryReload(bool isAuto)
        {
            if (HasActiveAction()) return;

            if (GetGun().currentAmmoCount >= GetGun().magSize)
                return;

            GetGun().currentAmmoCount = GetGun().magSize;
            reloadCountPerRound++;
            if (!isAuto)
                tacticalReloadCountPerRound++;
            if (isReloadSpikeEnabled)
            {
                gameManager.isEventBasedDelay = true;
                perRoundReloadSpikeCount++;
            }

            var reloadClip = GetGun().reloadClip;

            if (reloadClip == null) return;

            OnFireReleased();

            PlayAnimation(reloadClip);
            GetGun().Reload();
            actionState = FPSActionState.Reloading;
        }

        private void TryGrenadeThrow()
        {
            if (HasActiveAction()) return;
            if (GetGun().grenadeClip == null) return;

            OnFireReleased();
            DisableAim();
            PlayAnimation(GetGun().grenadeClip);
            actionState = FPSActionState.Reloading;
        }

        private bool _isLeaning;

        private void ChangeWeapon_Internal(int newIndex)
        {
            if (newIndex == _currentWeaponIndex || newIndex > weapons.Count - 1)
            {
                return;
            }

            _lastIndex = _currentWeaponIndex;
            _currentWeaponIndex = newIndex;

            OnFireReleased();

            UnequipWeapon();
            Invoke(nameof(EquipWeapon), equipDelay);
        }

        private void HandleWeaponChangeInput()
        {
            if (movementComponent.PoseState == FPSPoseState.Prone) return;
            if (HasActiveAction() || weapons.Count == 0) return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                //ChangeWeapon_Internal(_currentWeaponIndex + 1 > weapons.Count - 1 ? 0 : _currentWeaponIndex + 1);
                return;
            }

            for (int i = (int)KeyCode.Alpha1; i <= (int)KeyCode.Alpha9; i++)
            {
                if (Input.GetKeyDown((KeyCode)i))
                {
                    //ChangeWeapon_Internal(i - (int)KeyCode.Alpha1); //weapon change blocked
                }
            }
        }

        private void UpdateActionInput()
        {
            if (movementComponent.MovementState == FPSMovementState.Sprinting)
            {
                return;
            }

            if (!isPlayerReady || !isQoeDisabled || !isAcceptabilityDisabled) return;

            if (Input.GetKeyDown(KeyCode.R))
            {
                //TryReload(false); // RELOEAD DISABLED

            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                //TryGrenadeThrow();
            }

            HandleWeaponChangeInput();

            if (aimState != FPSAimState.Ready)
            {
                bool wasLeaning = _isLeaning;



                bool rightLean = Input.GetKey(KeyCode.E);
                bool leftLean = Input.GetKey(KeyCode.Q);

                //_isLeaning = rightLean || leftLean; // LEANING OFF

                if (_isLeaning != wasLeaning)
                {
                    slotLayer.PlayMotion(leanMotionAsset);
                    charAnimData.SetLeanInput(wasLeaning ? 0f : rightLean ? -startLean : startLean);
                }

                if (_isLeaning)
                {
                    float leanValue = Input.GetAxis("Mouse ScrollWheel") * smoothLeanStep;
                    charAnimData.AddLeanInput(leanValue);
                }

                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    OnFirePressed();
                    if (clickToPhotonIMG.isActiveAndEnabled)
                        clickToPhotonIMG.color = new UnityEngine.Color(.8f, .8f, .8f, 1f);
                }

                if (Input.GetKeyUp(KeyCode.Mouse0))
                {
                    OnFireReleased();
                    if (clickToPhotonIMG.isActiveAndEnabled)
                        clickToPhotonIMG.color = new UnityEngine.Color(.2f, .2f, .2f, 1f);
                }

                if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    //StartAiming(); //ADS OFF
                }
                if (Input.GetKeyUp(KeyCode.Mouse1))
                {
                    //StopAiming();
                }

                if (Input.GetKeyDown(KeyCode.V))
                {
                    //ChangeScope(); // SIDESCOPING OFF
                }

                if (Input.GetKeyDown(KeyCode.B) && IsAiming())
                {
                    if (aimState == FPSAimState.PointAiming)
                    {
                        adsLayer.SetPointAim(false);
                        aimState = FPSAimState.Aiming;
                    }
                    else
                    {
                        adsLayer.SetPointAim(true);
                        aimState = FPSAimState.PointAiming;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                if (aimState == FPSAimState.Ready)
                {
                    aimState = FPSAimState.None;
                    lookLayer.SetLayerAlpha(1f);
                }
                else
                {
                    aimState = FPSAimState.Ready;
                    lookLayer.SetLayerAlpha(.5f);
                    OnFireReleased();
                }
            }
        }

        private Quaternion desiredRotation;
        private Quaternion moveRotation;
        private float turnProgress = 1f;
        private bool isTurning = false;

        private void TurnInPlace()
        {
            float turnInput = _playerInput.x;
            _playerInput.x = Mathf.Clamp(_playerInput.x, -90f, 90f);
            turnInput -= _playerInput.x;

            //Debug.Log("Player input x:" + _playerInput.x);

            float sign = Mathf.Sign(_playerInput.x);
            if (Mathf.Abs(_playerInput.x) > turnInPlaceAngle)
            {
                if (!isTurning)
                {
                    turnProgress = 0f;

                    animator.ResetTrigger(TurnRight);
                    animator.ResetTrigger(TurnLeft);

                    animator.SetTrigger(sign > 0f ? TurnRight : TurnLeft);
                }

                isTurning = true;
            }
            isTurning = true;
            transform.rotation *= Quaternion.Euler(0f, turnInput, 0f);

            float lastProgress = turnCurve.Evaluate(turnProgress);
            turnProgress += Time.deltaTime * turnSpeed;
            turnProgress = Mathf.Min(turnProgress, 1f);

            float deltaProgress = turnCurve.Evaluate(turnProgress) - lastProgress;

            _playerInput.x -= sign * turnInPlaceAngle * deltaProgress;

            transform.rotation *= Quaternion.Slerp(Quaternion.identity,
                Quaternion.Euler(0f, sign * turnInPlaceAngle, 0f), deltaProgress);

            if (Mathf.Approximately(turnProgress, 1f) && isTurning)
            {
                isTurning = false;
            }
        }

        private float _jumpState = 0f;

        private void UpdateLookInput()
        {
            deltaMouseX = 0;
            deltaMouseY = 0;

            muzzlePointTransformGO = GetGun().shootPoint;

            if (isPlayerReady && isQoeDisabled && isAcceptabilityDisabled)
            {
                //_freeLook = Input.GetKey(KeyCode.X);

                deltaMouseX = Input.GetAxis("Mouse X") * sensitivity;
                deltaMouseY = -Input.GetAxis("Mouse Y") * sensitivity;
                if (aimAssistEnabled && currentEnemyHead != null)
                {
                    Transform muzzlePointTransform = GetGun().shootPoint.transform;

                    Vector3 toEnemy = currentEnemyHead.transform.position - muzzlePointTransform.position;
                    Vector3 enemyDir = toEnemy.normalized;
                    Vector3 localDir = muzzlePointTransform.InverseTransformDirection(enemyDir);

                    float errorHorizontal = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                    float errorVertical = Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;


                    if (Mathf.Abs(errorHorizontal) > 0.5f)
                        deltaMouseX += errorHorizontal * aimAssistStrength * Time.deltaTime;
                    if (Mathf.Abs(errorVertical) > 0.5f)
                        deltaMouseY -= errorVertical * aimAssistStrength * Time.deltaTime;

                }

                delXCumilative += Mathf.Abs(deltaMouseX);
                delYCumilative += Mathf.Abs(deltaMouseY);

                mouseMovedXPerEvent = Mathf.Abs(deltaMouseX);
                mouseMovedYPerEvent = Mathf.Abs(deltaMouseY);

                if (!targetMarked)
                {
                    degreeToTargetX += Mathf.Abs(deltaMouseX);
                    degreeToTargetY += Mathf.Abs(deltaMouseY);
                    timeToTargetEnemy += Time.deltaTime;
                }
                if (!targetShot)
                {
                    degreeToShootX += Mathf.Abs(deltaMouseX);
                    degreeToShootY += Mathf.Abs(deltaMouseY);
                    timeToHitEnemy += Time.deltaTime;
                }
                timeToKillEnemy += Time.deltaTime;

            }

            if (Mathf.Abs(deltaMouseX) > mouseSpikeDegreeOfMovement && isMouseMovementSpikeEnabled)
            {
                if (mouseSpikeCooldown <= 0)
                {
                    gameManager.isEventBasedDelay = true;
                    mouseSpikeCooldown = mouseSpikeDelay;
                    perRoundMouseMovementSpikeCount++;
                }
            }

            if (_freeLook)
            {
                // No input for both controller and animation component. We only want to rotate the camera
                _freeLookInput.x += deltaMouseX;
                _freeLookInput.y += deltaMouseY;

                _freeLookInput.x = Mathf.Clamp(_freeLookInput.x, -freeLookAngle.x, freeLookAngle.x);
                _freeLookInput.y = Mathf.Clamp(_freeLookInput.y, -freeLookAngle.y, freeLookAngle.y);

                return;
            }

            _freeLookInput = Vector2.Lerp(_freeLookInput, Vector2.zero,
                FPSAnimLib.ExpDecayAlpha(15f, Time.deltaTime));

            _playerInput.x += deltaMouseX;
            _playerInput.y += deltaMouseY;

            float proneWeight = animator.GetFloat("ProneWeight");
            Vector2 pitchClamp = Vector2.Lerp(new Vector2(-90f, 90f), new Vector2(-30, 0f), proneWeight);

            _playerInput.y = Mathf.Clamp(_playerInput.y, pitchClamp.x, pitchClamp.y);
            moveRotation *= Quaternion.Euler(0f, deltaMouseX, 0f);

            TurnInPlace();

            _jumpState = Mathf.Lerp(_jumpState, movementComponent.IsInAir() ? 1f : 0f,
                FPSAnimLib.ExpDecayAlpha(10f, Time.deltaTime));

            float moveWeight = Mathf.Clamp01(movementComponent.AnimatorVelocity.magnitude);
            transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, moveWeight);
            transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, _jumpState);
            _playerInput.x *= 1f - moveWeight;
            _playerInput.x *= 1f - _jumpState;

            charAnimData.SetAimInput(_playerInput);
            charAnimData.AddDeltaInput(new Vector2(deltaMouseX, charAnimData.deltaAimInput.y));
        }

        private Quaternion lastRotation;
        private Vector2 _cameraRecoilOffset;

        private void UpdateRecoil()
        {
            if (Mathf.Approximately(_controllerRecoil.magnitude, 0f)
                && Mathf.Approximately(_cameraRecoilOffset.magnitude, 0f))
            {
                return;
            }

            float smoothing = 8f;
            float restoreSpeed = 8f;
            float cameraWeight = 0f;

            if (GetGun().recoilPattern != null)
            {
                smoothing = GetGun().recoilPattern.smoothing;
                restoreSpeed = GetGun().recoilPattern.cameraRestoreSpeed;
                cameraWeight = GetGun().recoilPattern.cameraWeight;
            }

            _controllerRecoil = Vector2.Lerp(_controllerRecoil, Vector2.zero,
                FPSAnimLib.ExpDecayAlpha(smoothing, Time.deltaTime));

            _playerInput += _controllerRecoil * Time.deltaTime;

            Vector2 clamp = Vector2.Lerp(Vector2.zero, new Vector2(90f, 90f), cameraWeight);
            _cameraRecoilOffset -= _controllerRecoil * Time.deltaTime;
            _cameraRecoilOffset = Vector2.ClampMagnitude(_cameraRecoilOffset, clamp.magnitude);

            if (_isFiring) return;

            _cameraRecoilOffset = Vector2.Lerp(_cameraRecoilOffset, Vector2.zero,
                FPSAnimLib.ExpDecayAlpha(restoreSpeed, Time.deltaTime));
        }

        public void OnWarpStarted()
        {
            movementComponent.enabled = false;
            GetComponent<CharacterController>().enabled = false;
        }

        public void OnWarpEnded()
        {
            movementComponent.enabled = true;
            GetComponent<CharacterController>().enabled = true;
        }

        private void Update()
        {

            //Debug.DrawRay(GetGun().shootPoint.transform.position, GetGun().shootPoint.transform.forward * 5, Color.red);

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                isPlayerReady = true;
            }

            if (!isQoeDisabled || !isAcceptabilityDisabled)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (!isPlayerReady || !isQoeDisabled || !isAcceptabilityDisabled)
            {
                //OnSprintEnded();
                UpdateAnimController();
                OnFireReleased();
                StopAiming();
                UpdateCooldowns();
                UpdateActionInput();
                UpdateLookInput();
                OnSprintStarted();

                //Time.timeScale = 0.0f;
                return;
            }
            //Time.timeScale = 1.0f;
            UpdateReticle();
            UpdateActionInput();
            UpdateLookInput();
            UpdateRecoil();


            if (_isFiring)
            {
                fireTimer -= Time.deltaTime;

                isFiringDurationPerRound += Time.deltaTime;

                if (fireTimer < 0)
                {
                    Fire();
                }
            }
            else if (fireTimer > 0)
            {
                fireTimer -= Time.deltaTime;
            }

            charAnimData.moveInput = movementComponent.AnimatorVelocity;
            UpdateAnimController();
            UpdateCooldowns();

            if (deathTimeOut > 0)
            {
                deathTimeOut -= Time.deltaTime;
            }

            Vector3 distanceVector = transform.position - oldPosition;

            if (distanceVector.magnitude < 1)
                distanceTravelledPerRound += distanceVector.magnitude;
            oldPosition = transform.position;

            if (aimState == FPSAimState.Aiming)
            {
                aimDurationPerRound += Time.deltaTime;
            }
        }



        public void UpdatePlayerLog(double deltaTime)
        {
            GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");


            playerTickLog.time.Add(System.DateTime.Now.ToString());
            playerTickLog.mouseX.Add(deltaMouseX);
            playerTickLog.mouseY.Add(deltaMouseY);

            playerTickLog.playerX.Add(transform.position.x);
            playerTickLog.playerY.Add(transform.position.y);
            playerTickLog.playerZ.Add(transform.position.z);

            playerTickLog.scorePerSec.Add(score / (roundManager.roundDuration - roundManager.roundTimer));

            playerTickLog.roundTimer.Add((roundManager.roundDuration - roundManager.roundTimer));

            playerTickLog.playerRot.Add(this.transform.rotation);
            if (enemy != null)
                playerTickLog.enemyPos.Add(enemy.transform.position);
            else
                playerTickLog.enemyPos.Add(new Vector3(0, 0, 0));
            playerTickLog.isADS.Add(IsAiming());

            playerTickLog.frameTimeMS.Add(deltaTime);

            playerTickLog.muzzleX.Add(muzzlePointTransformGO.transform.position.x);
            playerTickLog.muzzleY.Add(muzzlePointTransformGO.transform.position.y);
            playerTickLog.muzzleZ.Add(muzzlePointTransformGO.transform.position.z);

            playerTickLog.targetAngleToEnemy.Add(missAnglePublic);
            playerTickLog.validMissContinious.Add(validMissPublic);

            playerTickLog.playerOnTarget.Add(playerOnTarget);
            playerTickLog.onTargetDuration.Add(onTargetDuration);
            //Debug.Log("TICK: " + Time.deltaTime * 1000.0);

            //Debug.Log("Aim: " + degreeToTargetX + "  " +targetMarked);
            //Debug.Log("Shoot: " + degreeToShootX + "  " + targetShot);
        }

        public void UpdateShotLog(float distanceToPlayer, float missAngle, bool isHit, float neededWorldRadius, float extraRadiusWorld,
            float neededLocalRadius, float currentWorldRadius, float currentLocalRadius,
            float currentAccuracy, long currentScore, float currentMouseSpeed, float avgMouseSpeed, bool enemyInView, float timeTillLastKill, bool validMiss,
            bool isFirstShotAfterSpike, int shotCountAfterSpike, int postSpikeFirstShotHits, int postSpikeFirstShotMisses, float postSpikeFirstShotAccuracy, float elapsedTimeFromLastSpike)
        {
            GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");
            missAnglePublic = missAngle;

            perShotLog.time.Add(System.DateTime.Now.ToString());
            perShotLog.mouseX.Add(deltaMouseX);
            perShotLog.mouseY.Add(deltaMouseY);

            perShotLog.playerX.Add(transform.position.x);
            perShotLog.playerY.Add(transform.position.y);
            perShotLog.playerZ.Add(transform.position.z);

            perShotLog.roundTimer.Add((roundManager.roundDuration - roundManager.roundTimer));

            perShotLog.playerRot.Add(this.transform.rotation);
            if (enemy != null)
                perShotLog.enemyPos.Add(enemy.transform.position);
            else
                perShotLog.enemyPos.Add(new Vector3(0, 0, 0));
            perShotLog.isADS.Add(IsAiming());

            perShotLog.isHit.Add(isHit);
            perShotLog.distanceToPlayer.Add(distanceToPlayer);
            perShotLog.missAngle.Add(missAngle);


            perShotLog.neededWorldRadius.Add(neededWorldRadius);
            perShotLog.extraRadiusWorld.Add(extraRadiusWorld);
            perShotLog.neededLocalRadius.Add(neededLocalRadius);
            perShotLog.currentWorldRadius.Add(currentWorldRadius);
            perShotLog.currentLocalRadius.Add(currentLocalRadius);

            perShotLog.currentAccuracy.Add(currentAccuracy);
            perShotLog.currentScore.Add(currentScore);

            perShotLog.avgMouseSpeed.Add(avgMouseSpeed);
            perShotLog.currentMouseSpeed.Add(currentMouseSpeed);
            perShotLog.enemyInView.Add(enemyInView);
            perShotLog.timeTillLastKill.Add(timeTillLastKill);
            perShotLog.validMiss.Add(validMiss);


            perShotLog.isFirstShotAfterSpike.Add(isFirstShotAfterSpike);
            perShotLog.shotCountAfterSpike.Add(shotCountAfterSpike);
            perShotLog.postSpikeFirstShotHits.Add(postSpikeFirstShotHits);
            perShotLog.postSpikeFirstShotMisses.Add(postSpikeFirstShotMisses);
            perShotLog.postSpikeFirstShotAccuracy.Add(postSpikeFirstShotAccuracy);
            perShotLog.elapsedTimeFromLastSpike.Add(elapsedTimeFromLastSpike);

            perShotLog.horizontalMissAngle.Add(horizontalMissAnglePublic);
            perShotLog.verticalMissAngle.Add(verticalMissAnglePublic);
            perShotLog.isEnemyInFront.Add(enemyIsInFrontPublic);

            perShotLog.elapsedTimeSinceLastEventExcludingSpike.Add(elapsedTimeSinceLastEventExcludingSpike);
            perShotLog.elapsedTimeSinceLastEventIncludingSpike.Add(elapsedTimeSinceLastEventIncludingSpike);

            //Debug.Log("TICK: " + Time.deltaTime * 1000.0);

            //Debug.Log("Aim: " + degreeToTargetX + "  " +targetMarked);
            //Debug.Log("Shoot: " + degreeToShootX + "  " + targetShot);


        }

        void UpdateCooldowns()
        {
            if (killCooldown > 0)
                killCooldown -= Time.deltaTime;

            if (regularHitCooldown > 0)
                regularHitCooldown -= Time.deltaTime;

            if (headshotCooldown > 0)
                headshotCooldown -= Time.deltaTime;

            if (GetGun().currentAmmoCount <= 0)
                TryReload(true);

            if (mouseSpikeCooldown > 0)
                mouseSpikeCooldown -= Time.deltaTime;

            if (aimSpikeCooldown > 0)
                aimSpikeCooldown -= Time.deltaTime;

            if (shotsFiredPerRound > 0)
                liveAccuracy = (float)shotsHitPerRound / (float)shotsFiredPerRound;
            else
                liveAccuracy = 0;

            if (Time.deltaTime < 0.05)
            {
                timeTillLastKill += Time.deltaTime;
                elapsedTimeFromLastSpike += Time.deltaTime;
                elapsedTimeSinceLastEventExcludingSpike += Time.deltaTime;
            }
            else
            {
                timeTillLastKill += 0.002f;
                elapsedTimeFromLastSpike += 0.002f;
                elapsedTimeSinceLastEventExcludingSpike += 0.002f;
            }
            elapsedTimeSinceLastEventIncludingSpike += Time.deltaTime;
        }

        public void UpdateCameraRotation()
        {
            Vector2 input = _playerInput;
            input += _cameraRecoilOffset;

            (Quaternion, Vector3) cameraTransform =
                (transform.rotation * Quaternion.Euler(input.y, input.x, 0f),
                    firstPersonCamera.position);

            cameraHolder.rotation = cameraTransform.Item1;
            cameraHolder.position = cameraTransform.Item2;

            mainCamera.rotation = cameraHolder.rotation * Quaternion.Euler(_freeLookInput.y, _freeLookInput.x, 0f);
        }
        public void UpdateReticle()
        {
            Transform muzzlePointTransform = GetGun().shootPoint.transform;
            Vector3 targetPoint = muzzlePointTransform.position + muzzlePointTransform.TransformDirection(Vector3.forward);
            Vector3 directionWithoutSpread = targetPoint - muzzlePointTransform.position;

            // --- Attribute Scaling & Per-frame miss angle + valid miss calculation ---
            if (currentEnemyHead != null)
            {
                roundManager.attributeScalingModule.enemyCollider = currentEnemyHead.GetComponent<SphereCollider>();

                SphereCollider sc = currentEnemyHead.GetComponent<SphereCollider>();
                Transform scT = sc.transform;

                float missAngle, horizontalMissAngle, verticalMissAngle, angularRadiusDeg, missAngleFromCenter;
                bool enemyInView, enemyIsInFront, validMiss;
                float surfaceDistance, worldRadiusRequired, extraRadiusWorld, localRadiusRequired, worldRadius;

                CalculateMissAndAngles(
                muzzlePointTransform,
                scT,
                sc,
                playerCamera,
                timeTillLastKill,
                out missAngle,              // Surface-based miss angle (new)
                out missAngleFromCenter,    // Center-to-center miss angle (old, required by new function)
                out horizontalMissAngle,
                out verticalMissAngle,
                out angularRadiusDeg,
                out enemyInView,
                out enemyIsInFront,
                out validMiss,
                out surfaceDistance,
                out worldRadiusRequired,
                out extraRadiusWorld,
                out localRadiusRequired,
                out worldRadius
            );

                missAnglePublic = missAngle;
                horizontalMissAnglePublic = horizontalMissAngle;
                verticalMissAnglePublic = verticalMissAngle;
                enemyIsInFrontPublic = enemyIsInFront;
                validMissPublic = validMiss;

                worldRadRequiredPublic = worldRadiusRequired;
                localRadRequiredPublic = localRadiusRequired;
                extraRadRequiredPublic = extraRadiusWorld;
                distanceFromEnemyPublic = surfaceDistance;

                worldRadiusPublic = worldRadius;
            }
            else
            {
                missAnglePublic = -999f;
                horizontalMissAnglePublic = 0f;
                verticalMissAnglePublic = 0f;
                enemyIsInFrontPublic = false;
                validMissPublic = false;
            }

            // --- Raycast to set reticle visuals ---
            if (Physics.Raycast(muzzlePointTransform.position, directionWithoutSpread, out hit, Mathf.Infinity, ~enemyLargeColliderLayer))
            {
                if (reticleObject != null)
                {
                    // Only set reticle active, don't update its position or scale
                    reticleObject.gameObject.SetActive(true);
                }
            }
            else
            {
                if (reticleObject != null)
                    reticleObject.gameObject.SetActive(false);
                enemyAimedFuse = false;
            }

            // --- Aim spike trigger (unchanged) ---
            RaycastHit largeColliderHit;
            if (isAimSpikeEnabled && Physics.Raycast(muzzlePointTransform.position, directionWithoutSpread, out largeColliderHit, Mathf.Infinity, enemyLargeColliderLayer))
            {
                ///PREVIOUS COOLDOWN STUFF
                /*if (!enemyAimedFuse && aimSpikeCooldown <= 0)
                {
                    gameManager.isEventBasedDelay = true;
                    perRoundAimSpikeCount++;
                    aimSpikeCooldown = aimSpikeDelay;
                    shotCountAfterSpike = 0;
                    elapsedTimeFromLastSpike = 0;
                    spikeCountPerEvent++;
                }*/


                if (!enemyAimedFuse && readyToInduceSpike)
                {
                    gameManager.isEventBasedDelay = true;
                    perRoundAimSpikeCount++;
                    //aimSpikeCooldown = aimSpikeDelay;
                    readyToInduceSpike = false;
                    shotCountAfterSpike = 0;
                    elapsedTimeFromLastSpike = 0;
                    spikeCountPerEvent++;
                }

                enemyAimedFuse = true;
            }
            else
            {
                enemyAimedFuse = false;
            }

            
            if (hit.collider != null && hit.collider.gameObject.name == "Head")
            {
                targetMarked = true;
                playerOnTarget = true;
                if (Time.deltaTime <= 0.05)
                {
                    onTargetDuration += Time.deltaTime;
                    timeOnTargetEachEnemy += Time.deltaTime;
                    timeOnTargetPerEvent += Time.deltaTime;
                }
                else
                {
                    onTargetDuration += 0.002f;
                    timeOnTargetEachEnemy += 0.002f;
                    timeOnTargetPerEvent += 0.002f;
                }
            }
            else
            {
                playerOnTarget = false;
            }
            if (Time.deltaTime <= 0.05)
            {
                durationOfEvent += Time.deltaTime;
            }
            else
            {
                durationOfEvent += 0.002f;
            }

            durationOfEventWithSpike += Time.deltaTime;

        }



        void PlayWeaponChangeSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(0.1f, 0.3f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);

            playerAudioSource.PlayOneShot(weaponChangeSFX);
        }

        void PlayADSInSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(0.15f, 0.2f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.9f, 1.05f);

            playerAudioSource.PlayOneShot(ADSInSFX);
        }

        void PlayADSOutSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(0.15f, 0.2f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);

            playerAudioSource.PlayOneShot(ADSOutSFX);
        }

        void PlayHitRegSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(1f, 1.2f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);

            playerAudioSource.PlayOneShot(hitRegSFX);
        }

        public void PlayKillSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(1f, 1.2f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);

            playerAudioSource.PlayOneShot(killSFX);
            targetShot = false;
        }

        void PlayHeadshotSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(1f, 1.2f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);

            playerAudioSource.PlayOneShot(headshotSFX);
        }

        public void PlayDeathSFX()
        {
            playerAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            playerAudioSource.PlayOneShot(deathSFX);
        }

        public void RespawnPlayer()
        {
            roundDeaths++;
            score += onDeathScore;

            movementComponent.enabled = false;
            GetComponent<CharacterController>().enabled = false;
            this.transform.position = playerSpawnPoint.position;
            GetComponent<CharacterController>().enabled = true;
            movementComponent.enabled = true;

            enemyManager.DestroyAllEnemy();
            GetGun().currentAmmoCount = GetGun().magSize;

            deathTimeOut = 1.5f;
            targetMarked = false;
            targetShot = false;
        }

        public void ResetPlayerAndDestroyEnemy()
        {
            movementComponent.enabled = false;
            GetComponent<CharacterController>().enabled = false;
            this.transform.position = playerSpawnPoint.position;
            GetComponent<CharacterController>().enabled = true;
            movementComponent.enabled = true;

            enemyManager.DestroyAllEnemy();
        }

        public void ResetRound()
        {
            GetGun().currentAmmoCount = GetGun().magSize;

            score = 0;
            roundKills = 0;
            roundDeaths = 0;
            distanceTravelledPerRound = 0;
            shotsFiredPerRound = 0;
            shotsHitPerRound = 0;
            headshotsHitPerRound = 0;
            delXCumilative = 0;
            delYCumilative = 0;
            reloadCountPerRound = 0;
            tacticalReloadCountPerRound = 0;
            spikeDurationCumulative = 0;
            perRoundMouseMovementSpikeCount = 0;
            perRoundAimSpikeCount = 0;
            perRoundReloadSpikeCount = 0;
            perRoundEnemySpawnSpikeCount = 0;

            degreeToTargetXCumulative = 0;
            degreeToShootXCumulative = 0;
            minAnlgeToEnemyCumulative = 0;

            timeToTargetEnemyCumulative = 0;
            timeToHitEnemyCumulative = 0;
            timeToKillEnemyCumulative = 0;

            enemySizeCumulative = 0;
            degreeToShootX = 0;
            degreeToShootY = 0;
            degreeToTargetX = 0;
            degreeToTargetY = 0;
            aimDurationPerRound = 0;
            isFiringDurationPerRound = 0;

            targetMarked = false;
            targetShot = false;

            isFirstShotAfterSpike = false;
            shotCountAfterSpike = 0;
            postSpikeFirstShotHits = 0;
            postSpikeFirstShotMisses = 0;
            postSpikeFirstShotAccuracy = 0f;
            elapsedTimeFromLastSpike = 0f;



            playerTickLog.time.Clear();
            playerTickLog.mouseX.Clear();
            playerTickLog.mouseY.Clear();

            playerTickLog.playerX.Clear();
            playerTickLog.playerY.Clear();
            playerTickLog.playerZ.Clear();

            playerTickLog.scorePerSec.Clear();
            playerTickLog.roundTimer.Clear();

            playerTickLog.playerRot.Clear();
            playerTickLog.enemyPos.Clear();
            playerTickLog.isADS.Clear();

            playerTickLog.frameTimeMS.Clear();

            playerTickLog.muzzleX.Clear();
            playerTickLog.muzzleY.Clear();
            playerTickLog.muzzleZ.Clear();
            playerTickLog.validMissContinious.Clear();
            playerTickLog.targetAngleToEnemy.Clear();

            playerTickLog.onTargetDuration.Clear();
            playerTickLog.playerOnTarget.Clear();


            perShotLog.time.Clear();
            perShotLog.mouseX.Clear();
            perShotLog.mouseY.Clear();

            perShotLog.playerX.Clear();
            perShotLog.playerY.Clear();
            perShotLog.playerZ.Clear();

            perShotLog.roundTimer.Clear();

            perShotLog.playerRot.Clear();
            perShotLog.enemyPos.Clear();
            perShotLog.isADS.Clear();

            perShotLog.distanceToPlayer.Clear();
            perShotLog.isHit.Clear();
            perShotLog.missAngle.Clear();

            perShotLog.currentAccuracy.Clear();
            perShotLog.currentScore.Clear();

            perShotLog.currentMouseSpeed.Clear();
            perShotLog.avgMouseSpeed.Clear();

            perShotLog.currentLocalRadius.Clear();
            perShotLog.currentWorldRadius.Clear();

            perShotLog.neededLocalRadius.Clear();
            perShotLog.neededWorldRadius.Clear();
            perShotLog.extraRadiusWorld.Clear();

            perShotLog.enemyInView.Clear();
            perShotLog.timeTillLastKill.Clear();
            perShotLog.validMiss.Clear();

            perShotLog.isFirstShotAfterSpike.Clear();
            perShotLog.shotCountAfterSpike.Clear();
            perShotLog.postSpikeFirstShotHits.Clear();
            perShotLog.postSpikeFirstShotMisses.Clear();
            perShotLog.postSpikeFirstShotAccuracy.Clear();
            perShotLog.elapsedTimeFromLastSpike.Clear();

            perShotLog.horizontalMissAngle.Clear();
            perShotLog.verticalMissAngle.Clear();
            perShotLog.isEnemyInFront.Clear();


            // Shooting event log clear

            shootingEventLog.time.Clear();
            shootingEventLog.roundTimer.Clear();
            shootingEventLog.mouseX.Clear();
            shootingEventLog.mouseY.Clear();

            shootingEventLog.playerX.Clear();
            shootingEventLog.playerY.Clear();
            shootingEventLog.playerZ.Clear();

            shootingEventLog.playerRot.Clear();

            shootingEventLog.enemyPos.Clear();
            shootingEventLog.muzzlePos.Clear();

            shootingEventLog.distanceToPlayer.Clear();
            shootingEventLog.isEnemyDead.Clear();
            shootingEventLog.targetTimeOnEnemy.Clear();

            shootingEventLog.mouseMovedX.Clear();
            shootingEventLog.mouseMovedY.Clear();
            shootingEventLog.shotsFiredPerEvent.Clear();
            shootingEventLog.shotsHitPerEvent.Clear();
            shootingEventLog.shotsMissedPerEvent.Clear();

            shootingEventLog.durationOfEventNotIncludingSpikes.Clear();
            shootingEventLog.durationOfEventIncludingSpikes.Clear();
            shootingEventLog.spikeCountPerEvent.Clear();
            shootingEventLog.angularDistanceFromEnemyOnStart.Clear();

        }

        // This function calculates the angular size (in degrees) of a sphere as seen from a reference point.
        public float CalculateAngularSize(GameObject sphere, Vector3 referencePoint)
        {
            // Get the sphere's position
            Vector3 spherePosition = sphere.transform.position;

            // Calculate the sphere's diameter using its scale (assuming it's a uniform scale sphere)
            float sphereDiameter = sphere.transform.localScale.x; // Assuming the sphere is uniformly scaled

            Vector3 horizontalVector = new Vector3(spherePosition.x - referencePoint.x, 0, spherePosition.z - referencePoint.z);
            float horizontalDistanceToSphere = horizontalVector.magnitude;

            float angularSizeInRadians = 2 * Mathf.Atan(sphereDiameter / (2 * horizontalDistanceToSphere));

            float angularSizeInDegrees = angularSizeInRadians * Mathf.Rad2Deg;

            return angularSizeInDegrees;
        }

        public void UpdatePerShootingEventLog(bool isEnemyDead)
        {
            GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");

            // Time and round info
            shootingEventLog.time.Add(System.DateTime.Now.ToString());
            shootingEventLog.roundTimer.Add(roundManager.roundDuration - roundManager.roundTimer);
            shootingEventLog.mouseX.Add(deltaMouseX);
            shootingEventLog.mouseY.Add(deltaMouseY);

            // Player position and rotation
            shootingEventLog.playerX.Add(transform.position.x);
            shootingEventLog.playerY.Add(transform.position.y);
            shootingEventLog.playerZ.Add(transform.position.z);
            shootingEventLog.playerRot.Add(this.transform.rotation);

            // Enemy position
            if (enemy != null)
                shootingEventLog.enemyPos.Add(enemy.transform.position);
            else
                shootingEventLog.enemyPos.Add(new Vector3(0, 0, 0));

            // Muzzle position
            shootingEventLog.muzzlePos.Add(muzzlePointTransformGO.transform.position);

            // Distance to enemy
            if (enemy != null)
                shootingEventLog.distanceToPlayer.Add(Vector3.Distance(transform.position, enemy.transform.position));
            else
                shootingEventLog.distanceToPlayer.Add(0);

            // Mouse moved during event
            shootingEventLog.mouseMovedX.Add(mouseMovedXPerEvent);
            shootingEventLog.mouseMovedY.Add(mouseMovedYPerEvent);

            // Shot and spike data
            shootingEventLog.shotsFiredPerEvent.Add(shotsFiredPerEvent);
            shootingEventLog.shotsHitPerEvent.Add(shotsHitPerEvent);
            shootingEventLog.shotsMissedPerEvent.Add(shotsMissedPerEvent);
            shootingEventLog.durationOfEventNotIncludingSpikes.Add(durationOfEvent);
            shootingEventLog.durationOfEventIncludingSpikes.Add(durationOfEventWithSpike);
            shootingEventLog.spikeCountPerEvent.Add(spikeCountPerEvent);
            shootingEventLog.angularDistanceFromEnemyOnStart.Add(angularDistanceFromEnemyOnStart);

            // Time on target and dead status
            shootingEventLog.targetTimeOnEnemy.Add(timeOnTargetPerEvent);
            shootingEventLog.isEnemyDead.Add(isEnemyDead);

            // Reset per-event variables
            timeOnTargetPerEvent = 0;
            mouseMovedXPerEvent = 0;
            mouseMovedYPerEvent = 0;
            shotsFiredPerEvent = 0;
            shotsHitPerEvent = 0;
            shotsMissedPerEvent = 0;
            durationOfEvent = 0;
            durationOfEventWithSpike = 0;
            //angularDistanceFromEnemyOnStart = 0;
            spikeCountPerEvent = 0;
            elapsedTimeSinceLastEventExcludingSpike = 0;
            elapsedTimeSinceLastEventIncludingSpike = 0;
        }


    }
}