using System;
using UnityEngine;
using UnityEngine.Rendering;

// An additional silhouette renderer. Original meshes, materials and physics stay intact.
[ExecuteAlways,DisallowMultipleComponent,DefaultExecutionOrder(900)]
public sealed class OfficeToonOutline : MonoBehaviour
{
    public OfficeOutlineSettings settings;
    public Material outlineMaterial;
    public Mesh outlineMesh;
    public Renderer[] sources;
    public SkinnedMeshRenderer animatedSource;
    public bool character;
    GameObject shell;
    Renderer outline;
    SkinnedMeshRenderer animatedOutline;
    MaterialPropertyBlock properties;
    static readonly int Width=Shader.PropertyToID("_OutlinePixels"),ColorId=Shader.PropertyToID("_OutlineColor"),Face=Shader.PropertyToID("_FaceScale"),Size=Shader.PropertyToID("_ObjectSize"),Distance=Shader.PropertyToID("_MaxDistance");

    void OnEnable()=>Rebuild();
    public void Rebuild()
    {
        Release();
        if(settings==null||outlineMaterial==null||outlineMesh==null||sources==null||sources.Length==0)return;
#if UNITY_EDITOR
        if(UnityEditor.EditorUtility.IsPersistent(this))return;
#endif
        shell=new GameObject("Toon silhouette (generated)"){hideFlags=HideFlags.HideAndDontSave};
        shell.transform.SetParent(transform,false);shell.layer=gameObject.layer;
        if(animatedSource!=null)
        {
            animatedOutline=shell.AddComponent<SkinnedMeshRenderer>();outline=animatedOutline;
            animatedOutline.sharedMesh=outlineMesh;animatedOutline.bones=animatedSource.bones;animatedOutline.rootBone=animatedSource.rootBone;
            animatedOutline.localBounds=animatedSource.localBounds;animatedOutline.quality=animatedSource.quality;
            animatedOutline.updateWhenOffscreen=animatedSource.updateWhenOffscreen;animatedOutline.forceMatrixRecalculationPerRender=true;
            animatedOutline.skinnedMotionVectors=false;
        }
        else
        {
            shell.AddComponent<MeshFilter>().sharedMesh=outlineMesh;outline=shell.AddComponent<MeshRenderer>();
        }
        var materials=new Material[Mathf.Max(1,outlineMesh.subMeshCount)];for(int i=0;i<materials.Length;i++)materials[i]=outlineMaterial;
        outline.sharedMaterials=materials;outline.shadowCastingMode=ShadowCastingMode.Off;outline.receiveShadows=false;
        outline.lightProbeUsage=LightProbeUsage.Off;outline.reflectionProbeUsage=ReflectionProbeUsage.Off;
        outline.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;
        properties=new MaterialPropertyBlock();LateUpdate();
    }
    void LateUpdate()
    {
        if(outline==null||settings==null||outlineMesh==null||sources==null)return;
        bool visible=false;foreach(var source in sources)if(source!=null&&source.enabled&&!source.forceRenderingOff&&source.gameObject.activeInHierarchy){visible=true;break;}
        if(animatedSource!=null)
        {
            visible=animatedSource.enabled&&!animatedSource.forceRenderingOff&&animatedSource.gameObject.activeInHierarchy;
            if(animatedOutline!=null&&animatedSource.sharedMesh!=null)
            {
                animatedOutline.localBounds=animatedSource.localBounds;
                for(int i=0;i<Math.Min(outlineMesh.blendShapeCount,animatedSource.sharedMesh.blendShapeCount);i++)animatedOutline.SetBlendShapeWeight(i,animatedSource.GetBlendShapeWeight(i));
            }
        }
        var camera=Camera.main;
        if(camera!=null&&Vector3.Distance(camera.transform.position,outline.bounds.center)>settings.maxDistance+outline.bounds.extents.magnitude)visible=false;
        outline.enabled=visible;if(!visible)return;
        shell.layer=gameObject.layer;
        properties.SetColor(ColorId,settings.color);properties.SetFloat(Width,character?settings.characterPixels:settings.propPixels);
        properties.SetFloat(Face,character?settings.faceAndHands:1);properties.SetFloat(Distance,settings.maxDistance);
        var scale=transform.lossyScale;properties.SetFloat(Size,outlineMesh.bounds.size.magnitude*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)));
        outline.SetPropertyBlock(properties);
    }
    void OnDisable()=>Release();
    void OnDestroy()=>Release();
    void Release()
    {
        if(shell!=null){shell.SetActive(false);if(Application.isPlaying)Destroy(shell);else DestroyImmediate(shell);}
        shell=null;outline=null;animatedOutline=null;
    }
}
