using Demo.Scripts.Runtime;
using UnityEngine;

public class AttributeScaling : MonoBehaviour
{
    public enum AttributeScalingMode
    {
        ColliderExpandingAS,
        MouseSpeedAS
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

    [Header("MouseSpeedAS Settings")]
    [Tooltip("Lerp factor for moving average of mouse speed")]
    public float mouseSmoothFactor = 0.1f;
    [Tooltip("Scale multiplier applied to the averaged mouse speed")]
    public float mouseSpeedScale = 0.01f;

    [Header("Deflation Settings")]
    [Tooltip("Duration for smooth deflation back to target")]
    public float scaleDownDuration = 0.5f;

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
    }

    void Update()
    {
        // --- Robustly ensure enemyCollider is valid ---
        if (enemyCollider == null)
        {
            // Try to reacquire from playerController's currentEnemyHead
            if (playerController != null && playerController.currentEnemyHead != null)
            {
                enemyCollider = playerController.currentEnemyHead.GetComponent<SphereCollider>();
            }
            if (enemyCollider == null)
            {
                // Log warning only in Editor or Development builds
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("AttributeScaling: enemyCollider is null. No scaling will occur this frame.");
#endif
                return; // Skip all logic until a valid collider is assigned
            }
        }

        if (!useAttributeScaling)
            return;

        switch (scalingMode)
        {
            case AttributeScalingMode.ColliderExpandingAS:
                // trigger on spike or manual key
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

                // compute raw mouse speed
                float dx = playerController.deltaMouseX;
                float dy = playerController.deltaMouseY;
                currentSpeed = new Vector2(dx, dy).magnitude / Time.deltaTime;

                // moving average
                avgMouseSpeed = Mathf.Lerp(avgMouseSpeed, currentSpeed, mouseSmoothFactor);
                if (isAttributeScalingEnabled)
                {
                    // desired scale
                    float desiredScale = Mathf.Clamp(
                        initialScale + avgMouseSpeed * mouseSpeedScale,
                        minScale,
                        maxScale
                    );

                    if (colliderASFuse)
                    {
                        if (desiredScale >= enemyCollider.radius)
                        {
                            // instant expansion
                            enemyCollider.radius = desiredScale;
                            isScalingDown = false;
                        }
                        else
                        {
                            // start deflation if not already
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
                // smooth deflation
                if (isScalingDown)
                {
                    scaleDownTimer = Mathf.Min(scaleDownTimer + Time.deltaTime, scaleDownDuration);
                    float t = scaleDownTimer / scaleDownDuration;
                    enemyCollider.radius = Mathf.Lerp(scaleOffStart, scaleTarget, t);

                    if (scaleDownTimer >= scaleDownDuration)
                        isScalingDown = false;
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
