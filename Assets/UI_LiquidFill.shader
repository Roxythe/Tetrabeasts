Shader "Custom/UI_LiquidFill"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData]_AlphaTex ("External Alpha", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Aspect ("UV Aspect (Width/Height)", Float) = 1

        _BubbleColor ("Bubble Color", Color) = (1,1,1,1)
        _BubbleCutoff ("Bubble Cutoff", Range(0, 1)) = 0.55
        _BubblePower ("Bubble Contrast (Power)", Range(0.5, 8)) = 3

        _NoiseTex ("Noise (Grayscale)", 2D) = "gray" {}
        _BubbleTex ("Bubbles (Mask)", 2D) = "black" {}

        _NoiseScale ("Noise Scale", Float) = 3
        _NoiseStrength ("Noise Strength", Range(0, 0.2)) = 0.06
        _NoiseSpeed ("Noise Speed (XY)", Vector) = (0.18, 0.05, 0, 0)

        _BubbleScale ("Bubble Scale", Float) = 6
        _BubbleIntensity ("Bubble Intensity", Range(0, 2)) = 0.6
        _BubbleSpeed ("Bubble Speed (XY)", Vector) = (0.00, 0.35, 0, 0)

        _BubbleSpawnChance ("Bubble Spawn Chance", Range(0, 1)) = 0.35
        _BubbleJitter ("Bubble Cell Jitter", Range(0, 0.25)) = 0.08

        _BubblePatchScale ("Bubble Patch Scale", Float) = 1.2
        _BubblePatchCutoff ("Bubble Patch Cutoff", Range(0, 1)) = 0.55
        _BubblePatchSpeed ("Bubble Patch Speed (XY)", Vector) = (0.03, 0.06, 0, 0)
        _BubblePatchStrength ("Bubble Patch Strength", Range(0, 1)) = 0

        _EdgeFoam ("Top Foam Height", Range(0, 0.5)) = 0.10
        _FoamIntensity ("Foam Intensity", Range(0, 1)) = 0.12

        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15

        [HideInInspector]_ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
        [HideInInspector]_UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UI"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ UNITY_ETC1_EXTERNAL_ALPHA

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex);  SAMPLER(sampler_AlphaTex);
            TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_BubbleTex); SAMPLER(sampler_BubbleTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;

                float _Aspect;

                float4 _BubbleColor;
                float _BubbleCutoff;
                float _BubblePower;

                float _NoiseScale;
                float _NoiseStrength;
                float4 _NoiseSpeed;

                float _BubbleScale;
                float _BubbleIntensity;
                float4 _BubbleSpeed;

                float _BubbleSpawnChance;
                float _BubbleJitter;

                float _BubblePatchScale;
                float _BubblePatchCutoff;
                float4 _BubblePatchSpeed;
                float _BubblePatchStrength;

                float _EdgeFoam;
                float _FoamIntensity;

                float4 _ClipRect;
                float _UseUIAlphaClip;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                float4 worldPos    : TEXCOORD1;
            };

            float UnityGet2DClipping(float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position) * step(position, clipRect.zw);
                return inside.x * inside.y;
            }

            half SampleSpriteAlpha(float2 uv)
            {
            #ifdef UNITY_ETC1_EXTERNAL_ALPHA
                return SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r;
            #else
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            #endif
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv = IN.uv;

                OUT.color = IN.color * _Color;
                OUT.worldPos = IN.positionOS;
                return OUT;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 Hash22(float2 p)
            {
                float n = Hash21(p);
                return float2(n, Hash21(p + n + 17.0));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y;
                float2 uv = IN.uv;

                float2 nUV = uv * _NoiseScale + (t * _NoiseSpeed.xy);
                float n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nUV).r;
                n = (n * 2.0 - 1.0);

                float2 duv = uv + float2(n * _NoiseStrength, 0);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, duv) * IN.color;

                col.a *= SampleSpriteAlpha(duv);

                float wave = sin((uv.x * 10.0) + (t * 2.5) + (n * 2.0)) * 0.04; 
                float shade = 1.0 + wave;
                col.rgb *= shade;

                float2 uvAspect = float2(uv.x * _Aspect, uv.y);

                float2 bBase = uvAspect * _BubbleScale;

                float2 cell = floor(bBase);
                float2 rnd2 = Hash22(cell);
                float spawnMask = step(1.0 - _BubbleSpawnChance, rnd2.x);

                float2 jitter = (rnd2 - 0.5) * _BubbleJitter;
                float2 bUV = bBase + jitter + (t * _BubbleSpeed.xy);
                bUV.x += sin(t * 1.7 + uv.y * 8.0) * 0.02;

                float2 patchUV = uvAspect * _BubblePatchScale + (t * _BubblePatchSpeed.xy);

                patchUV = frac(patchUV);

                float patch = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, patchUV).r;
                patch = smoothstep(_BubblePatchCutoff, 1.0, patch);

                float b = SAMPLE_TEXTURE2D(_BubbleTex, sampler_BubbleTex, bUV).r;
                b = pow(saturate(b), _BubblePower);
                b = smoothstep(_BubbleCutoff, 1.0, b);

                b *= spawnMask;
                float patchMin = 0.15;
                b *= lerp(1.0, max(patch, patchMin), _BubblePatchStrength);

                float bubbleFade = saturate(1.0 - uv.y * 0.6);
                b *= bubbleFade;

                col.rgb = lerp(col.rgb, _BubbleColor.rgb, b * _BubbleIntensity * 0.35);
                col.rgb += _BubbleColor.rgb * (b * _BubbleIntensity * 0.65);

                float foamMask = smoothstep(1.0 - _EdgeFoam, 1.0, uv.y);
                col.rgb += foamMask * _FoamIntensity;

            #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPos.xy, _ClipRect);
            #endif

            #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
            #endif

                return col;
            }
            ENDHLSL
        }
    }
}