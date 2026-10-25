#ifndef OFFICE_PLANT_BREEZE_INCLUDED
#define OFFICE_PLANT_BREEZE_INCLUDED
// Atlas colors and URP lighting remain untouched; the lower pot stays still.
float4 _BreezeParams;
float3 OfficeBreeze(float3 p)
{
    float h=saturate((p.y-_BreezeParams.x)/max(_BreezeParams.y,.001));
    float weight=smoothstep(.34,.95,h)*h;
    float3 origin=TransformObjectToWorld(float3(0,0,0));
    float phase=dot(origin.xz,float2(.73,1.37))+_BreezeParams.z;
    float time=_Time.y;
    float gust=sin(time*1.12+phase)+.22*sin(time*2.37+phase+p.y*3);
    p.x+=gust*_BreezeParams.y*_BreezeParams.w*weight;
    p.z+=sin(time*.87+phase+1.3)*_BreezeParams.y*_BreezeParams.w*.65*weight;
    return p;
}
#endif
