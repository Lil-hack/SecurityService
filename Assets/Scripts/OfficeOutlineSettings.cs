using UnityEngine;

[CreateAssetMenu(menuName="Office/Cartoon outline settings")]
public sealed class OfficeOutlineSettings : ScriptableObject
{
    public Color color=new Color(.045f,.062f,.085f,1);
    [Range(.25f,4)] public float characterPixels=1.7f;
    [Range(.2f,3)] public float propPixels=1.05f;
    [Range(.15f,1)] public float faceAndHands=.55f;
    [Range(5,40)] public float maxDistance=20;
}
