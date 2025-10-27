
Shader "URP/2D/CastZone2D_Unlit_Procedural"
{
    Properties
    {
        [MainTexture]_MainTex ("Sprite", 2D) = "white" {}
        [HDR]_MainColor ("Main Color", Color) = (1, 0.5, 0.1, 1)
        [HDR]_EdgeColor ("Edge Color", Color) = (1, 0.9, 0.3, 1)

        _Radius ("Radius", Range(0,1)) = 0.45
        _EdgeThickness ("Edge Thickness", Range(0,0.5)) = 0.06
        _FillStrength ("Fill Strength", Range(0,1)) = 0.3
        _Opacity ("Opacity", Range(0,1)) = 0.85
        _Strength ("Strength", Range(0,1)) = 1.0

        _RotationSpeed ("Rotation Speed", Float) = 0.7
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseAmp ("Pulse Amplitude", Range(0,0.2)) = 0.02

        _SegmentCount ("Segment Count", Range(0,64)) = 24
        _SegmentGap ("Segment Gap (0-1)", Range(0,1)) = 0.35
        [Toggle(_SEGMENTS_ON)] _SegmentsOn ("Enable Dashed Segments", Float) = 1

        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _NoiseScale ("Noise Scale (x,y)", Vector) = (2.5,2.5,0,0)
        _Distortion ("Distortion", Range(0,0.5)) = 0.08

        [Toggle(_USE_SPRITE_ALPHA)] _UseSpriteAlpha ("Multiply by Sprite Alpha", Float) = 0
        [Toggle(_ADDITIVE)] _Additive ("Additive Blend", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        ZWrite Off
        Cull Off

        Pass
        {
            Name "SpriteUnlit2D"
            Tags { "LightMode" = "Universal2D" }

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIVE
            #pragma multi_compile _ _USE_SPRITE_ALPHA
            #pragma multi_compile _ _SEGMENTS_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            float4 _MainColor;
            float4 _EdgeColor;

            float _Radius;
            float _EdgeThickness;
            float _FillStrength;
            float _Opacity;
            float _Strength;

            float _RotationSpeed;
            float _PulseSpeed;
            float _PulseAmp;

            float _SegmentCount;
            float _SegmentGap;

            float2 _NoiseScale;
            float _Distortion;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            // Rotate a 2D vector by angle (radians)
            float2 rotate2D(float2 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float2(c*p.x - s*p.y, s*p.x + c*p.y);
            }

            // Smooth ring mask centered at 0.5 UV
            void ringMasks(float2 uv, float radius, float thickness, out float ring, out float fill, float distortion, float2 noiseScale, Texture2D noiseTex, SamplerState noiseSamp, float t)
            {
                float2 p = uv - 0.5;

                // Distortion with noise
                float2 nUV = uv * noiseScale + float2(t * 0.08, t * -0.06);
                float n = noiseTex.Sample(noiseSamp, nUV).r - 0.5;

                float d = length(p) + n * distortion;

                float inner = smoothstep(radius - thickness, radius, d);
                float outer = smoothstep(radius, radius + thickness, d);
                ring = saturate(inner - outer);

                fill = saturate(1.0 - smoothstep(0.0, radius, d));
            }

            // Dashed/segmented mask along angle
            float segmentMask(float2 uv, float radius, float segCount, float segGap, float rotAngle)
            {
                float2 p = uv - 0.5;
                if (length(p) < radius * 0.6) return 0; // keep center cleaner

                float2 pr = rotate2D(p, rotAngle);
                float ang = atan2(pr.y, pr.x); // [-pi,pi]
                ang = (ang + 3.14159265) / (2.0 * 3.14159265); // [0,1)

                float segCoord = frac(ang * max(segCount, 1.0));
                float vis = step(segCoord, saturate(1.0 - segGap));
                return vis;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float spriteAlpha = baseSample.a;

                float t = _Time.y;

                // Pulse radius
                float r = _Radius + sin(t * _PulseSpeed) * _PulseAmp;

                float ring, fill;
                ringMasks(IN.uv, r, _EdgeThickness, ring, fill, _Distortion, _NoiseScale, _NoiseTex, sampler_NoiseTex, t);

                // Optional dashed segments rotating
                float segMask = 1.0;
                #ifdef _SEGMENTS_ON
                    float rot = t * _RotationSpeed * 6.2831853; // radians/sec
                    segMask = segmentMask(IN.uv, r, _SegmentCount, _SegmentGap, rot);
                    ring *= lerp(1.0, segMask, 0.9);
                #endif

                // Compose colors
                float3 col = _EdgeColor.rgb * ring + _MainColor.rgb * (fill * _FillStrength);

                // Alpha
                float alpha = saturate((ring + fill * _FillStrength) * _Opacity * _Strength);

                #ifdef _USE_SPRITE_ALPHA
                    alpha *= spriteAlpha;
                #endif

                // Vertex tint
                col *= IN.color.rgb;
                alpha *= IN.color.a;

                float4 outCol = float4(col, alpha);

                #ifdef _ADDITIVE
                    outCol.rgb *= outCol.a;
                    outCol.a = saturate(alpha * 0.5);
                #endif

                return outCol;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
