Shader "Custom/FrameBlend"
{
    Properties
    {
        _MainTex ("Current Frame", 2D) = "white" {}
        _PrevTex ("Previous Frame", 2D) = "white" {}
        _BlendFactor ("Blend Factor", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            // The current frame texture.
            sampler2D _MainTex;
            // The previous frame texture.
            sampler2D _PrevTex;
            // A value between 0 and 1 that controls blending.
            float _BlendFactor;

            fixed4 frag(v2f_img i) : SV_Target
            {
                // Sample the current frame.
                fixed4 currentColor = tex2D(_MainTex, i.uv);
                // Sample the previous frame.
                fixed4 prevColor = tex2D(_PrevTex, i.uv);
                // Blend the two frames via linear interpolation.
                return lerp(currentColor, prevColor, _BlendFactor);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
