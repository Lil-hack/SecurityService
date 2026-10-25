Shader "Office/Animated Monitor"
{
    Properties
    {
        _ScreenBounds("Local XY bounds",Vector)=(-.33,.15,.66,.40)
        _PageSeconds("Seconds per picture",Range(4,20))=9
        _Brightness("Screen brightness",Range(.3,1.5))=.9
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _ScreenBounds;
                float _PageSeconds,_Brightness;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float phase:TEXCOORD1;};
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.uv=(v.positionOS.xy-_ScreenBounds.xy)/_ScreenBounds.zw;o.uv.x=1-o.uv.x;
                o.phase=dot(TransformObjectToWorld(float3(0,0,0)).xz,float2(.73,1.37));return o;
            }
            float Box(float2 p,float2 center,float2 halfSize,float radius)
            {
                float2 q=abs(p-center)-halfSize+radius;
                float d=length(max(q,0))+min(max(q.x,q.y),0)-radius;
                return 1-smoothstep(-.003,.003,d);
            }
            float Disc(float2 p,float2 center,float radius) {return 1-smoothstep(radius-.003,radius+.003,length(p-center));}
            float3 Page(float2 uv,float page,float time)
            {
                float2 p=uv*float2(1.7,1);
                float3 ink=float3(.075,.13,.2),paper=float3(.85,.91,.9),teal=float3(.08,.63,.61),amber=float3(1,.64,.20);
                float3 c=lerp(float3(.055,.13,.25),float3(.12,.31,.42),uv.y);
                if(page<1.5)
                {
                    c=lerp(c,paper,Box(p,float2(.85,.51),float2(.77,.405),.035));
                    c=lerp(c,ink,Box(p,float2(.85,.86),float2(.77,.055),.02));
                    for(int i=0;i<3;i++)c=lerp(c,i==0?amber:teal,Disc(p,float2(.15+i*.045,.86),.012));
                    c=lerp(c,float3(.7,.8,.81),Box(p,float2(.22,.47),float2(.1,.31),.018));
                    for(int j=0;j<5;j++)c=lerp(c,j==1?teal:float3(.49,.63,.66),Box(p,float2(.22,.69-j*.1),float2(.065,.012),.008));
                    if(page<.5)
                    {
                        c=lerp(c,float3(.97,.97,.9),Box(p,float2(.94,.45),float2(.53,.265),.025));
                        for(int k=0;k<5;k++)
                        {
                            float h=.12+.19*(.5+.5*sin(k*1.8+time*.35));
                            c=lerp(c,k==3?amber:teal,Box(p,float2(.54+k*.19,.25+h*.5),float2(.055,h*.5),.014));
                        }
                        c=lerp(c,ink,Box(p,float2(.70,.75),float2(.27,.012),.008));
                        c=lerp(c,teal,Box(p,float2(1.36,.75),float2(.12,.025),.015));
                        c=lerp(c,float3(.45,.57,.59),Box(p,float2(.91,.16),float2(.45,.006),.003));
                    }
                    else
                    {
                        c=lerp(c,ink,Box(p,float2(.72,.73),float2(.27,.014),.008));
                        for(int row=0;row<4;row++)for(int col=0;col<7;col++)
                        {
                            float2 cell=float2(.5+col*.155,.59-row*.112);
                            float3 color=fmod((float)(col+row*2),7)<.1?amber:float3(.96,.97,.94);
                            c=lerp(c,color,Box(p,cell,float2(.064,.043),.012));
                            c=lerp(c,teal,Box(p,cell+float2(-.018,.012),float2(.023,.006),.003));
                        }
                    }
                }
                else
                {
                    c=lerp(float3(.91,.61,.35),float3(.14,.40,.59),uv.y);
                    c=lerp(c,float3(1,.82,.43),Disc(p,float2(1.27,.72),.13));
                    float mountain=.20+.37*saturate(1-abs(uv.x-.35)*2.5);
                    c=lerp(c,float3(.26,.48,.51),1-smoothstep(mountain-.002,mountain+.002,uv.y));
                    float near=.13+.27*saturate(1-abs(uv.x-.78)*2.3);
                    c=lerp(c,float3(.07,.28,.35),1-smoothstep(near-.002,near+.002,uv.y));
                    float cloudX=frac(time*.008)*1.9-.1;
                    float clouds=max(Box(p,float2(cloudX,.77),float2(.13,.018),.016),Box(p,float2(cloudX+.5,.64),float2(.10,.014),.012));
                    c=lerp(c,float3(.87,.91,.87),clouds*.6);
                }
                c=lerp(c,ink,Box(p,float2(.85,.045),float2(.85,.035),.012));
                for(int icon=0;icon<4;icon++)c=lerp(c,icon==2?amber:teal,Box(p,float2(.68+icon*.11,.045),float2(.028,.018),.007));
                return c;
            }
            half4 Frag(Varyings v):SV_Target
            {
                float t=max(0,_Time.y+v.phase+100);
                float cycle=floor(t/_PageSeconds),local=fmod(t,_PageSeconds);
                float3 c=lerp(Page(v.uv,fmod(cycle,3),t),Page(v.uv,fmod(cycle+1,3),t),smoothstep(_PageSeconds-.8,_PageSeconds,local));
                float edge=smoothstep(0,.025,min(min(v.uv.x,1-v.uv.x),min(v.uv.y,1-v.uv.y)));
                return half4(c*_Brightness*lerp(.65,1,edge),1);
            }
            ENDHLSL
        }
    }
}
