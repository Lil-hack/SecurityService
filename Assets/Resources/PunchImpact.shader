Shader "Office/Punch Impact"
{
 Properties { _Round("Round particles",Float)=1 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   float _Round;
   struct A {float4 p:POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;};
   struct V {float4 p:SV_POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;};
   V Vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.c=a.c;v.uv=a.uv;return v;}
   half4 Frag(V v):SV_Target {float2 p=v.uv*2-1; float a=atan2(p.y,p.x); float edge=.83+.10*cos(a*5)+.05*sin(a*3); float alpha=1-smoothstep(edge*.4,edge,length(p));return half4(v.c.rgb,v.c.a*alpha*alpha);}
   ENDHLSL
  }
 }
}

