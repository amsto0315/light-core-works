Shader "Echo/Surface"
{
    Properties
    {
        _Color ("Surface Color", Color) = (0.24, 0.45, 0.46, 1)
        _GridColor ("Echo Color", Color) = (0.48, 1.0, 0.88, 1)
        _Base ("Permanent Glow", Range(0,1)) = 0.012
        _Grid ("Grid Strength", Range(0,1)) = 0.55
        _Smoothness ("Smoothness", Range(0,1)) = 0.18
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        fixed4 _Color;
        fixed4 _GridColor;
        half _Base;
        half _Grid;
        half _Smoothness;

        float4 _PulsePos[4];
        float _PulseCount;
        float _PulseLife;
        float _PulseSpeed;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos;

            // Four simultaneous sonar pulses. A pulse is a thin expanding
            // shell: geometry is only visible where the wave is passing.
            float reveal = 0;
            float ripple = 0;
            float trail = 0;
            [unroll]
            for (int i = 0; i < 4; i++)
            {
                if (i >= _PulseCount) break;
                float4 pulse = _PulsePos[i];
                float age = _Time.y - pulse.w;
                if (age < 0 || age > _PulseLife) continue;
                float wave = age * _PulseSpeed;
                float d = distance(p, pulse.xyz);
                float band = 1 - smoothstep(0.55, 1.8, abs(d - wave));
                float fade = 1 - saturate(age / _PulseLife);
                reveal = max(reveal, band * fade);
                // The wave leaves a decaying memory on nearby geometry so
                // the room does not go completely black between pulses.
                float memory = (1 - saturate(age / _PulseLife)) *
                               (1 - smoothstep(2.0, 30.0, d)) * 0.62;
                reveal = max(reveal, memory);
                ripple += band * fade;
                trail = max(trail, (1 - saturate((wave - d) / 4.0)) * fade * 0.12);
            }

            // A sparse architectural grid, only present where the echo is.
            float3 cell = abs(frac(p * 1.35) - 0.5);
            float grid = smoothstep(0.455, 0.5, max(cell.x, max(cell.y, cell.z)));
            float fine = smoothstep(0.485, 0.5, max(abs(frac(p.x * 6.4) - 0.5),
                                                  abs(frac(p.y * 6.4) - 0.5)));
            float g = grid * _Grid + fine * _Grid * 0.35;

            float edge = pow(saturate(1 - dot(normalize(IN.viewDir), float3(0,1,0))), 2.0) * 0.10;
            float light = _Base + reveal * 0.92 + trail + edge * reveal;

            o.Albedo = _Color.rgb * (0.020 + light * 0.82);
            o.Emission = _GridColor.rgb * (g * (0.12 + reveal * 1.65) + ripple * 0.55) +
                         _Color.rgb * (_Base * 0.55 + reveal * 0.24);
            o.Smoothness = _Smoothness;
            o.Metallic = 0.05;
            o.Occlusion = 1;
        }
        ENDCG
    }
    FallBack Off
}
