using Demo.Scripts.Runtime;
using UnityEngine;

public class AttributeScaling : MonoBehaviour
{
    public enum AttributeScalingMode
    {
        ColliderExpandingAS,
        MouseSpeedAS,
        FixedAS
    }

    [Header("General")]
    public AttributeScalingMode scalingMode = AttributeScalingMode.MouseSpeedAS;
    public bool useAttributeScaling = true;

    [Header("References")]
    public SphereCollider enemyCollider;
    public FPSController playerController;
    public GameObject muzzlePoint;

    [Header("ColliderExpandingAS Settings")]
    public float spikeThreshold = 0.03f;
    public float attributeScalingDuration = 0.5f;
    public float attributeScalingDurationMultiplier = 1.0f;
    public float scaleDuration = 0.05f;
    public float minScale = 0.5f;
    public float maxScale = 3f;
    public float initialScale;

    public float attributeScaleRadius;

    [Header("MouseSpeedAS Settings")]
    [Tooltip("Lerp factor for moving average of mouse speed")]
    public float mouseSmoothFactor = 0.1f;
    [Tooltip("Scale multiplier applied to the averaged mouse speed")]
    public float mouseSpeedScale = 0.01f;

    [Header("Deflation Settings")]
    [Tooltip("Duration for smooth deflation back to target")]
    public float scaleDownDuration = 0.5f;

    [Header("FixedAS Settings")]
    [Tooltip("Current spike magnitude to use for scaling (set from elsewhere or inspector)")]
    public float spikeMagnitude = 0f;
    [Tooltip("Maximum expected spike magnitude (for curve mapping)")]
    public float maxSpikeMagnitude = 1350f;
    [Tooltip("Curve maps spikeMagnitude (0 to max) → collider size (min to max)")]
    public AnimationCurve spikeToScaleCurve;

    // --- internal state ---
    private bool isAttributeScalingEnabled = false;
    private float currentTimer = 0f;
    private float _scaleTimer = 0f;
    private bool _scaleLocked = false;

    private bool isScalingDown = false;
    private float scaleDownTimer = 0f;
    private float scaleOffStart = 0f;
    private float scaleTarget = 0f;

    public float avgMouseSpeed = 0f;
    public float currentSpeed = 0f;

    bool colliderASFuse = false;

    void Start()
    {
        Time.maximumDeltaTime = 1f;
        if (enemyCollider == null)
            enemyCollider = GetComponent<SphereCollider>();

        avgMouseSpeed = 0f;
        scaleTarget = initialScale;
        if (enemyCollider != null)
            enemyCollider.radius = initialScale;

        // --- PRESET THE CURVE FOR FIXEDAS BASED ON YOUR GRAPH DATA ---
        spikeToScaleCurve = new AnimationCurve(
            new Keyframe(0f, 0.7f),    // 0 spike
            new Keyframe(0.06f, 0.75f),   // 75 spike
            new Keyframe(0.11f, 1.3f),    // 150 spike
            new Keyframe(0.17f, 1.5f),    // 225 spike
            new Keyframe(0.33f, 2.1f),    // 450 spike
            new Keyframe(0.5f, 2.9f),    // 675 spike
            new Keyframe(1f, 3f)         // 1350 spike
        );
    }

