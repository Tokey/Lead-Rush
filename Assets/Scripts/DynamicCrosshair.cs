using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    public Transform shootPoint;
    public float minArmLength = 16f;
    public float maxArmLength = 38f;
    public float minGap = 4f;
    public float maxGap = 16f;
    public float thickness = 2f;
    public float border = 2f;
    public float velocityToMax = 600f;
    public float smoothTime = 0.09f;
    public Color crosshairColor = new Color(0f, 1f, 0f, 0.48f); // Green, semi-transparent
    public Color borderColor = new Color(0, 0, 0, 0.75f);
    public float maxDistance = 200f;
    public LayerMask mask = ~(1 << 8);

    private Vector2 prevScreenPos;
    private float curArmLength;
    private float curGap;

    // Expose the crosshair center (updated every frame)
    public Vector2 CrosshairScreenPosition { get; private set; }

    void Start()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        prevScreenPos = screenCenter;
        curArmLength = minArmLength;
        curGap = minGap;
        CrosshairScreenPosition = screenCenter;
    }

    void OnGUI()
    {
        if (shootPoint == null || Camera.main == null)
            return;

        // Raycast to get where the crosshair should point
        Vector3 hitPos = shootPoint.position + shootPoint.forward * maxDistance;
        if (Physics.Raycast(shootPoint.position, shootPoint.forward, out RaycastHit hit, maxDistance, mask))
            hitPos = hit.point;

        Vector3 sp = Camera.main.WorldToScreenPoint(hitPos);
        if (sp.z < 0) return;
        float x = sp.x;
        float y = Screen.height - sp.y; // OnGUI Y-flip

        Vector2 screenPos = new Vector2(x, y);
        CrosshairScreenPosition = screenPos;

        float moveSpeed = ((screenPos - prevScreenPos) / Time.deltaTime).magnitude;
        prevScreenPos = screenPos;

        float targetArm = Mathf.Lerp(minArmLength, maxArmLength, Mathf.Clamp01(moveSpeed / velocityToMax));
        float targetGap = Mathf.Lerp(minGap, maxGap, Mathf.Clamp01(moveSpeed / velocityToMax));
        curArmLength = Mathf.Lerp(curArmLength, targetArm, 1f - Mathf.Exp(-Time.deltaTime / smoothTime));
        curGap = Mathf.Lerp(curGap, targetGap, 1f - Mathf.Exp(-Time.deltaTime / smoothTime));

        DrawCrosshairLines(x, y, curArmLength, curGap, thickness + border * 2, borderColor);
        DrawCrosshairLines(x, y, curArmLength, curGap, thickness, crosshairColor);
    }

    void DrawCrosshairLines(float x, float y, float arm, float gap, float thick, Color col)
    {
        Color prevColor = GUI.color;
        GUI.color = col;

        // Left
        GUI.DrawTexture(new Rect(x - gap - arm, y - thick / 2, arm, thick), Texture2D.whiteTexture);
        // Right
        GUI.DrawTexture(new Rect(x + gap, y - thick / 2, arm, thick), Texture2D.whiteTexture);
        // Top
        GUI.DrawTexture(new Rect(x - thick / 2, y - gap - arm, thick, arm), Texture2D.whiteTexture);
        // Bottom
        GUI.DrawTexture(new Rect(x - thick / 2, y + gap, thick, arm), Texture2D.whiteTexture);

        // Optional: center dot (uncomment to enable)
        // float dotSize = thick * 1.7f;
        // GUI.DrawTexture(new Rect(x - dotSize / 2, y - dotSize / 2, dotSize, dotSize), Texture2D.whiteTexture);

        GUI.color = prevColor;
    }
}
