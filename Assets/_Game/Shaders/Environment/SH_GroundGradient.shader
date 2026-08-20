Shader "ROS/Environment/GroundGradient"
{
    Properties
    {
        _DarkColor ("Verde oscuro", Color) = (0.025, 0.19, 0.055, 1)
        _LightColor ("Verde claro", Color) = (0.24, 0.64, 0.14, 1)
        _Smoothness ("Suavidad", Range(0, 1)) = 0.08
        [HideInInspector] _GradientMinimum ("Inicio", Float) = -200
        [HideInInspector] _GradientRange ("Rango", Float) = 400
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        fixed4 _DarkColor;
        fixed4 _LightColor;
        half _Smoothness;
        float _GradientMinimum;
        float _GradientRange;

        struct Input
        {
            float3 worldPos;
        };

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            float diagonal = saturate(
                (input.worldPos.x + input.worldPos.z - _GradientMinimum) /
                max(_GradientRange, 0.001)
            );
            float blend = smoothstep(0.05, 0.95, diagonal);
            fixed3 groundColor = lerp(
                _DarkColor.rgb,
                _LightColor.rgb,
                blend
            );

            output.Albedo = groundColor;
            output.Metallic = 0;
            output.Smoothness = _Smoothness;
            output.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
