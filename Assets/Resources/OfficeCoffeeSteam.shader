Shader "Office/Soft Coffee Steam"
{
    Properties { _Tint("Steam tint",Color)=(.94,.96,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            Varyings Vert(Attributes v) { Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color*_Tint;return o; }
            half4 Frag(Varyings v):SV_Target
            {
                float2 p=v.uv*2-1;
                float r=dot(p,p);
                float cloud=pow(saturate(1-r),2.2);
                float lobe=saturate(1-dot(p-float2(.25,.16),p-float2(.25,.16)));
                half alpha=v.color.a*cloud*lerp(.75,1,lobe);
                clip(alpha-.002);return half4(v.color.rgb,alpha);
            }
            ENDHLSL
        }
    }
}
