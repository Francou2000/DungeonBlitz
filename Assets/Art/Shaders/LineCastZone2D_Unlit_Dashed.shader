
Shader "URP/2D/LineCastZone2D_Unlit_Dashed"
{
    Properties
    {
        [MainTexture]_MainTex ("Sprite", 2D) = "white" {}
        [HDR]_Color ("Color", Color) = (0.2, 0.8, 1.0, 1)

        _LineWidth ("Line Width", Range(0,1)) = 0.12
        _Feather ("Feather (Edge Softness)", Range(0,0.5)) = 0.06

        _Opacity ("Opacity", Range(0,1)) = 0.9
        _Strength ("Strength", Range(0,1)) = 1.0

        _RotationDegrees ("Rotation (Degrees)", Range(0,180)) = 0.0

        // Dashed segments controls
        _SegmentCount ("Segments Per UV", Range(1,128)) = 36
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

            float4 _Color;
            float _LineWidth;
            float _Feather;
            float _Opacity;
            float _Strength;
            float _RotationDegrees;
            float _SegmentCount;
            float _SegmentGap;
            float _DashScroll;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            float2 rotate2D(float2 p, float aRad)
            {
                float s = sin(aRad), c = cos(aRad);
                return float2(c*p.x - s*p.y, s*p.x + c*p.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y;
                float aRad = radians(_RotationDegrees);

                // Centered UV in [-0.5, 0.5] and rotate so the line runs along X
                float2 uv = IN.uv - 0.5;
                float2 pr = rotate2D(uv, aRad);

                // Line mask: soft band around Y=0
                float halfWidth = _LineWidth * 0.5;
                float d = abs(pr.y);
                float fill = 1.0 - smoothstep(halfWidth, halfWidth + _Feather, d);

                // Dashes along X
                float segCoord = frac((pr.x + t * _DashScroll + 0.5) * max(_SegmentCount, 1.0));
                float dashMask = step(segCoord, saturate(1.0 - _SegmentGap));

                float mask = fill * dashMask;

                float3 col = _Color.rgb;
                float alpha = saturate(mask * _Opacity * _Strength);

                #ifdef _USE_SPRITE_ALPHA
                    alpha *= IN.color.a;
                #endif

                col *= IN.color.rgb;

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
