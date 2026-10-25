Shader "Office/Toon Silhouette"
{
    Properties
    {
        _OutlineColor("Ink color", Color)=(0.045,0.062,0.085,1)
        _OutlinePixels("Width in pixels", Float)=1.7
        _FaceScale("Face and hand thickness", Float)=0.55
        _ObjectSize("Object world size", Float)=1
        _MaxDistance("Maximum distance", Float)=20
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+20" "RenderType"="Opaque" }
        Pass
        {
            Name "Silhouette"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlinePixels, _FaceScale, _ObjectSize, _MaxDistance;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float visible:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world=TransformObjectToWorld(input.positionOS.xyz);
                float3 normal=TransformObjectToWorldNormal(input.normalOS);
                float4 position=TransformWorldToHClip(world);
                float4 projectedNormal=mul(UNITY_MATRIX_VP,float4(normal,0));
                float2 direction=(projectedNormal.xy*position.w-position.xy*projectedNormal.w)*_ScaledScreenParams.xy;
                direction*=rsqrt(max(dot(direction,direction),1e-8));
                float viewDepth=max(abs(TransformWorldToView(world).z),.01);
                float pixelSize=_ObjectSize*_ScaledScreenParams.y*.5*abs(UNITY_MATRIX_P._m11)/(unity_OrthoParams.w>.5?1:viewDepth);
                float fade=saturate((_MaxDistance-distance(_WorldSpaceCameraPos,world))/3);
                float width=_OutlinePixels*lerp(_FaceScale,1,input.color.a)*saturate(pixelSize/32)*fade;
                position.xy+=direction*(2*width/_ScaledScreenParams.xy)*position.w;
                output.positionCS=position;output.visible=width;return output;
            }
            half4 Frag(Varyings input):SV_Target { clip(input.visible-.05);return _OutlineColor; }
            ENDHLSL
        }
    }
}
