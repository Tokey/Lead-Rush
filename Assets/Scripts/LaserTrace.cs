using UnityEngine;

public class LaserBeamAuto : MonoBehaviour
{
    public Transform muzzleTransform;         // Assign in Inspector!
    public float maxLength = 100f;
    public Color laserColor = Color.red;
    public Color pulseColor = Color.yellow;   // Color at brightest pulse
    public float laserRadius = 0.02f;         // Min thickness
    public float pulseWidthMultiplier = 2f;   // Max thickness = laserRadius * this
    public float pulseSpeed = 8f;             // Pulses per second
    public float noiseWaviness = 2.2f;        // Higher = more wiggle
    public float noiseSpeed = 2.0f;           // Speed of wave

    [Header("Reticle Settings")]
    public GameObject reticlePrefab;          // Assign a prefab (sphere, dot, etc)
    public float reticleBaseSize = 0.05f;     // Diameter at min pulse
    public float reticlePulseMultiplier = 0.4f; // How much reticle pulses (0 = steady, 1 = matches laser)

    private GameObject laserObj;
    private Material laserMat;
    private GameObject reticleObj;
    private Material reticleMat;
    private float t;

    // Layer to ignore (set by name)
    private int ignoreLayer;
    private int ignoreMask;

    void Start()
    {
        if (muzzleTransform == null)
        {
            Debug.LogError("Assign muzzleTransform in Inspector!");
            enabled = false;
            return;
        }

        // Set up layer mask to ignore 'LargeColliders'
        ignoreLayer = LayerMask.NameToLayer("LargeColliders");
        ignoreMask = ~(1 << ignoreLayer);

        // Create the laser cylinder
        laserObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        laserObj.transform.SetParent(muzzleTransform, false);
        laserObj.transform.localPosition = Vector3.zero;
        laserObj.transform.localRotation = Quaternion.identity;
        laserObj.transform.localEulerAngles = new Vector3(90, 0, 0);

        laserMat = new Material(Shader.Find("Unlit/Color"));
        laserMat.color = laserColor;
        laserObj.GetComponent<Renderer>().material = laserMat;

        laserObj.transform.localScale = new Vector3(laserRadius, 0.5f, laserRadius);

        Destroy(laserObj.GetComponent<Collider>());

        // Create or assign the reticle
        if (reticlePrefab != null)
        {
            reticleObj = Instantiate(reticlePrefab, muzzleTransform);
            reticleObj.transform.localPosition = Vector3.zero;
            reticleObj.transform.localRotation = Quaternion.identity;
            var rend = reticleObj.GetComponent<Renderer>();
            if (rend != null)
            {
                reticleMat = new Material(Shader.Find("Unlit/Color"));
                reticleMat.color = laserColor;
                rend.material = reticleMat;
            }
        }
    }

    void Update()
    {
        if (!laserObj) return;

        t += Time.deltaTime * pulseSpeed;

        // Pulse factor with Perlin noise + sine
        float noise = Mathf.PerlinNoise(t * noiseSpeed, 100f) * noiseWaviness;
        float sin = (Mathf.Sin(t * Mathf.PI * 2) + 1f) * 0.5f;
        float pulse = Mathf.Clamp01(0.6f * sin + 0.4f * noise); // 0..1

        // Color shift for pulse
        Color c = Color.Lerp(laserColor, pulseColor, pulse);
        c.a = Mathf.Lerp(0.4f, 1f, pulse);
        laserMat.color = c;

        // Wavy width
        float curRadius = Mathf.Lerp(laserRadius, laserRadius * pulseWidthMultiplier, pulse);

        // Raycast for length, IGNORING 'LargeColliders' layer
        float length = maxLength;
        if (Physics.Raycast(muzzleTransform.position, muzzleTransform.forward, out RaycastHit hit, maxLength, ignoreMask))
            length = hit.distance;

        laserObj.transform.localScale = new Vector3(curRadius, length / 2f, curRadius);
        laserObj.transform.localPosition = new Vector3(0, 0, length / 2f);

        // Update reticle
        if (reticleObj != null)
        {
            reticleObj.transform.localPosition = new Vector3(0, 0, length);

            // Reticle pulse is weaker (so looks steadier)
            float reticlePulse = Mathf.Lerp(0, pulse, reticlePulseMultiplier);
            float reticleScale = Mathf.Lerp(reticleBaseSize, reticleBaseSize * pulseWidthMultiplier, reticlePulse);
            reticleObj.transform.localScale = new Vector3(reticleScale, reticleScale, reticleScale);

            // Reticle color (less alpha fade, more steady)
            if (reticleMat != null)
            {
                Color reticleColor = Color.Lerp(laserColor, pulseColor, reticlePulse);
                reticleColor.a = Mathf.Lerp(0.7f, 1f, reticlePulse); // more opaque
                reticleMat.color = reticleColor;
            }
        }
    }
}
