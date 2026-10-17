using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 11f, -10f);
    [SerializeField] float followSharpness = 6f;

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = target.position + offset;
        float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
        transform.LookAt(target.position + Vector3.up);
    }
}
