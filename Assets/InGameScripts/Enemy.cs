using Demo.Scripts.Runtime;
using Michsky.UI.Heat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;

public enum EnemyMovementMode
{
    Follow,
    Arc,
    TeleportInView
}

public class Enemy : MonoBehaviour
{
    GameObject player;
    public FPSController playerController;
    NavMeshAgent enemyAgent;

    public float maxHealth;
    float currentHealth;

    public ParticleSystem deathPE;
    public ParticleSystem explodePE;
    public SphereCollider largeCollider;
    public Transform headTransform;
    public float minAngleToPlayer;
    public GameObject manager;
    public GameObject enemyHead;
    public float angularSizeOnSpawn;
    public SphereCollider headCollider;

    public EnemyManager enemyManager;

    [Header("Movement Mode")]
    public EnemyMovementMode movementMode = EnemyMovementMode.Follow;

    [Header("Arc (Orbit) Settings")]
    public float arcRadius = 7f;
    public float arcBaseSpeed = 40f; // degrees/sec
    public float arcSpeedRandomness = 0.6f; // 0=no change, 1=lots of change
    public bool arcClockwise = true;
    public float arcHeightOffset = 0f; // Y offset relative to player
    public float arcYBobStrength = 2.5f; // Amplitude of up-down bobbing
    public float arcYBobSpeed = 0.7f; // Speed of up-down bobbing (0.5-2 recommended)
    public float arcChangeDirCooldown = 1.0f; // Time in seconds between possible direction changes

    private float arcAngle; // in degrees
    private float perlinSeedY, perlinSeedSpeed;
    private Camera mainCamera;
    private float lastDirChangeTime = -100f;

    // --- TeleportInView (Whack-a-mole) ---
    [Header("Teleport (Whack-a-Mole) Settings")]
    public float teleportIntervalMin = 0.7f;
    public float teleportIntervalMax = 1.2f;
    [HideInInspector] public float teleportInterval = 1f; // Auto-set internally
    public float teleportRadius = 7f;
    public float teleportYMin = -1f;
    public float teleportYMax = 2f;

    public float teleportRandSuperMin = 0.5f;
    public float teleportRandSuperMax = 2.5f;

    [Range(0.1f, 1.0f)]
    public float teleportFOV = 0.7f;

    public float teleportMoveDuration = 0.5f; 

    private float teleportTimer = 0f;

    // Teleport smoothing state
    private Vector3 teleportTargetPos;
    private bool isTeleportingSmooth = false;
    private float teleportMoveElapsed = 0f;

    // Lateral move after teleport (left -> right or right -> left)
    [Header("Lateral move after teleport")]
    [Tooltip("Speed of lateral movement (world units/sec)")]
    public float lateralMoveSpeed = 3f;
    private bool lateralMoveActive = false;
    private Vector3 lateralTargetPos;
    private int lateralDirection = 1; // 1 = camera-right, -1 = camera-left

    float durationAlive;
    public FPSController fPSController;

    bool startMissAngleFuse = false;

    public AudioSource audioSource;
    public AudioClip teleportSFX;

    public bool isTeleporting;

    public GameObject invincibilityEffect;

    RoundManager roundManager;

    [Header("Unpredictable Mode")]
    public bool unpredictable = false;

    [Tooltip("Frequency multiplier for both Y bob and speed variation.")]
    [Range(0.05f, 5f)] public float unpredictableFreq = 1f;

    // internals (no sliders)
    private float lateralCenterY;          // set after each teleport
    private float avgAbsOrbitSpeed = 0f;   // running avg of |speed| for compensation




