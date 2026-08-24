Shader "Hidden/ROS/LobbyColorGrade"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Exposure;
            float _Contrast;
            float _Saturation;
            float _Vignette;
            float _VignetteSoftness;
            float _Aspect;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 sampleColor = tex2D(_MainTex, i.uv);
                float3 color = sampleColor.rgb;

                color *= exp2(_Exposure);
                color = (color - 0.5) * _Contrast + 0.5;

                float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
                color = lerp(luminance.xxx, color, _Saturation);

                float2 centered = i.uv * 2.0 - 1.0;
                centered.x *= max(1.0, _Aspect / 1.7777778);
                float distanceFromCenter = length(centered);
                float vignetteMask = smoothstep(
                    _VignetteSoftness,
                    1.35,
                    distanceFromCenter
                );
                color *= lerp(1.0, 1.0 - _Vignette, vignetteMask);

                return fixed4(saturate(color), sampleColor.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
