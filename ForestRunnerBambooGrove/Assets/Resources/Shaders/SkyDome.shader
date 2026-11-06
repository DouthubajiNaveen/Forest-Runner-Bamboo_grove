Shader "ForestRunner/SkyDome"
{
    // Procedural gradient sky with sun/moon disc, stars and distant hill silhouettes.
    // No textures or shader keywords, so it works in the Editor and in standalone builds.
    Properties
    {
        _Top ("Zenith colour", Color) = (0.25, 0.55, 0.9, 1)
        _Horizon ("Horizon colour", Color) = (0.8, 0.9, 0.85, 1)
        _Bottom ("Below horizon colour", Color) = (0.3, 0.4, 0.35, 1)
        _HillColor ("Hill colour", Color) = (0.3, 0.45, 0.4, 1)
        _SunColor ("Sun colour", Color) = (1, 0.95, 0.8, 1)
        _SunDir ("Sun direction", Vector) = (0, 0.5, 1, 0)
        _SunSize ("Sun size", Range(0.9, 0.9999)) = 0.996
        _SunGlow ("Sun glow", Range(0, 2)) = 0.6
        _Stars ("Star amount", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "IgnoreProjector" = "True" }
        Cull Front
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Top, _Horizon, _Bottom, _HillColor, _SunColor;
            float4 _SunDir;
            float _SunSize, _SunGlow, _Stars;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash31 (float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                float3 col;
                if (h >= 0.0)
                {
                    float t = pow(saturate(h), 0.45);
                    col = lerp(_Horizon.rgb, _Top.rgb, t);
                }
                else
                {
                    col = lerp(_Horizon.rgb, _Bottom.rgb, saturate(-h * 6.0));
                }

                // distant hills
                float az = atan2(d.x, d.z);
                float ridge = 0.05 + 0.025 * sin(az * 4.0 + 1.0) + 0.015 * sin(az * 9.0 + 2.0) + 0.008 * sin(az * 23.0);
                float hillMask = 1.0 - smoothstep(ridge - 0.004, ridge + 0.004, h);
                float3 hillCol = lerp(_HillColor.rgb, _Horizon.rgb, saturate(h / max(ridge, 0.001)) * 0.6);
                col = lerp(col, hillCol, hillMask);

                // sun / moon
                float3 sd = normalize(_SunDir.xyz);
                float sdot = dot(d, sd);
                float disc = smoothstep(_SunSize, _SunSize + (1.0 - _SunSize) * 0.4, sdot);
                float glow = pow(saturate(sdot), 24.0) * _SunGlow;
                col += _SunColor.rgb * (glow * (1.0 - hillMask * 0.8));
                col = lerp(col, _SunColor.rgb, disc * (1.0 - hillMask));

                // stars
                if (_Stars > 0.001 && h > 0.0)
                {
                    float3 q = floor(d * 260.0);
                    float s = step(0.9975, hash31(q));
                    col += s * _Stars * saturate(h * 3.0) * (1.0 - disc);
                }
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
