using UnityEngine;

public static class OfficeThrowHud
{
    static GUIStyle title, small, number, centered;
    static readonly Color Cyan=new Color(.43f,.88f,1), Gold=new Color(1,.77f,.3f);

    static void Rounded(Rect rect,Color color,float radius)
    {
        GUI.DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,color,0,radius);
    }

    public static void Draw(bool carrying,bool target,bool charging,float charge)
    {
        if(title==null)
        {
            title=new GUIStyle(GUI.skin.label){fontSize=16,fontStyle=FontStyle.Bold};
            small=new GUIStyle(GUI.skin.label){fontSize=13};
            number=new GUIStyle(GUI.skin.label){fontSize=21,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleRight};
            centered=new GUIStyle(GUI.skin.label){fontSize=14,alignment=TextAnchor.MiddleCenter};
        }
        var previous=GUI.color; GUI.color=Color.white;
        var accent=target?Gold:Color.Lerp(Cyan,Gold,charge);
        float cx=Screen.width*.5f, cy=Screen.height*.5f;
        Rounded(new Rect(cx-1,cy-8,2,4),accent,.8f); Rounded(new Rect(cx-1,cy+4,2,4),accent,.8f);
        Rounded(new Rect(cx-8,cy-1,4,2),accent,.8f); Rounded(new Rect(cx+4,cy-1,4,2),accent,.8f);
        if(target && !carrying)
        {
            Rounded(new Rect(cx-120,cy+30,240,40),new Color(.035f,.05f,.075f,.92f),10);
            Rounded(new Rect(cx-110,cy+38,26,24),Gold,5);
            GUI.color=new Color(.1f,.08f,.03f); GUI.Label(new Rect(cx-110,cy+38,26,24),"F",centered);
            GUI.color=Color.white; GUI.Label(new Rect(cx-74,cy+38,190,24),"Взять в две руки",small);
        }
        if(carrying)
        {
            float x=cx-174, y=Screen.height-142;
            Rounded(new Rect(x,y+4,348,104),new Color(0,0,0,.22f),14);
            Rounded(new Rect(x,y,348,104),new Color(accent.r,accent.g,accent.b,.38f),14);
            Rounded(new Rect(x+1,y+1,346,102),new Color(.025f,.04f,.065f,.95f),13);
            GUI.color=Color.white; GUI.Label(new Rect(x+18,y+12,220,26),"Бросок вперёд",title);
            GUI.color=accent;
            GUI.Label(new Rect(x+236,y+9,94,29),charging?Mathf.RoundToInt(charge*100)+"%":"ЛКМ",number);
            Rounded(new Rect(x+18,y+46,312,8),new Color(.15f,.19f,.25f),4);
            if(charging && charge>.001f)
            {
                Rounded(new Rect(x+18,y+43,312*charge,14),new Color(accent.r,accent.g,accent.b,.12f),6);
                Rounded(new Rect(x+18,y+46,312*charge,8),accent,4);
            }
            for(int i=1;i<4;i++) Rounded(new Rect(x+18+78*i,y+46,1,8),new Color(.025f,.04f,.065f,.6f),0);
            GUI.color=new Color(.78f,.84f,.91f);
            GUI.Label(new Rect(x+18,y+67,245,22),charging?(charge>=1?"Максимум · отпусти ЛКМ":"Отпусти ЛКМ, чтобы бросить"):"Зажми ЛКМ — набрать силу",small);
            Rounded(new Rect(x+286,y+66,26,24),new Color(.19f,.23f,.3f),5);
            GUI.color=Color.white; GUI.Label(new Rect(x+286,y+66,26,24),"F",centered);
        }
        GUI.color=previous;
    }
}
