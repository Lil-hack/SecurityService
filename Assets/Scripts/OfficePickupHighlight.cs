using UnityEngine;

// Per-renderer tint preserves textures and restores every original property block.
// Shared materials are never edited and no highlight meshes enter the physics scene.
public sealed class OfficePickupHighlight
{
    Renderer[] renderers;
    MaterialPropertyBlock[][] originals;

    public void Show(OfficePushableProp prop)
    {
        Clear();
        renderers=prop.GetComponentsInChildren<Renderer>();
        originals=new MaterialPropertyBlock[renderers.Length][];
        for(int i=0;i<renderers.Length;i++)
        {
            var materials=renderers[i].sharedMaterials;
            originals[i]=new MaterialPropertyBlock[materials.Length];
            for(int j=0;j<materials.Length;j++)
            {
                var original=new MaterialPropertyBlock(); renderers[i].GetPropertyBlock(original,j); originals[i][j]=original;
                var tint=new MaterialPropertyBlock(); renderers[i].GetPropertyBlock(tint,j);
                var material=materials[j]; if(material==null) continue;
                var baseColor=material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):Color.white;
                var selected=Color.Lerp(baseColor,new Color(1f,.76f,.15f,baseColor.a),.8f)*1.65f;
                selected.a=baseColor.a;
                tint.SetColor("_BaseColor",selected); tint.SetColor("_Color",selected);
                tint.SetColor("_EmissionColor",new Color(.5f,.28f,.025f));
                renderers[i].SetPropertyBlock(tint,j);
            }
        }
    }

    public void Clear()
    {
        if(renderers==null) return;
        for(int i=0;i<renderers.Length;i++)
            if(renderers[i]!=null)
                for(int j=0;j<originals[i].Length;j++)
                    renderers[i].SetPropertyBlock(originals[i][j].isEmpty?null:originals[i][j],j);
        renderers=null; originals=null;
    }
}
