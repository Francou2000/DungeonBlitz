
Shader "URP/2D/Shield2D_SpriteUnlit_Procedural"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        [HDR]_MainColor ("Main Color", Color) = (0.2, 0.6, 1, 1)
        [HDR]_EdgeColor ("Edge Color", Color) = (0.6, 0.95, 1, 1)
        _Radius ("Radius", Range(0,1)) = 0.45
        _Thickness ("Thickness", Range(0,0.5)) = 0.08
        _Opacity ("Opacity", Range(0,1)) = 0.7
        _Strength ("Strength", Range(0,1)) = 1.0

        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _NoiseScale ("Noise Scale (x,y)", Vector) = (3,3,0,0)
        _Distortion ("Distortion", Range(0,1)) = 0.15

        _PulseSpeed ("Pulse Speed", Float) = 3.0
        _PulseAmp ("Pulse Amplitude", Range(0,1)) = 0.05

        [Toggle(_ADDITIVE)] _Additive ("Additive Blend", Float) = 0
        [Toggle(_USE_SPRITE_ALPHA)] _UseSpriteAlpha ("Multiply by Sprite Alpha", Float) = 1
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
            float _Thickness;
            float _Opacity;
            float _Strength;
            float2 _NoiseScale;
            float _Distortion;
            float _PulseSpeed;
            float _PulseAmp;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Base sprite sample
                float4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float spriteAlpha = baseSample.a;

                // Time
                float t = _Time.y;

                // Pulse
                float radiusDynamic = _Radius + sin(t * _PulseSpeed) * _PulseAmp;

                // Distortion
                float2 nUV = IN.uv * _NoiseScale + float2(t * 0.1, t * -0.07);
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nUV).r - 0.5;

                // Radial distance (distorted)
                float2 c = IN.uv - 0.5;
                float d = length(c) + noise * _Distortion;

                // Ring
                float inner = smoothstep(radiusDynamic - _Thickness, radiusDynamic, d);
                float outer = smoothstep(radiusDynamic, radiusDynamic + _Thickness, d);
                float ring = saturate(inner - outer);

                // Interior (subtle)
                float fill = saturate(1.0 - smoothstep(0.0, radiusDynamic, d)) * 0.15;

                // Color
                float3 col = _EdgeColor.rgb * ring + _MainColor.rgb * fill;

                // Alpha
                float alpha = saturate(ring * _Opacity * _Strength + fill * _Strength);

                #ifdef _USE_SPRITE_ALPHA
                    alpha *= spriteAlpha; // respect sprite transparency if desired
                #endif

                // Vertex tint
                col *= IN.color.rgb;
                alpha *= IN.color.a;

                float4 outCol = float4(col, alpha);

                #ifdef _ADDITIVE
                    // emulate additive look
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
