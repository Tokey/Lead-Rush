using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameBlender : MonoBehaviour
{
    // Assign a material that uses the "Custom/FrameBlend" shader.
    public Material blendMaterial;

    // Blend factor between 0 and 1.
    [Range(0, 1)]
    public float blendFactor = 0.5f;

    private RenderTexture previousFrame;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (blendMaterial == null)
        {
            // If no blend material is assigned, just pass the image through.
            Graphics.Blit(source, destination);
            return;
        }

        // Check if we need to initialize or recreate the previous frame buffer.
        if (previousFrame == null || previousFrame.width != source.width || previousFrame.height != source.height)
        {
            if (previousFrame != null)
                previousFrame.Release();

            previousFrame = new RenderTexture(source.width, source.height, 0);
            // Initialize previous frame by copying the current frame.
            Graphics.Blit(source, previousFrame);
        }

        // Set the blend factor and previous frame texture in the material.
        blendMaterial.SetFloat("_BlendFactor", blendFactor);
        blendMaterial.SetTexture("_PrevTex", previousFrame);

        // Blend the current frame with the previous frame.
        Graphics.Blit(source, destination, blendMaterial);

        // Update the previous frame for the next render.
        Graphics.Blit(destination, previousFrame);
    }

    void OnDestroy()
    {
        if (previousFrame != null)
        {
            previousFrame.Release();
        }
    }
}