    void Start()
    {
        enemyAgent = gameObject.GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player");
        manager = GameObject.FindGameObjectWithTag("Manager");
        playerController = player.GetComponent<FPSController>();

        enemyManager = manager.GetComponent<EnemyManager>();

        roundManager = manager.GetComponent<RoundManager>();

        maxHealth = playerController.enemyHealthGlobal;
        currentHealth = maxHealth;

        headCollider = headTransform.GetComponent<SphereCollider>();

        var relativePos = this.transform.position - player.transform.position;
        var forward = player.transform.forward;
        minAngleToPlayer = Vector3.Angle(relativePos, forward);
        angularSizeOnSpawn = playerController.CalculateAngularSize(enemyHead, playerController.mainCamera.position);

        enemyAgent.speed = playerController.enemySpeedGlobal;

        player.GetComponent<FPSController>().currentEnemyHead = enemyHead;
        player.GetComponent<FPSController>().roundManager.attributeScalingModule.enemyCollider = enemyHead.GetComponent<SphereCollider>();

        // Set initial arc angle based on position
        Vector3 offset = transform.position - player.transform.position;
        arcAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        // Unique Perlin seeds for each enemy
        perlinSeedY = UnityEngine.Random.Range(0f, 1000f);
        perlinSeedSpeed = UnityEngine.Random.Range(1000f, 2000f);

        mainCamera = Camera.main;

        float teleportIntAdd = 0f;
        // Randomize the first interval

        //Non uniform random
        float min = UnityEngine.Random.Range(teleportRandSuperMin, teleportIntervalMin);
        float max = UnityEngine.Random.Range(teleportIntervalMax, teleportRandSuperMax);

        // adds delay duration if using temporal AS
            teleportIntAdd = (playerController.gameManager.delayDuration / 1000.0f) * roundManager.roundConfigs.temporalASMagnitude[roundManager.indexArray[roundManager.currentRoundNumber - 1]];

        teleportInterval = UnityEngine.Random.Range(teleportIntAdd + min, teleportIntAdd + max);

        teleportTimer = teleportInterval;
        durationAlive = 0f;


        //player.GetComponent<FPSController>().angularDistanceFromEnemyOnStart = player.GetComponent<FPSController>().missAnglePublic;

        audioSource = GetComponent<AudioSource>();
        isTeleporting = false;

        teleportInterval = UnityEngine.Random.Range(teleportIntAdd + min, teleportIntAdd + max);
        enemyManager.invincibilityTimer = enemyManager.remainingTeleportTimeFromLastEnemy;

        // Set initial teleport target and place enemy instantly
        TrySetTeleportTarget();
        if (enemyAgent.enabled) enemyAgent.enabled = false;
        transform.position = teleportTargetPos;

        // Disable first smooth teleport
        isTeleportingSmooth = false;
        teleportMoveElapsed = teleportMoveDuration; // treat it as complete
        teleportTimer = 0f; // start timing from now
        playerController.readyToInduceSpike = true;

        lateralMoveSpeed = roundManager.roundConfigs.enemyLateralMoveSpeed[roundManager.indexArray[roundManager.currentRoundNumber - 1]];
        unpredictable = !roundManager.roundConfigs.predictableEnemyMovement[roundManager.indexArray[roundManager.currentRoundNumber - 1]];

        StartLateralMovementRandomDirection();

    }

    void Update()
    {
        if (!playerController.isPlayerReady || !playerController.isQoeDisabled || !playerController.isAcceptabilityDisabled)
            return;

        if (movementMode == EnemyMovementMode.Follow)
        {
            if (!enemyAgent.enabled) enemyAgent.enabled = true;
            enemyAgent.destination = player.transform.position;
        }
        else if (movementMode == EnemyMovementMode.Arc)
        {
            if (enemyAgent.enabled) enemyAgent.enabled = false;
            ArcMove();
        }
        else if (movementMode == EnemyMovementMode.TeleportInView)
        {
            if (enemyAgent.enabled) enemyAgent.enabled = false;
            TeleportInViewMove();
        }

        if (Vector3.Distance(player.transform.position, headCollider.transform.position) < 1.75f)
        {
            // NO PLAYER DEATH ON PROXIMITY
            //Instantiate(explodePE, headTransform.position, headTransform.rotation);
            //player.GetComponent<FPSController>().PlayDeathSFX();
            //player.GetComponent<FPSController>().RespawnPlayer();
        }

        largeCollider.radius = Mathf.Lerp(0.85f, 2, Mathf.PingPong(Time.time * 2f, 1f));




        durationAlive += Time.deltaTime;

        //DEBUG VISUALIZER
        //DebugDrawEnemyHeadCollider(Color.yellow, 36);
        //DebugDrawLargeCollider(Color.red, 36);

        if (!startMissAngleFuse)
        {
            player.GetComponent<FPSController>().angularDistanceFromEnemyOnStart = player.GetComponent<FPSController>().missAnglePublic;

            if (player.GetComponent<FPSController>().angularDistanceFromEnemyOnStart != -999)
                startMissAngleFuse = true;
        }

        if(IsInvincible())
            invincibilityEffect.SetActive(true);
        else
            invincibilityEffect.SetActive(false);
    }


