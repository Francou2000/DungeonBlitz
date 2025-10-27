
Shader "URP/2D/LineBeam2D_Unlit_Procedural"
{
    Properties
    {
        [MainTexture]_MainTex ("Sprite", 2D) = "white" {}
        [HDR]_MainColor ("Main Color", Color) = (0.2, 0.8, 1.0, 1)
        [HDR]_EdgeColor ("Edge Color", Color) = (0.6, 1.0, 1.0, 1)

        _LineWidth ("Line Width", Range(0,1)) = 0.12
        _EdgeThickness ("Edge Thickness", Range(0,0.5)) = 0.06
        _Opacity ("Opacity", Range(0,1)) = 0.9
        _Strength ("Strength", Range(0,1)) = 1.0

        _RotationDegrees ("Rotation (Degrees)", Range(0,180)) = 0.0
        _PulseSpeed ("Pulse Speed", Float) = 2.5
        _PulseAmp ("Pulse Amplitude", Range(0,0.2)) = 0.03

        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _NoiseScale ("Noise Scale (x,y)", Vector) = (3,1.5,0,0)
        _Distortion ("Distortion", Range(0,0.5)) = 0.08
        _NoiseScroll ("Noise Scroll", Float) = 0.6

        // Dashed segments
        [Toggle(_SEGMENTS_ON)] _SegmentsOn ("Enable Dashes", Float) = 1
        _SegmentCount ("Segments Per UV (0=off)", Range(0,128)) = 36
        _SegmentGap ("Segment Gap (0-1)", Range(0,1)) = 0.35
        _DashScroll ("Dash Scroll Speed", Float) = 0.6

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

            float _LineWidth;
            float _EdgeThickness;
            float _Opacity;
            float _Strength;

            float _RotationDegrees;
            float _PulseSpeed;
            float _PulseAmp;

            float2 _NoiseScale;
            float _Distortion;
            float _NoiseScroll;

            float _SegmentCount;
            float _SegmentGap;
            float _DashScroll;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            float2 rotate2D(float2 p, float aRad)
            {
                float s = sin(aRad);
                float c = cos(aRad);
                return float2(c*p.x - s*p.y, s*p.x + c*p.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float spriteAlpha = baseSample.a;

                float t = _Time.y;
                float aRad = radians(_RotationDegrees);

                // Centered UV in [-0.5, 0.5]
                float2 uv = IN.uv - 0.5;
                // Rotate so line runs along +X axis in rotated space
                float2 pr = rotate2D(uv, aRad);

                // Distortion noise (scrolls along line direction X)
                float2 nUV = IN.uv * _NoiseScale + float2(t * _NoiseScroll, 0);
                float n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nUV).r - 0.5;

                // Distance to line center (Y=0 in rotated space), with distortion
                float d = abs(pr.y + n * _Distortion);

                // Pulse widens/narrows the line
                float halfWidth = _LineWidth * 0.5 + sin(t * _PulseSpeed) * _PulseAmp;

                // Smooth band around Y=0
                float inner = smoothstep(halfWidth - _EdgeThickness, halfWidth, d);
                float outer = smoothstep(halfWidth, halfWidth + _EdgeThickness, d);
                float edge = saturate(inner - outer);

                // Fill inside line (softer)
                float fill = 1.0 - smoothstep(0.0, halfWidth, d);
                fill *= 0.35; // softer than edge

                // Optional dashed segments along X (line direction)
                #ifdef _SEGMENTS_ON
                    float sCount = max(_SegmentCount, 1.0);
                    float segCoord = frac((pr.x + t * _DashScroll + 0.5) * sCount);
                    float segVisible = step(segCoord, saturate(1.0 - _SegmentGap));
                    edge *= segVisible;
                    fill *= segVisible;
                #endif

                // Compose color
                float3 col = _EdgeColor.rgb * edge + _MainColor.rgb * fill;

                // Alpha
                float alpha = saturate((edge + fill) * _Opacity * _Strength);

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
