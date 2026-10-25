Shader "Office/Airborne Dust"
{
    Properties
    {
        _NearFade("Fade near the camera",Range(.1,2))=.65
        _FarFade("Fade at distance",Range(5,40))=20
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest LEqual Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _NearFade,_FarFade;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float fade:TEXCOORD1;};
            Varyings Vert(Attributes v)
            {
                Varyings o;float3 world=TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(world);o.uv=v.uv;o.color=v.color;
                float distanceToCamera=distance(world,_WorldSpaceCameraPos);
                o.fade=smoothstep(_NearFade,_NearFade+.65,distanceToCamera)*(1-smoothstep(_FarFade-4,_FarFade,distanceToCamera));
                return o;
            }
            half4 Frag(Varyings v):SV_Target
            {
                float2 p=v.uv*2-1;
                // A small round, soft speck, without square cards, streaks or star glints.
                float profile=pow(saturate(1-dot(p,p)),1.65);
                half alpha=v.color.a*profile*v.fade;
                clip(alpha-.003);return half4(v.color.rgb,alpha);
            }
            ENDHLSL
        }
    }
}