    void ArcMove()
    {
        float time = Time.time;
        float perlinY = Mathf.PerlinNoise(perlinSeedY, time * arcYBobSpeed);
        float yOffset = arcHeightOffset + (perlinY - 0.5f) * 2f * arcYBobStrength;

        float perlinSpeed = Mathf.PerlinNoise(perlinSeedSpeed, time * 0.4f);
        float speedMultiplier = Mathf.Lerp(1f - arcSpeedRandomness, 1f + arcSpeedRandomness, perlinSpeed);
        float deltaAngle = arcBaseSpeed * speedMultiplier * Time.deltaTime * (arcClockwise ? 1 : -1);
        arcAngle += deltaAngle;

        float radians = arcAngle * Mathf.Deg2Rad;
        Vector3 center = player.transform.position + Vector3.up * arcHeightOffset;
        Vector3 offset = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians)) * arcRadius;
        Vector3 targetPos = center + offset + Vector3.up * (yOffset - arcHeightOffset);

        if (mainCamera != null)
        {
            Vector3 viewport = mainCamera.WorldToViewportPoint(targetPos);
            bool outOfView = (viewport.z < 0) || (viewport.x < 0.07f) || (viewport.x > 0.93f) || (viewport.y < 0.07f) || (viewport.y > 0.93f);
            if (outOfView && (Time.time - lastDirChangeTime > arcChangeDirCooldown))
            {
                arcClockwise = !arcClockwise;
                lastDirChangeTime = Time.time;
            }
        }

        float lerpSpeed = 8f;
        transform.position = Vector3.Lerp(transform.position, targetPos, lerpSpeed * Time.deltaTime);

        Vector3 lookDir = (player.transform.position + Vector3.up * arcHeightOffset) - transform.position;
        lookDir.y = 0;
        if (lookDir.magnitude > 0.1f)
            transform.rotation = Quaternion.LookRotation(lookDir);
    }

    void TrySetTeleportTarget()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Stop orbiting when we start a new teleport
        lateralMoveActive = false;

        Vector3 chosenTarget = Vector3.zero;
        bool found = false;

        for (int i = 0; i < 10; i++)
        {
            float x = UnityEngine.Random.Range(0.5f - teleportFOV / 2, 0.5f + teleportFOV / 2);
            float y = UnityEngine.Random.Range(0.5f - teleportFOV / 2, 0.5f + teleportFOV / 2);

            Ray ray = mainCamera.ViewportPointToRay(new Vector3(x, y, 0f));
            Vector3 target = ray.origin + ray.direction * teleportRadius;
            float playerY = player.transform.position.y;
            target.y = playerY + UnityEngine.Random.Range(teleportYMin, teleportYMax);

            Vector3 viewport = mainCamera.WorldToViewportPoint(target);
            if (viewport.z > 0 && viewport.x > 0.05f && viewport.x < 0.95f && viewport.y > 0.05f && viewport.y < 0.95f)
            {
                chosenTarget = target;
                found = true;
                break;
            }
        }

        if (!found)
        {
            chosenTarget = player.transform.position + player.transform.forward * teleportRadius;
            chosenTarget.y = player.transform.position.y + UnityEngine.Random.Range(teleportYMin, teleportYMax);
        }

        teleportTargetPos = chosenTarget;
        isTeleportingSmooth = true;
        teleportMoveElapsed = 0f;

        float min = UnityEngine.Random.Range(teleportRandSuperMin, teleportIntervalMin);
        float max = UnityEngine.Random.Range(teleportIntervalMax, teleportRandSuperMax);

        float teleportIntAdd = playerController.gameManager.delayDuration / 1000.0f;
        teleportInterval = UnityEngine.Random.Range(teleportIntAdd + min, teleportIntAdd + max);

        PlayTeleportSFX();
    }


    /// <summary>
