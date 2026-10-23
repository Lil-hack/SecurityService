using UnityEngine;

// A planar reflection rendered only while the player is near the cabin.
public sealed class ElevatorMirror : MonoBehaviour
{
    public Renderer surface;
    Camera reflection; RenderTexture texture; Material material;
    void LateUpdate()
    {
        RenderFor(Camera.main);
    }
    public void RenderFor(Camera source)
    {
        if(source==null || surface==null || Vector3.Distance(source.transform.position,transform.position)>18)return;
        if(reflection==null)
        {
            var go=new GameObject("Elevator mirror camera");go.hideFlags=HideFlags.HideAndDontSave;
            reflection=go.AddComponent<Camera>();reflection.enabled=false;
            texture=new RenderTexture(1024,1024,24);texture.name="Elevator live mirror";
            material=new Material(surface.sharedMaterial);surface.sharedMaterial=material;material.SetTexture("_ReflectionTex",texture);
        }
        reflection.CopyFrom(source);reflection.enabled=false;reflection.targetTexture=texture;
        Vector3 n=transform.forward;float d=-Vector3.Dot(n,transform.position);
        Vector4 p=new Vector4(n.x,n.y,n.z,d);Matrix4x4 m=Matrix4x4.identity;
        for(int i=0;i<3;i++)for(int j=0;j<4;j++)m[i,j]-=2*p[i]*p[j];
        reflection.transform.position=m.MultiplyPoint(source.transform.position);
        reflection.worldToCameraMatrix=source.worldToCameraMatrix*m;
        Vector3 cp=reflection.worldToCameraMatrix.MultiplyPoint(transform.position+n*.015f);
        Vector3 cn=reflection.worldToCameraMatrix.MultiplyVector(n).normalized;
        reflection.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(cn.x,cn.y,cn.z,-Vector3.Dot(cp,cn)));
        material.SetMatrix("_MirrorVP",GL.GetGPUProjectionMatrix(reflection.projectionMatrix,true)*reflection.worldToCameraMatrix);
        bool visible=surface.enabled;bool inverted=GL.invertCulling;
        try{surface.enabled=false;GL.invertCulling=!inverted;reflection.Render();}
        finally{GL.invertCulling=inverted;surface.enabled=visible;}
    }
    void OnDisable()
    {
        if(reflection!=null)Destroy(reflection.gameObject);if(texture!=null){texture.Release();Destroy(texture);}if(material!=null)Destroy(material);
    }
}
