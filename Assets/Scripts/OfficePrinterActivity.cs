using UnityEngine;

public sealed class OfficePrinterActivity : MonoBehaviour
{
    public Renderer readyIndicator,activityIndicator;
    [Min(3)]public float jobInterval=9;
    MaterialPropertyBlock ready,activity;
    float nextUpdate;
    void Awake(){ready=new MaterialPropertyBlock();activity=new MaterialPropertyBlock();}
    void Update()
    {
        if(Time.time<nextUpdate)return;nextUpdate=Time.time+.08f;
        float cycle=Mathf.Repeat(Time.time+transform.position.x,jobInterval);
        float busy=cycle<2.5f?Mathf.Pow(.5f+.5f*Mathf.Sin(cycle*9),2):0;
        Set(readyIndicator,ready,new Color(.12f,.85f,.55f),.65f+.08f*Mathf.Sin(Time.time*1.4f));
        Set(activityIndicator,activity,new Color(1,.53f,.1f),busy);
    }
    static void Set(Renderer renderer,MaterialPropertyBlock block,Color color,float intensity)
    {
        if(renderer==null)return;
        renderer.GetPropertyBlock(block);block.SetColor("_BaseColor",Color.Lerp(new Color(.055f,.09f,.1f),color,intensity));block.SetColor("_EmissionColor",color*intensity*1.4f);renderer.SetPropertyBlock(block);
    }
}
