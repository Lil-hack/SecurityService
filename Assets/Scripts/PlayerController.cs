using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 7f;
    [SerializeField] float turnSpeed = 14f;

    Rigidbody body;
    Vector2 input;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    void Update()
    {
        input = ReadMove();
    }

    void FixedUpdate()
    {
        Vector3 velocity = body.linearVelocity;
        velocity.x = input.x * moveSpeed;
        velocity.z = input.y * moveSpeed;
        body.linearVelocity = velocity;

        Vector3 look = new Vector3(input.x, 0f, input.y);
        if (look.sqrMagnitude < 0.001f)
            return;

        Quaternion target = Quaternion.LookRotation(look, Vector3.up);
        float t = 1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime);
        body.MoveRotation(Quaternion.Slerp(body.rotation, target, t));
    }

    static Vector2 ReadMove()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector2.zero;

        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            y += 1f;

        Vector2 move = new Vector2(x, y);
        return move.sqrMagnitude > 1f ? move.normalized : move;
    }
}
