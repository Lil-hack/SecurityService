using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StudioRoundedPoufs
{
    [MenuItem("Tools/Office/Replace Cube Poufs")]
    public static void Apply()
    {
        if(Application.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
        var root=GameObject.Find("05 Open plan studio office");
        var poufs=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Colourful upholstered pouf").ToArray();
        if(poufs.Length==0) throw new System.InvalidOperationException("No upholstered poufs found.");
        var mesh=BuildMesh();
        const string path="Assets/OfficeExtension/Rounded upholstered pouf.asset";
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
        else {EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/Kitchen honey oak.mat");
        int i=0;
        foreach(var t in poufs)
        {
            Undo.RegisterFullObjectHierarchyUndo(t.gameObject,"Replace cube pouf");
            t.GetComponent<MeshFilter>().sharedMesh=saved;
            var mat=t.GetComponent<Renderer>().sharedMaterial;
            var old=t.Find("Tailored upholstery details");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var details=new GameObject("Tailored upholstery details").transform;details.SetParent(t,false);
            foreach(var c in t.GetComponents<Collider>())Object.DestroyImmediate(c);
            var collider=t.gameObject.AddComponent<CapsuleCollider>();collider.radius=.49f;collider.height=1f;collider.direction=1;
            // Root transforms, rigidbody and pickup component remain on the original object.
            var trim=new Material(mat);trim.name="Pouf piping "+i;
            trim.color=Color.Lerp(mat.color,Color.white,.22f);
            string trimPath="Assets/OfficeExtension/Materials/Pouf piping "+i+".mat";
            var previous=AssetDatabase.LoadAssetAtPath<Material>(trimPath);
            if(previous==null)AssetDatabase.CreateAsset(trim,trimPath);
            else {EditorUtility.CopySerialized(trim,previous);Object.DestroyImmediate(trim);trim=previous;}
            for(int j=0;j<48;j++)
            {
                float a=j*Mathf.PI*2/48,b=(j+1)*Mathf.PI*2/48;
                foreach(float y in new[]{-.33f,.34f})
                    Rod(details,"Stitched piping",new Vector3(Mathf.Cos(a)*.475f,y,Mathf.Sin(a)*.475f),new Vector3(Mathf.Cos(b)*.475f,y,Mathf.Sin(b)*.475f),.008f,trim);
            }
            for(int j=0;j<8;j++)
            {
                float a=j*Mathf.PI/4;
                Rod(details,"Upholstery panel seam",new Vector3(Mathf.Cos(a)*.499f,-.3f,Mathf.Sin(a)*.499f),new Vector3(Mathf.Cos(a)*.499f,.29f,Mathf.Sin(a)*.499f),.003f,trim);
            }
            Part(details,"Recessed oak plinth",new Vector3(0,-.46f,0),new Vector3(.77f,.035f,.77f),wood);
            Part(details,"Fabric covered central button",new Vector3(0,.488f,0),new Vector3(.07f,.009f,.07f),trim);
            // A carry tab supplies an asymmetrical silhouette on the back.
            Rod(details,"Leather carry tab",new Vector3(-.08f,.03f,.49f),new Vector3(.08f,.03f,.49f),.022f,wood);
            i++;
        }
        Physics.SyncTransforms();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
        File.WriteAllText("OfficePreview/RoundedPoufs_Result.txt","Replaced "+i+" cube poufs; preserved pickup components and rigidbodies.\n"+StudioPropPhysicsSetup.Validate());
        Preview();
    }
    static Mesh BuildMesh()
    {
        float[] heights={-.5f,-.47f,-.38f,-.22f,.22f,.36f,.46f,.49f,.5f};
        float[] radii={.36f,.43f,.48f,.5f,.5f,.48f,.40f,.22f,0f};
        const int sides=48;var verts=new Vector3[heights.Length*(sides+1)];var uv=new Vector2[verts.Length];
        for(int row=0;row<heights.Length;row++)for(int j=0;j<=sides;j++)
        {float a=j*Mathf.PI*2/sides;int k=row*(sides+1)+j;verts[k]=new Vector3(Mathf.Cos(a)*radii[row],heights[row],Mathf.Sin(a)*radii[row]);uv[k]=new Vector2((float)j/sides,(float)row/(heights.Length-1));}
        var tris=new System.Collections.Generic.List<int>();
        for(int row=0;row<heights.Length-1;row++)for(int j=0;j<sides;j++)
        {int k=row*(sides+1)+j;tris.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});}
        // Close the underside.
        for(int j=1;j<sides-1;j++)tris.AddRange(new[]{0,j,j+1});
        var mesh=new Mesh{name="Tailored round upholstered pouf"};mesh.vertices=verts;mesh.uv=uv;mesh.triangles=tris.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static GameObject Part(Transform root,string name,Vector3 p,Vector3 scale,Material mat)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static void Rod(Transform root,string name,Vector3 a,Vector3 b,float radius,Material mat)
    {var g=Part(root,name,(a+b)*.5f,new Vector3(radius*2,(b-a).magnitude*.5f,radius*2),mat);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);}
    [MenuItem("Tools/Office/Capture Rounded Poufs")]
    public static void Preview()
    {
        var go=new GameObject("Pouf preview"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();
        camera.transform.position=new Vector3(20.2f,2.8f,2f);camera.transform.LookAt(new Vector3(23.4f,.6f,5.3f));camera.fieldOfView=62;camera.nearClipPlane=.05f;
        var rt=new RenderTexture(1400,1000,24);var old=RenderTexture.active;var tex=new Texture2D(1400,1000,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1400,1000),0,0);tex.Apply();File.WriteAllBytes("OfficePreview/RoundedPoufs.png",tex.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
    }
}
