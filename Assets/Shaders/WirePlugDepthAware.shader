Shader "Custom/WirePlugDepthAware"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _SnapAlignmentThreshold ("Snap Alignment Threshold", Range(0, 1)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "SnappedDepthAware"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half alignment : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _SnapAlignmentThreshold;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 forwardWS = normalize(TransformObjectToWorldDir(float3(0, 0, 1)));
                output.alignment = abs(forwardWS.x);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                clip(_SnapAlignmentThreshold - input.alignment);
                return _BaseColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DraggingAlwaysVisible"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half alignment : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _SnapAlignmentThreshold;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 forwardWS = normalize(TransformObjectToWorldDir(float3(0, 0, 1)));
                output.alignment = abs(forwardWS.x);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                clip(input.alignment - _SnapAlignmentThreshold);
                return _BaseColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