    void Update()
    {
        if (enemyCollider == null)
        {
            if (playerController != null && playerController.currentEnemyHead != null)
                enemyCollider = playerController.currentEnemyHead.GetComponent<SphereCollider>();
            if (enemyCollider == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("AttributeScaling: enemyCollider is null. No scaling will occur this frame.");
#endif
                return;
            }
        }

        if (!useAttributeScaling && scalingMode != AttributeScalingMode.FixedAS)
            return;

        switch (scalingMode)
        {
            case AttributeScalingMode.ColliderExpandingAS:
                if (Time.deltaTime > spikeThreshold || Input.GetKeyDown(KeyCode.X))
                    EnableAttributeScaling();

                if (isAttributeScalingEnabled)
                {
                    if (playerController.playerOnTarget && !_scaleLocked)
                        _scaleLocked = true;

                    if (!_scaleLocked && enemyCollider.radius < maxScale)
                    {
                        _scaleTimer = Mathf.Min(_scaleTimer + Time.deltaTime, scaleDuration);
                        float t = _scaleTimer / scaleDuration;
                        enemyCollider.radius = Mathf.Lerp(initialScale, maxScale, t);
                    }

                    currentTimer -= Time.deltaTime;
                    if (currentTimer <= 0f)
                    {
                        isAttributeScalingEnabled = false;
                        isScalingDown = true;
                        scaleDownTimer = 0f;
                        scaleOffStart = enemyCollider.radius;
                        _scaleTimer = 0f;
                        _scaleLocked = false;
                        scaleTarget = initialScale;
                    }
                }
                else if (isScalingDown)
                {
                    scaleDownTimer = Mathf.Min(scaleDownTimer + Time.deltaTime, scaleDownDuration);
                    float t = scaleDownTimer / scaleDownDuration;
                    enemyCollider.radius = Mathf.Lerp(scaleOffStart, scaleTarget, t);

                    if (scaleDownTimer >= scaleDownDuration)
                        isScalingDown = false;
                }
                else
                {
                    enemyCollider.radius = initialScale;
                }
                break;

            case AttributeScalingMode.MouseSpeedAS:
                if (Time.deltaTime > spikeThreshold || Input.GetKeyDown(KeyCode.X))
                    EnableAttributeScaling();

                float dx = playerController.deltaMouseX;
                float dy = playerController.deltaMouseY;
                currentSpeed = new Vector2(dx, dy).magnitude / Time.deltaTime;

                avgMouseSpeed = Mathf.Lerp(avgMouseSpeed, currentSpeed, mouseSmoothFactor);
                if (isAttributeScalingEnabled)
                {
                    float desiredScale = Mathf.Clamp(
                        initialScale + avgMouseSpeed * mouseSpeedScale,
                        minScale,
                        maxScale
                    );

                    if (colliderASFuse)
                    {
                        if (desiredScale >= enemyCollider.radius)
                        {
                            enemyCollider.radius = desiredScale;
                            isScalingDown = false;
                        }
                        else
                        {
                            if (!isScalingDown)
                            {
                                isScalingDown = true;
                                scaleDownTimer = 0f;
                                scaleOffStart = enemyCollider.radius;
                                scaleTarget = desiredScale;
                            }
                        }
                        colliderASFuse = false;
                    }

                    currentTimer -= Time.deltaTime;
                    if (currentTimer <= 0f)
                    {
                        colliderASFuse = true;
                        isAttributeScalingEnabled = false;
                        isScalingDown = true;
                        scaleDownTimer = 0f;
                        scaleOffStart = enemyCollider.radius;
                        _scaleTimer = 0f;
                        _scaleLocked = false;
                        scaleTarget = initialScale;
                    }
                }
                if (isScalingDown)
                {
                    scaleDownTimer = Mathf.Min(scaleDownTimer + Time.deltaTime, scaleDownDuration);
                    float t = scaleDownTimer / scaleDownDuration;
                    enemyCollider.radius = Mathf.Lerp(scaleOffStart, scaleTarget, t);

                    if (scaleDownTimer >= scaleDownDuration)
                        isScalingDown = false;
                }
                break;

            case AttributeScalingMode.FixedAS:
                if (useAttributeScaling)
                {
                    /*float normalizedSpike = Mathf.Clamp01(spikeMagnitude / maxSpikeMagnitude);
                    float curveValue = spikeToScaleCurve.Evaluate(normalizedSpike);*/


                    enemyCollider.radius = initialScale * attributeScaleRadius;
                }
                else
                {
                    enemyCollider.radius = initialScale;
                }
                break;
        }
    }

    public void EnableAttributeScaling()
    {
        isAttributeScalingEnabled = true;
        currentTimer = attributeScalingDuration * attributeScalingDurationMultiplier;
        _scaleTimer = 0f;
        _scaleLocked = false;

        if (enemyCollider != null)
            enemyCollider.radius = initialScale;

        colliderASFuse = true;
    }
}