/// Begin continuous left/right orbit around the player after teleport.
/// Uses lateralMoveSpeed as tangential speed (world units/sec).
/// </summary>
private void StartLateralMovementRandomDirection()
{
    // Randomly pick orbit direction: -1 (left/counterclockwise) or +1 (right/clockwise)
    lateralDirection = (UnityEngine.Random.value < 0.5f) ? -1 : 1;

    // Make sure we're in a valid horizontal position relative to player
    Vector3 centerToEnemy = transform.position - player.transform.position;
    centerToEnemy.y = 0f;

    if (centerToEnemy.sqrMagnitude < 0.01f)
    {
        // If we somehow teleported exactly on the player center, nudge outward
        Vector3 outDir = player.transform.right; // arbitrary horizontal direction
        transform.position = player.transform.position + outDir * Mathf.Max(teleportRadius, 1f);
    }

    lateralMoveActive = true;
    // (No fixed target — orbiting is driven per-frame in TeleportInViewMove)
}



    public void TakeDamage(float damage)
    {
        if (isTeleporting || IsInvincible())
            return;

        currentHealth -= damage;
        if (currentHealth < 0)
        {
            fPSController = player.GetComponent<FPSController>();

            fPSController.degreeToTargetXCumulative += fPSController.degreeToTargetX;
            fPSController.degreeToShootXCumulative += fPSController.degreeToShootX;

            fPSController.timeToTargetEnemyCumulative += fPSController.timeToTargetEnemy;
            fPSController.timeToHitEnemyCumulative += fPSController.timeToHitEnemy;
            fPSController.timeToKillEnemyCumulative += fPSController.timeToKillEnemy;

            fPSController.minAnlgeToEnemyCumulative += minAngleToPlayer;
            fPSController.enemySizeCumulative += angularSizeOnSpawn;

            EnemyLog();

            fPSController.killCooldown = .3f;
            fPSController.targetMarked = false;
            fPSController.targetShot = false;
            fPSController.PlayKillSFX();
            Instantiate(deathPE, headTransform.position, headTransform.rotation);

            fPSController.score += fPSController.onKillScore;
            fPSController.roundKills++;
            fPSController.timeTillLastKill = 0f;

            float timeLeft = teleportInterval - teleportTimer;
            enemyManager.remainingTeleportTimeFromLastEnemy = Mathf.Max(0f, timeLeft);

            player.GetComponent<FPSController>().eventCount++;
            fPSController.UpdatePerShootingEventLog(true);

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log("Enemycol: " + other.gameObject.name);
    }

    public void EnemyLog()
    {
        
        FPSController fPSController = player.GetComponent<FPSController>();

        string filenameEnemyLog = "Data\\Logs\\EnemyData_" + roundManager.fileNameSuffix + "_" + roundManager.sessionID + "_" + ".csv";

        bool fileExists = File.Exists(filenameEnemyLog);

        using (StreamWriter textWriter = File.AppendText(filenameEnemyLog))
        {
            if (!fileExists)
            {
                textWriter.WriteLine(
                    "SessionID,LatinRow,RoundNumber,SessionStart,KillTimestamp,RoundFPS,SpikeMagnitude," +
                    "OnAimSpike,OnEnemySpawnSpike,OnMouseSpike,OnReloadSpike,AttributeScalingEnabled," + "AttributeScaleRadius," + "EnemyMoveSpeed," + "PredictableEnemyMovement," + "UsingTemporalAS," +
                    "RoundIndex,EnemyHealth,MinAngleToPlayer,AngularSizeOnSpawn," +
                    "DegreeToTargetX,DegreeToTargetY,DegreeToShootX,DegreeToShootY," +
                    "TimeToTargetEnemy,TimeToHitEnemy,TimeToKillEnemy," +
                    "TargetMarked,TargetShot,DurationAlive,TimeOnTargetEachEnemy"
                );
            }

            string enemyLogLine =
               roundManager.sessionID.ToString() + "," +
               roundManager.latinRow.ToString() + "," +
               roundManager.currentRoundNumber.ToString() + "," +
               roundManager.sessionStartTime.ToString() + "," +
               System.DateTime.Now.ToString("HH:mm:ss.fff") + "," +
               roundManager.roundConfigs.roundFPS[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.spikeMagnitude[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.onAimSpikeEnabled[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.onEnemySpawnSpikeEnabled[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.onMouseSpikeEnabled[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.onReloadSpikeEnabled[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.attributeScalingEnabled[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.attributeScaleRadius[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.enemyLateralMoveSpeed[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.predictableEnemyMovement[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.temporalASMagnitude[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.indexArray[roundManager.currentRoundNumber - 1].ToString() + "," +
               currentHealth.ToString("F2") + "," +
               minAngleToPlayer.ToString("F2") + "," +
               angularSizeOnSpawn.ToString("F2") + "," +
               fPSController.degreeToTargetX.ToString("F2") + "," +
               fPSController.degreeToTargetY.ToString("F2") + "," +
               fPSController.degreeToShootX.ToString("F2") + "," +
               fPSController.degreeToShootY.ToString("F2") + "," +
               fPSController.timeToTargetEnemy.ToString("F3") + "," +
               fPSController.timeToHitEnemy.ToString("F3") + "," +
               fPSController.timeToKillEnemy.ToString("F3") + "," +
               fPSController.targetMarked.ToString() + "," +
               fPSController.targetShot.ToString() + "," +
               durationAlive.ToString("F3") + "," +
               fPSController.timeOnTargetEachEnemy.ToString("F3");

            textWriter.WriteLine(enemyLogLine);
        }

        // Reset per-enemy metrics
        fPSController.degreeToTargetX = 0;
        fPSController.degreeToTargetY = 0;
        fPSController.degreeToShootX = 0;
        fPSController.degreeToShootY = 0;
        fPSController.timeToKillEnemy = 0;
        fPSController.timeToHitEnemy = 0;
        fPSController.timeToTargetEnemy = 0;
        fPSController.timeOnTargetEachEnemy = 0;
    }


    // ---- DEBUG VISUALIZER FOR ENEMYHEAD COLLIDER ----
    void DebugDrawEnemyHeadCollider(Color color, int steps = 64)
    {
        if (enemyHead == null) return;
        SphereCollider headCol = enemyHead.GetComponent<SphereCollider>();
        if (headCol == null) return;

        LineRenderer lr = enemyHead.GetComponent<LineRenderer>();
        if (lr == null)
            lr = enemyHead.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = steps + 1;
        lr.widthMultiplier = 0.03f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;

        Vector3 center = headCol.transform.TransformPoint(headCol.center);
        float worldRadius = headCol.radius * Mathf.Max(
            Mathf.Abs(headCol.transform.lossyScale.x),
            Mathf.Abs(headCol.transform.lossyScale.y),
            Mathf.Abs(headCol.transform.lossyScale.z)
        );

        // Camera-facing (billboard) plane
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 forward = cam.transform.forward.normalized;
        Vector3 up = cam.transform.up.normalized;
        Vector3 right = cam.transform.right.normalized;

        float deltaTheta = (2f * Mathf.PI) / steps;
        for (int i = 0; i <= steps; i++)
        {
            float theta = i * deltaTheta;
            // Circle in the camera's plane
            Vector3 offset = (Mathf.Cos(theta) * right + Mathf.Sin(theta) * up) * worldRadius;
            lr.SetPosition(i, center + offset);
        }
    }

    void PlayTeleportSFX()
    {
        audioSource.volume = UnityEngine.Random.Range(0.7f, 1f);
        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);

        audioSource.PlayOneShot(teleportSFX);
    }

    bool IsInvincible()
    {
        return durationAlive < enemyManager.invincibilityTimer || isTeleporting || isTeleportingSmooth;
    }

    // Call this from Update(): DebugDrawLargeCollider(Color.cyan, 64);
    void DebugDrawLargeCollider(Color color, int steps = 64)
    {
        if (largeCollider == null) return;                         // must be assigned in the Inspector
        SphereCollider col = largeCollider;                        // it's already a SphereCollider field
        GameObject go = col.gameObject;

        // Get / add a LineRenderer on the largeCollider's GameObject
        LineRenderer lr = go.GetComponent<LineRenderer>();
        if (lr == null) lr = go.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = steps + 1;
        lr.widthMultiplier = 0.03f;
        lr.material = new Material(Shader.Find("Sprites/Default")); // simple unlit
        lr.startColor = color;
        lr.endColor = color;

        // World-space center & radius (handles non-uniform scale)
        Vector3 center = col.transform.TransformPoint(col.center);
        float worldRadius = col.radius * Mathf.Max(
            Mathf.Abs(col.transform.lossyScale.x),
            Mathf.Abs(col.transform.lossyScale.y),
            Mathf.Abs(col.transform.lossyScale.z)
        );

        // Billboard to camera
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 right = cam.transform.right.normalized;
        Vector3 up = cam.transform.up.normalized;

        // Circle in camera plane
        float delta = (2f * Mathf.PI) / steps;
        for (int i = 0; i <= steps; i++)
        {
            float a = i * delta;
            Vector3 offset = (Mathf.Cos(a) * right + Mathf.Sin(a) * up) * worldRadius;
            lr.SetPosition(i, center + offset);
        }
    }

    void TeleportInViewMove()
    {
        if (isTeleportingSmooth)
        {
            teleportMoveElapsed += Time.deltaTime;
            isTeleporting = true;
            float t = teleportMoveElapsed / teleportMoveDuration;

            if (t >= 1f)
            {
                t = 1f;
                isTeleportingSmooth = false;
                playerController.readyToInduceSpike = true;
                player.GetComponent<FPSController>().angularDistanceFromEnemyOnStart =
                    player.GetComponent<FPSController>().missAnglePublic;

                // Store teleport Y for asymmetric bobbing
                lateralCenterY = transform.position.y;

                // same random direction start each teleport
                StartLateralMovementRandomDirection();
            }

            transform.position = Vector3.Lerp(transform.position, teleportTargetPos, t);

            Vector3 lookDirT = player.transform.position - transform.position;
            lookDirT.y = 0;
            if (lookDirT.magnitude > 0.1f)
                transform.rotation = Quaternion.LookRotation(lookDirT);

            return;
        }

        isTeleporting = false;
        if (!lateralMoveActive) return;

        // --- ensure radius ---
        Vector3 centerToEnemy = transform.position - player.transform.position;
        centerToEnemy.y = 0f;
        float radius = centerToEnemy.magnitude;
        if (radius < 0.05f)
        {
            radius = Mathf.Max(teleportRadius, 0.5f);
            Vector3 outDir = (transform.position - player.transform.position);
            outDir.y = 0f;
            if (outDir.sqrMagnitude < 1e-4f) outDir = player.transform.right;
            outDir.Normalize();
            transform.position = player.transform.position + outDir * radius;
        }

        float effectiveSpeed = lateralMoveSpeed;
        float targetY = transform.position.y;
        float time = Time.time;
        float dt = Time.deltaTime;

        if (unpredictable)
        {
            // frequency tied to base speed
            float freq = Mathf.Max(0.05f, lateralMoveSpeed * unpredictableFreq);

            // === Perlin direction and magnitude (bidirectional but compensated) ===
            float perlinSpeed = Mathf.PerlinNoise(perlinSeedSpeed, time * freq);
            float sNoise = (perlinSpeed - 0.5f) * 2f; // [-1, 1]

            float dirSign = Mathf.Sign(sNoise == 0f ? 1f : sNoise);
            float magBlend = Mathf.Lerp(0.7f, 1.3f, Mathf.Abs(sNoise)); // speed fluctuation
            float signedMag = dirSign * lateralMoveSpeed * magBlend;

            // compensate toward avg magnitude ≈ lateralMoveSpeed
            float alpha = 1f - Mathf.Exp(-5f * dt);
            avgAbsOrbitSpeed = Mathf.Lerp(
                avgAbsOrbitSpeed <= 0f ? Mathf.Abs(signedMag) : avgAbsOrbitSpeed,
                Mathf.Abs(signedMag),
                alpha);
            float gain = lateralMoveSpeed / Mathf.Max(0.001f, avgAbsOrbitSpeed);
            gain = Mathf.Clamp(gain, 0.7f, 1.3f);
            effectiveSpeed = signedMag * gain;

            // === Y bob, bounded ===
            float perlinY = Mathf.PerlinNoise(perlinSeedY, time * freq);
            float yNoise = (perlinY - 0.5f) * 2f;
            float worldMinY = player.transform.position.y + teleportYMin;
            float worldMaxY = player.transform.position.y + teleportYMax;
            float upAvail = Mathf.Max(0f, worldMaxY - lateralCenterY);
            float downAvail = Mathf.Max(0f, lateralCenterY - worldMinY);
            float yOffset = (yNoise >= 0f) ? (yNoise * upAvail) : (yNoise * downAvail);
            targetY = Mathf.Clamp(lateralCenterY + yOffset, worldMinY, worldMaxY);

            // safety floor
            float floor = 0.3f * lateralMoveSpeed;
            if (Mathf.Abs(effectiveSpeed) < floor)
                effectiveSpeed = dirSign * floor;
        }

        // === orbit motion ===
        float angularDegPerSec = (effectiveSpeed / Mathf.Max(radius, 0.0001f)) * Mathf.Rad2Deg;
        float deltaAngle = (unpredictable ? angularDegPerSec : lateralDirection * angularDegPerSec) * Time.deltaTime;
        transform.RotateAround(player.transform.position, Vector3.up, deltaAngle);

        // === Y motion only when unpredictable ===
        if (unpredictable)
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, targetY, dt * 3f);
            transform.position = pos;
        }

        // === Always face player ===
        Vector3 lookDir = player.transform.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 1e-4f)
            transform.rotation = Quaternion.LookRotation(lookDir);

        // === Teleport timing ===
        teleportTimer += dt;
        if (teleportTimer >= teleportInterval)
        {
            teleportTimer = 0f;
            TrySetTeleportTarget();
            player.GetComponent<FPSController>().UpdatePerShootingEventLog(false);
            player.GetComponent<FPSController>().eventCount++;

        }
    }


}
