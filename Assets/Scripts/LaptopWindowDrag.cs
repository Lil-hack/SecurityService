using UnityEngine;
using UnityEngine.EventSystems;

// Drag only the title bar; positions are measured in the scaled canvas space.
public sealed class LaptopWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
{
    public RectTransform target;
    Vector2 grabOffset;
    RectTransform parent;
    public void OnPointerDown(PointerEventData e)
    {
        if(e.button!=PointerEventData.InputButton.Left || target==null)return;
        target.SetAsLastSibling();
        var bar=target.parent.Find("Taskbar");if(bar!=null)bar.SetAsLastSibling();
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if(e.button!=PointerEventData.InputButton.Left || target==null)return;
        parent=target.parent as RectTransform;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,e.position,e.pressEventCamera,out var p))grabOffset=(Vector2)target.localPosition-p;
    }
    public void OnDrag(PointerEventData e)
    {
        if(e.button!=PointerEventData.InputButton.Left || target==null || parent==null)return;
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,e.position,e.pressEventCamera,out var p))return;
        p+=grabOffset;
        var bounds=parent.rect;var size=target.rect.size;
        // Keep the whole window visible where it fits, and its header visible on small displays.
        float left=bounds.xMin+size.x*target.pivot.x,right=bounds.xMax-size.x*(1-target.pivot.x);
        float bottom=bounds.yMin+64+size.y*target.pivot.y,top=bounds.yMax-size.y*(1-target.pivot.y);
        p.x=left<=right?Mathf.Clamp(p.x,left,right):(left+right)*.5f;
        p.y=bottom<=top?Mathf.Clamp(p.y,bottom,top):top;
        target.localPosition=new Vector3(p.x,p.y,target.localPosition.z);
    }
}
