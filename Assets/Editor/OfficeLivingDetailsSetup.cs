using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class OfficeLivingDetailsSetup
{
    const string Folder="Assets/ExecutiveOffice/LivingDetails";
    static Material steam,fanTeal,fanMetal,fanCream,fanDark,indicator;

    [MenuItem("Tools/Office/Apply Living Details and Toy Materials")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play before decorating the office.");
        string original=SceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/Textures");Directory.CreateDirectory(Folder+"/Meshes");AssetDatabase.Refresh();
        CreateWood();
        steam=Material("Coffee steam",Shader.Find("Office/Soft Coffee Steam"),Color.white);
        fanTeal=Material("Fan enamel teal",Shader.Find("Universal Render Pipeline/Lit"),new Color(.065f,.43f,.43f),.48f);
        fanMetal=Material("Fan brushed champagne",Shader.Find("Universal Render Pipeline/Lit"),new Color(.65f,.53f,.32f),.4f,.45f);
        fanCream=Material("Fan butter blades",Shader.Find("Universal Render Pipeline/Lit"),new Color(.91f,.76f,.43f),.32f);
        fanDark=Material("Fan rubber and controls",Shader.Find("Universal Render Pipeline/Lit"),new Color(.035f,.085f,.10f),.22f);
        indicator=Material("Printer status lens",Shader.Find("Universal Render Pipeline/Lit"),new Color(.08f,.5f,.35f),.45f);
        indicator.EnableKeyword("_EMISSION");indicator.SetColor("_EmissionColor",new Color(.05f,.5f,.3f));EditorUtility.SetDirty(indicator);
        int cups=0,plants=0,screens=0,printers=0;
        foreach(string path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/ExecutiveOffice.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);
            var renderers=Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>!r.name.StartsWith("Toon silhouette")).ToArray();
            FinishMaterials(renderers);
            foreach(var renderer in renderers)
            {
                string name=renderer.name;
                if(name.Contains("PotPlant")||name.Contains("Plant_Palm")||name.Contains("Plant_Fern")){Breeze(renderer);plants++;}
                if(name.Contains("Monitor")&&name.Contains("Screen_")){Monitor(renderer);screens++;}
            }
            foreach(var prop in Object.FindObjectsByType<OfficePushableProp>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(prop.name=="Coffee cup"||prop.name=="Desk coffee mug"||prop.name=="Guest mug"||prop.name=="SM_Item_Mug_01")
                {Steam(prop);cups++;}
                if(prop.name=="Shared office printer"){Printer(prop.transform);printers++;}
            }
            var details=GameObject.Find("Office living details");if(details!=null)Object.DestroyImmediate(details);
            var root=new GameObject("Office living details").transform;
            Fan(root,new Vector3(13.03f,1.065f,7.04f),0);
            Fan(root,new Vector3(1.95f,.94f,-4.98f),180);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        Debug.Log("Living details saved across both scenes: "+cups+" hot cups, "+plants+" breeze meshes, "+screens+" changing monitors, "+printers+" blinking printers, four oscillating desk fans. Toy finishes and wood grain applied.");
    }

    static Material Material(string name,Shader shader,Color color,float smooth=.2f,float metal=0)
    {
        string path=Folder+"/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
        material.shader=shader;if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
        if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",smooth);
        if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",metal);
        EditorUtility.SetDirty(material);return material;
    }
    static void CreateWood()
    {
        const int size=512;var height=new float[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=(float)x/size,v=(float)y/size;
            float warp=.034f*Mathf.Sin(u*Mathf.PI*2)+.012f*Mathf.Sin(u*Mathf.PI*6+1.2f)+.005f*Mathf.Sin((u*2+v*3)*Mathf.PI*2);
            height[y*size+x]=.5f+.28f*Mathf.Sin((v+warp)*Mathf.PI*2*15)+.12f*Mathf.Sin((v+warp*.8f)*Mathf.PI*2*43)+.10f*Mathf.Sin((v+warp*1.1f)*Mathf.PI*2*6);
        }
        var albedo=new Color[size*size];var normal=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float h=height[y*size+x],value=.83f+h*.16f;
            albedo[y*size+x]=new Color(value,value*.985f,value*.965f,1);
            float dx=height[y*size+(x+1)%size]-height[y*size+(x+size-1)%size];
            float dy=height[((y+1)%size)*size+x]-height[((y+size-1)%size)*size+x];
            var n=new Vector3(-dx*.7f,-dy*.7f,1).normalized;
            normal[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
        }
        SaveTexture("Warm wood grain",albedo,size,false);SaveTexture("Warm wood grain normal",normal,size,true);
    }
    static void SaveTexture(string name,Color[] pixels,int size,bool normal)
    {
        string path=Folder+"/Textures/"+name+".png";
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false,normal);texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        importer.sRGBTexture=!normal;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.mipmapEnabled=true;importer.anisoLevel=4;importer.maxTextureSize=512;importer.SaveAndReimport();
    }
    static void FinishMaterials(MeshRenderer[] renderers)
    {
        var wood=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/Warm wood grain.png");
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/Warm wood grain normal.png");
        foreach(var renderer in renderers)
        {
            var filter=renderer.GetComponent<MeshFilter>();var mesh=filter!=null?filter.sharedMesh:null;
            if(mesh!=null&&mesh.isReadable&&mesh.tangents.Length==0&&AssetDatabase.GetAssetPath(mesh).StartsWith("Assets/OfficeExtension/RoundedTables/"))
            {mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);}
        }
        var seen=new HashSet<Material>();
        foreach(var renderer in renderers)foreach(var material in renderer.sharedMaterials)
        {
            if(material==null||!seen.Add(material)||material.shader.name!="Universal Render Pipeline/Lit")continue;
            string n=material.name.ToLowerInvariant();
            bool timber=n=="walnut joinery"||n=="honey oak"||n=="kitchen honey oak"||n.Contains("oak light plank")||n.Contains("oak amber plank")||n=="notice walnut frame";
            bool wall=n.Contains("plaster")||n=="blue grey wall"||n=="kitchen charcoal wall";
            bool fabric=n.Contains("linen")||n.Contains("fabric")||n.Contains("upholstery")||n.Contains("pouf piping");
            bool lacquer=n=="porcelain white"||n=="kitchen porcelain"||n=="ocean blue"||n=="kitchen ocean"||n=="kitchen teal"||n=="kitchen tangerine"||n=="kitchen sunflower";
            if(timber)
            {
                if(material.GetTexture("_BaseMap")==null||material.GetTexture("_BaseMap")==wood)material.SetTexture("_BaseMap",wood);
                material.SetTexture("_BumpMap",normal);material.SetFloat("_BumpScale",.22f);material.EnableKeyword("_NORMALMAP");material.SetFloat("_Smoothness",.36f);
            }
            else if(wall)material.SetFloat("_Smoothness",.045f);
            else if(fabric)material.SetFloat("_Smoothness",.10f);
            else if(lacquer)material.SetFloat("_Smoothness",.39f);
            else if(n=="executive leather"||n=="navy leather")material.SetFloat("_Smoothness",.33f);
            else if(n.Contains("oak floor")||n=="honey oak parquet")material.SetFloat("_Smoothness",.29f);
            else continue;
            if(wall||fabric||timber||lacquer)material.SetFloat("_Metallic",0);
            EditorUtility.SetDirty(material);
        }
    }
    static void Breeze(MeshRenderer renderer)
    {
        var filter=renderer.GetComponent<MeshFilter>();var mesh=filter!=null?filter.sharedMesh:null;if(mesh==null)return;
        var materials=renderer.sharedMaterials;
        for(int i=0;i<materials.Length;i++)
        {
            var source=materials[i];if(source==null||source.shader.name=="Office/Plant Breeze")continue;
            string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string path=Folder+"/Materials/Plant breeze "+guid+"_"+mesh.name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(source);material.shader=Shader.Find("Office/Plant Breeze");AssetDatabase.CreateAsset(material,path);}
            material.SetVector("_BreezeParams",new Vector4(mesh.bounds.min.y,mesh.bounds.size.y,0,.032f));
            material.SetFloat("_Smoothness",.22f);EditorUtility.SetDirty(material);materials[i]=material;
        }
        renderer.sharedMaterials=materials;
        GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)&~StaticEditorFlags.BatchingStatic);
        EditorUtility.SetDirty(renderer);if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }
    static void Monitor(MeshRenderer renderer)
    {
        var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;string path=Folder+"/Materials/Office monitor pictures.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Office/Animated Monitor"));AssetDatabase.CreateAsset(material,path);}
        material.SetVector("_ScreenBounds",new Vector4(mesh.bounds.min.x,mesh.bounds.min.y,mesh.bounds.size.x,mesh.bounds.size.y));
        material.SetFloat("_PageSeconds",9);material.SetFloat("_Brightness",.9f);
        material.doubleSidedGI=false;EditorUtility.SetDirty(material);renderer.sharedMaterial=material;EditorUtility.SetDirty(renderer);
        GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)&~StaticEditorFlags.BatchingStatic);
        if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }
    static void Steam(OfficePushableProp cup)
    {
        var old=cup.transform.Find("Warm coffee steam");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var renderers=cup.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.name.StartsWith("Toon")).ToArray();if(renderers.Length==0)return;
        var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        var surface=cup.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Coffee surface"||t.name=="Ambient coffee surface");
        if(surface==null)
        {
            // Imported mugs include an empty white interior. Fill below their actual rim.
            Vector3 center=Vector3.zero;int count=0;
            foreach(var cupRenderer in renderers)
            {
                var filter=cupRenderer.GetComponent<MeshFilter>();var mesh=filter!=null?filter.sharedMesh:null;if(mesh==null)continue;
                using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
                {
                    var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp);
                    try{data[0].GetVertices(vertices);foreach(var v in vertices){var world=cupRenderer.transform.TransformPoint(v);if(world.y>bounds.max.y-.002f){center+=world;count++;}}}
                    finally{vertices.Dispose();}
                }
            }
            if(count>0)center/=count;else center=bounds.center;
            center.y=bounds.max.y-.012f;float diameter=Mathf.Min(bounds.size.x,bounds.size.z)*.74f;
            var coffee=Primitive(cup.transform,"Ambient coffee surface",PrimitiveType.Cylinder,Vector3.zero,Vector3.one,AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/Kitchen coffee.mat"));
            coffee.transform.SetPositionAndRotation(center,Quaternion.identity);var parentScale=cup.transform.lossyScale;
            coffee.transform.localScale=new Vector3(diameter/Mathf.Abs(parentScale.x),.001f/Mathf.Abs(parentScale.y),diameter/Mathf.Abs(parentScale.z));surface=coffee.transform;
        }
        Vector3 rim=surface!=null?surface.position:new Vector3(bounds.center.x,bounds.max.y-.006f,bounds.center.z);
        var g=new GameObject("Warm coffee steam");g.transform.SetParent(cup.transform,false);g.transform.SetPositionAndRotation(rim+Vector3.up*.009f,Quaternion.Euler(-90,0,0));
        var scale=cup.transform.lossyScale;g.transform.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
        var ps=g.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.duration=4;main.loop=true;main.playOnAwake=true;main.prewarm=true;main.maxParticles=24;main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;
        main.startLifetime=new ParticleSystem.MinMaxCurve(2.4f,3.1f);main.startSpeed=new ParticleSystem.MinMaxCurve(.07f,.105f);main.startSize=new ParticleSystem.MinMaxCurve(.022f,.035f);main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
        var emission=ps.emission;emission.rateOverTime=4.5f;
        var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=9;shape.radius=.009f;shape.length=.001f;
        var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.65f,1,3.2f));
        var colors=ps.colorOverLifetime;colors.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.92f,.94f,.96f),0),new GradientColorKey(new Color(.95f,.95f,.92f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.34f,.18f),new GradientAlphaKey(.16f,.6f),new GradientAlphaKey(0,1)});colors.color=gradient;
        var noise=ps.noise;noise.enabled=true;noise.strength=.018f;noise.frequency=.8f;noise.scrollSpeed=.22f;noise.quality=ParticleSystemNoiseQuality.Low;
        var renderer=g.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=steam;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;
        g.AddComponent<OfficeCoffeeSteam>().cup=cup.transform;
    }
    static GameObject Primitive(Transform parent,string name,PrimitiveType type,Vector3 local,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=local;g.transform.localScale=scale;
        Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;return g;
    }
    static void Printer(Transform printer)
    {
        var old=printer.Find("Printer living controls");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Printer living controls").transform;root.SetParent(printer,false);
        // Measured from this printer's upper lid, rather than its raised paper tray bounds.
        root.localPosition=new Vector3(.13f,.237f,-.014f);root.localRotation=Quaternion.Euler(6.4f,0,0);
        Primitive(root,"Inset control panel",PrimitiveType.Cube,Vector3.zero,new Vector3(.21f,.018f,.075f),fanDark);
        var ready=Primitive(root,"Ready light · mint",PrimitiveType.Sphere,new Vector3(-.055f,.014f,0),new Vector3(.027f,.012f,.027f),indicator).GetComponent<Renderer>();
        var activity=Primitive(root,"Printing light · amber",PrimitiveType.Sphere,new Vector3(.005f,.014f,0),new Vector3(.027f,.012f,.027f),indicator).GetComponent<Renderer>();
        Primitive(root,"Status button",PrimitiveType.Sphere,new Vector3(.065f,.014f,0),new Vector3(.025f,.011f,.025f),fanCream);
        var script=root.gameObject.AddComponent<OfficePrinterActivity>();script.readyIndicator=ready;script.activityIndicator=activity;
    }
    static Mesh RingMesh()
    {
        string path=Folder+"/Meshes/Fan cage ring.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
        var vertices=new List<Vector3>();var triangles=new List<int>();const int segments=64,sides=6;
        for(int i=0;i<segments;i++)for(int j=0;j<sides;j++){float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*(1+.018f*Mathf.Cos(b)),Mathf.Sin(a)*(1+.018f*Mathf.Cos(b)),.018f*Mathf.Sin(b)));}
        for(int i=0;i<segments;i++)for(int j=0;j<sides;j++){int a=i*sides+j,b=((i+1)%segments)*sides+j,c=i*sides+(j+1)%sides,d=((i+1)%segments)*sides+(j+1)%sides;triangles.AddRange(new[]{a,b,c,b,d,c});}
        mesh=new Mesh{name="Fine tubular fan guard"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static void Ring(Transform parent,float radius,float z,Material material)
    {
        var g=new GameObject("Rounded fan guard");g.transform.SetParent(parent,false);g.transform.localPosition=new Vector3(0,0,z);g.transform.localScale=Vector3.one*radius;
        g.AddComponent<MeshFilter>().sharedMesh=RingMesh();g.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    static void Fan(Transform parent,Vector3 position,float yaw)
    {
        var root=new GameObject("Retro oscillating desk fan").transform;root.SetParent(parent,false);root.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        Primitive(root,"Soft oval fan foot",PrimitiveType.Sphere,new Vector3(0,.035f,0),new Vector3(.37f,.07f,.27f),fanTeal);
        Primitive(root,"Fan neck",PrimitiveType.Capsule,new Vector3(0,.19f,.025f),new Vector3(.055f,.13f,.055f),fanMetal);
        for(int i=0;i<3;i++)Primitive(root,"Speed button",PrimitiveType.Sphere,new Vector3(-.07f+i*.07f,.067f,-.065f),new Vector3(.037f,.014f,.033f),i==1?fanCream:fanDark);
        var head=new GameObject("Oscillating fan head").transform;head.SetParent(root,false);head.localPosition=new Vector3(0,.46f,0);head.localRotation=Quaternion.Euler(-7,0,0);
        Primitive(head,"Enamel motor housing",PrimitiveType.Sphere,new Vector3(0,0,.055f),new Vector3(.16f,.16f,.18f),fanTeal);
        var rotor=new GameObject("Spinning three blade rotor").transform;rotor.SetParent(head,false);rotor.localPosition=new Vector3(0,0,-.035f);
        for(int i=0;i<3;i++)
        {
            var pivot=new GameObject("Blade pivot").transform;pivot.SetParent(rotor,false);pivot.localRotation=Quaternion.Euler(0,0,i*120+17);
            var blade=Primitive(pivot,"Rounded butter blade",PrimitiveType.Capsule,new Vector3(.025f,.112f,0),new Vector3(.095f,.092f,.018f),fanCream);blade.transform.localRotation=Quaternion.Euler(0,0,-18);
        }
        Primitive(rotor,"Brass rotor hub",PrimitiveType.Sphere,new Vector3(0,0,-.012f),new Vector3(.09f,.09f,.055f),fanMetal);
        Ring(head,.255f,-.067f,fanTeal);Ring(head,.255f,.035f,fanTeal);
        foreach(float radius in new[]{.10f,.18f})Ring(head,radius,-.085f,fanMetal);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI*2/8;var bar=Primitive(head,"Fine guard spoke",PrimitiveType.Capsule,new Vector3(Mathf.Sin(a)*.14f,Mathf.Cos(a)*.14f,-.085f),new Vector3(.006f,.115f,.006f),fanMetal);
            bar.transform.localRotation=Quaternion.Euler(0,0,-i*45);
        }
        Primitive(head,"Fan badge",PrimitiveType.Sphere,new Vector3(0,0,-.105f),new Vector3(.07f,.07f,.018f),fanTeal);
        var script=root.gameObject.AddComponent<OfficeFanMotion>();script.rotor=rotor;script.head=head;
    }
}
