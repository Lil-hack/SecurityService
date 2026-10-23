Shader "Office/ElevatorMirror"
{
 Properties { _ReflectionTex("Reflection",2D)="gray"{} }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass
  {
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_ReflectionTex); SAMPLER(sampler_ReflectionTex);float4x4 _MirrorVP;
   struct A {float4 positionOS:POSITION;};struct V {float4 positionCS:SV_POSITION;float4 projected:TEXCOORD0;};
   V vert(A v){V o;float3 w=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(w);o.projected=mul(_MirrorVP,float4(w,1));return o;}
   half4 frag(V i):SV_Target {float2 uv=i.projected.xy/i.projected.w*.5+.5;uv.y=1-uv.y;return half4(SAMPLE_TEXTURE2D(_ReflectionTex,sampler_ReflectionTex,uv).rgb*.94+half3(.025,.03,.035),1);}
   ENDHLSL
  }
 }
}

