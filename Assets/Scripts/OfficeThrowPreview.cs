using UnityEngine;
using UnityEngine.Rendering;

public sealed class OfficeThrowPreview
{
    readonly GameObject root;
    readonly LineRenderer arc, halo, ring, outerRing, impactDot;
    readonly LineRenderer[] pulses=new LineRenderer[5];
    readonly Material material;
    readonly Vector3[] points=new Vector3[241];
    public bool Visible { get { return root!=null && root.activeSelf; } }

    public OfficeThrowPreview(Transform owner)
    {
        root=new GameObject("Throw trajectory and impact ring");
        root.transform.SetParent(owner,false); root.layer=2;
        var shader=Shader.Find("Office/Throw Trajectory");
        if(shader==null) shader=Shader.Find("Universal Render Pipeline/Unlit");
        if(shader==null) shader=Shader.Find("Sprites/Default");
        material=new Material(shader){name="Throw aim indicator",hideFlags=HideFlags.HideAndDontSave};
        if(material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",Color.white);
        halo=Line("Soft trajectory glow",.065f); arc=Line("Fine trajectory core",.016f);
        arc.widthCurve=new AnimationCurve(new Keyframe(0,.55f),new Keyframe(.12f,1),new Keyframe(1,.45f));
        ring=Line("Impact ring",.018f); outerRing=Line("Impact halo",.035f);
        ring.loop=true; outerRing.loop=true;
        impactDot=Line("Impact center",.045f); impactDot.positionCount=2;
        for(int i=0;i<pulses.Length;i++) pulses[i]=Line("Forward trajectory pulse",.026f);
    }

    LineRenderer Line(string name,float width)
    {
        var go=new GameObject(name); go.transform.SetParent(root.transform,false); go.layer=2;
        var line=go.AddComponent<LineRenderer>(); line.sharedMaterial=material;
        line.useWorldSpace=true; line.widthMultiplier=width; line.numCornerVertices=6; line.numCapVertices=8;
        line.shadowCastingMode=ShadowCastingMode.Off; line.receiveShadows=false;
        return line;
    }

    public void Refresh(OfficePropGrabber grabber)
    {
        int count=grabber.PredictThrow(points,out var impact,out var normal);
        if(count<2) { Hide(); return; }
        root.SetActive(true); arc.positionCount=halo.positionCount=count;
        for(int i=0;i<count;i++) { arc.SetPosition(i,points[i]); halo.SetPosition(i,points[i]); }
        var color=Color.Lerp(new Color(.42f,.85f,1),new Color(1,.77f,.3f),grabber.ThrowCharge01);
        arc.colorGradient=Gradient(Color.Lerp(Color.white,color,.25f),color,.9f,.8f);
        halo.colorGradient=Gradient(color,color,.18f,.06f);
        for(int i=0;i<pulses.Length;i++)
        {
            float sample=Mathf.Repeat(Time.unscaledTime*.4f+i/(float)pulses.Length,1)*(count-1);
            int index=Mathf.Min((int)sample,count-2);
            var point=Vector3.Lerp(points[index],points[index+1],sample-index);
            var tangent=(points[index+1]-points[index]).normalized;
            pulses[i].positionCount=2; pulses[i].SetPosition(0,point);
            pulses[i].SetPosition(1,Vector3.MoveTowards(point,points[index+1],.06f));
            pulses[i].startColor=pulses[i].endColor=new Color(1,.97f,.85f,.9f);
        }
        ring.enabled=normal.sqrMagnitude>.1f;
        outerRing.enabled=impactDot.enabled=ring.enabled;
        if(!ring.enabled) return;
        var rotation=Quaternion.FromToRotation(Vector3.up,normal);
        ring.startColor=ring.endColor=new Color(color.r,color.g,color.b,.95f);
        outerRing.startColor=outerRing.endColor=new Color(color.r,color.g,color.b,.2f);
        impactDot.startColor=impactDot.endColor=new Color(1,.97f,.85f,.95f);
        float pulse=.012f*Mathf.Sin(Time.unscaledTime*4);
        ring.positionCount=outerRing.positionCount=72;
        for(int i=0;i<72;i++)
        {
            float angle=i*Mathf.PI*2/72;
            var radial=rotation*new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            ring.SetPosition(i,impact+normal*.018f+radial*.20f);
            outerRing.SetPosition(i,impact+normal*.018f+radial*(.255f+pulse));
        }
        impactDot.SetPosition(0,impact+normal*.022f); impactDot.SetPosition(1,impact+normal*.027f);
    }

    static Gradient Gradient(Color start,Color end,float alphaStart,float alphaEnd)
    {
        var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(start,0),new GradientColorKey(end,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(alphaStart,.08f),new GradientAlphaKey(alphaEnd,1)});
        return gradient;
    }

    public void Hide() { if(root!=null) root.SetActive(false); }
    public void Dispose()
    {
        if(root!=null) Object.Destroy(root);
        if(material!=null) Object.Destroy(material);
    }
}
