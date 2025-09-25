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

    float durationAlive;
    public FPSController fPSController;

    bool startMissAngleFuse = false;

    public AudioSource audioSource;
    public AudioClip teleportSFX;

    public bool isTeleporting;

    public GameObject invincibilityEffect;

    void Start()
    {
        enemyAgent = gameObject.GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player");
        manager = GameObject.FindGameObjectWithTag("Manager");
        playerController = player.GetComponent<FPSController>();

        enemyManager = manager.GetComponent<EnemyManager>();

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


        // Randomize the first interval

        //Non uniform random
        float min = UnityEngine.Random.Range(teleportRandSuperMin, teleportIntervalMin);
        float max = UnityEngine.Random.Range(teleportIntervalMax, teleportRandSuperMax);

        float teleportIntAdd = playerController.gameManager.delayDuration / 1000.0f;

        teleportInterval = UnityEngine.Random.Range(teleportIntAdd + min, teleportIntAdd + max);

        teleportTimer = teleportInterval;
        durationAlive = 0f;


        //player.GetComponent<FPSController>().angularDistanceFromEnemyOnStart = player.GetComponent<FPSController>().missAnglePublic;

        audioSource = GetComponent<AudioSource>();
        isTeleporting = false;

        teleportInterval = UnityEngine.Random.Range(teleportIntAdd + min, teleportIntAdd + max);
        enemyManager.invincibilityTimer = enemyManager.remainingTeleportTimeFromLastEnemy;
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
            Instantiate(explodePE, headTransform.position, headTransform.rotation);
            player.GetComponent<FPSController>().PlayDeathSFX();
            player.GetComponent<FPSController>().RespawnPlayer();
        }

        largeCollider.radius = 3.0f + Mathf.PingPong(Time.time, 1.5f);


        durationAlive += Time.deltaTime;

        //DebugDrawEnemyHeadCollider(Color.yellow, 36);

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

    void TeleportInViewMove()
    {
        if (isTeleportingSmooth)
        {
            teleportMoveElapsed += Time.deltaTime;
            isTeleporting = true;
            float t = teleportMoveElapsed / teleportMoveDuration;

            //invincibilityEffect.SetActive(true);

            if (t >= 1f)
            {
                t = 1f;
                isTeleportingSmooth = false;
                playerController.readyToInduceSpike = true;
                player.GetComponent<FPSController>().angularDistanceFromEnemyOnStart = player.GetComponent<FPSController>().missAnglePublic;

            }

            transform.position = Vector3.Lerp(transform.position, teleportTargetPos, t);

            Vector3 lookDir = player.transform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.magnitude > 0.1f)
                transform.rotation = Quaternion.LookRotation(lookDir);

            //player.GetComponent<FPSController>().angularDistanceFromEnemyOnStart = player.GetComponent<FPSController>().missAnglePublic;
            return;
        }
        else
        {
            isTeleporting = false;
            /*if(!IsInvincible())
                invincibilityEffect.SetActive(false);*/
        }

        teleportTimer += Time.deltaTime;
        if (teleportTimer >= teleportInterval)
        {
            teleportTimer = 0f;
            TrySetTeleportTarget();

            player.GetComponent<FPSController>().UpdatePerShootingEventLog(false);
        }
    }

    void TrySetTeleportTarget()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

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
        RoundManager roundManager = manager.GetComponent<RoundManager>();
        FPSController fPSController = player.GetComponent<FPSController>();

        string filenameEnemyLog = "Data\\Logs\\EnemyData_" + roundManager.fileNameSuffix + "_" + roundManager.sessionID + "_" + ".csv";

        bool fileExists = File.Exists(filenameEnemyLog);

        using (StreamWriter textWriter = File.AppendText(filenameEnemyLog))
        {
            if (!fileExists)
            {
                textWriter.WriteLine(
                    "SessionID,LatinRow,RoundNumber,SessionStart,KillTimestamp,RoundFPS,SpikeMagnitude," +
                    "OnAimSpike,OnEnemySpawnSpike,OnMouseSpike,OnReloadSpike,AttributeScalingEnabled," + "AttributeScaleRadius," +
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
}
