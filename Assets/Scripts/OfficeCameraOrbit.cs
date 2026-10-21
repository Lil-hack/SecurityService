using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Right drag: orbit. Wheel: zoom. Home: reset the office overview.</summary>
[RequireComponent(typeof(Camera))]
public class OfficeCameraOrbit : MonoBehaviour
{
    readonly Vector3 target = new Vector3(0, .3f, 0);
    Vector3 startPosition;
    Quaternion startRotation;
    Camera view;
    float startSize;

    void Awake()
    {
        view = GetComponent<Camera>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        startSize = view.orthographicSize;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                transform.RotateAround(target, Vector3.up, delta.x * .2f);
                float elevation = Mathf.Asin((transform.position - target).normalized.y) * Mathf.Rad2Deg;
                float change = Mathf.Clamp(elevation - delta.y * .15f, 15f, 80f) - elevation;
                transform.RotateAround(target, transform.right, change);
            }
            view.orthographicSize = Mathf.Clamp(view.orthographicSize - mouse.scroll.ReadValue().y * .008f, 4, 22);
        }
        if (Keyboard.current != null && Keyboard.current.homeKey.wasPressedThisFrame)
        {
            transform.SetPositionAndRotation(startPosition, startRotation);
            view.orthographicSize = startSize;
        }
    }
}
