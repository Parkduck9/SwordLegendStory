// S8-1 먹선: 깊이·법선 차이로 외곽선을 찾아 먹색으로 덧그린다(URP FullScreenPassRendererFeature에서 사용).
// 멀어질수록 옅어져 안개에 묻힌다.
Shader "Hidden/GeomHyeop/InkOutline"
{
    Properties
    {
        _Thickness ("Thickness (px)", Float) = 1.2
        _DepthThreshold ("Depth Threshold", Float) = 0.6
        _NormalThreshold ("Normal Threshold", Float) = 0.45
        _InkColor ("Ink Color", Color) = (0.12, 0.11, 0.10, 1)
        _Strength ("Strength", Range(0, 1)) = 0.85
        _FadeDistance ("Fade Distance", Float) = 60
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "InkOutline"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float _Thickness;
            float _DepthThreshold;
            float _NormalThreshold;
            float4 _InkColor;
            float _Strength;
            float _FadeDistance;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float2 px = _Thickness / _ScreenParams.xy;
                float d0 = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float3 n0 = SampleSceneNormals(uv);
                float2 offsets[4] = { float2(px.x, 0), float2(-px.x, 0), float2(0, px.y), float2(0, -px.y) };
                float edge = 0;
                [unroll]
                for (int k = 0; k < 4; k++)
                {
                    float d = LinearEyeDepth(SampleSceneDepth(uv + offsets[k]), _ZBufferParams);
                    float3 n = SampleSceneNormals(uv + offsets[k]);
                    // 깊이 차이는 거리에 비례한 상대값으로 비교(멀리 있는 바닥에서 선이 생기지 않게)
                    edge = max(edge, step(_DepthThreshold * 0.05 * max(d0, 1.0), abs(d - d0)));
                    edge = max(edge, step(_NormalThreshold, 1.0 - dot(n, n0)));
                }
                float fade = saturate(1.0 - d0 / _FadeDistance);
                return lerp(color, half4(_InkColor.rgb, color.a), edge * _Strength * fade);
            }
            ENDHLSL
        }
    }
}
