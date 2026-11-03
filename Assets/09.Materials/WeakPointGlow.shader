// 약점 표시용 셰이더: 반투명한 붉은 구가 가장자리는 짙고 안쪽은 옅게, 밝은 붉은색과 짙은 붉은색이 물결치며 일렁인다.
// 라이팅과 상관없이 같은 색으로 보이는 Unlit이고, 깊이는 쓰지 않아 뒤의 것을 가리지 않는다.
Shader "Game/WeakPointGlow"
{
    Properties
    {
        [HDR] _Color ("Color (물결 마루의 밝은 색)", Color) = (1, 0.05, 0.05, 1)
        [HDR] _DarkColor ("Dark Color (물결 골의 짙은 색)", Color) = (0.3, 0, 0.02, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.5
        _CoreAlpha ("Core Alpha (안쪽 투명도 비율)", Range(0, 1)) = 0.35
        _RimPower ("Rim Power (클수록 가장자리만 짙어짐)", Range(0.5, 8)) = 2
        _PulseSpeed ("Pulse Speed", Float) = 2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.3
        _WaveScale ("Wave Scale (일렁임 물결 크기)", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _DarkColor;
                half _Alpha;
                half _CoreAlpha;
                half _RimPower;
                half _PulseSpeed;
                half _PulseAmount;
                half _WaveScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                half fresnel = pow(1.0h - saturate(dot(normalize(input.normalWS), viewDirWS)), _RimPower);

                // 위치마다 위상이 달라 물결이 번지듯 일렁인다.
                half wave = sin(_Time.y * _PulseSpeed + dot(input.positionWS, float3(1.0, 1.3, 0.7)) * _WaveScale) * 0.5h + 0.5h;
                half pulse = (1.0h - _PulseAmount) + (_PulseAmount * wave);

                half alpha = _Alpha * lerp(_CoreAlpha, 1.0h, fresnel) * pulse;

                // 물결의 마루는 밝은 색, 골은 짙은 색이라 일렁임이 색 대비로도 보인다.
                half3 color = lerp(_DarkColor.rgb, _Color.rgb, wave);
                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
