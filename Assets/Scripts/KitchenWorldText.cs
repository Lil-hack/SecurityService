using UnityEngine;

// Keep dynamic font atlas changes while rendering ordinary, depth-tested world text.
[ExecuteAlways, RequireComponent(typeof(TextMesh)), DisallowMultipleComponent]
public sealed class KitchenWorldText : MonoBehaviour
{
    Material material;
    void OnEnable() { Font.textureRebuilt+=FontRebuilt; Refresh(); }
    void OnDisable() { Font.textureRebuilt-=FontRebuilt; if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);} }
    void FontRebuilt(Font font) { if(font==GetComponent<TextMesh>().font)Refresh(); }
    public void Refresh()
    {
        var text=GetComponent<TextMesh>();if(text.font==null)return;
        if(material==null)
        {
            var shader=Resources.Load<Shader>("KitchenWorldText");if(shader==null)return;
            material=new Material(shader){name="Kitchen depth-tested font",hideFlags=HideFlags.HideAndDontSave};
        }
        material.mainTexture=text.font.material.mainTexture;
        GetComponent<MeshRenderer>().sharedMaterial=material;
    }
}
