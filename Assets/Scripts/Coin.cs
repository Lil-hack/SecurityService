using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] float spinSpeed = 140f;
    [SerializeField] float bobHeight = 0.15f;
    [SerializeField] float bobSpeed = 2.2f;

    Vector3 origin;
    bool collected;

    void Start()
    {
        origin = transform.position;
    }

    void Update()
    {
        if (collected)
            return;

        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        Vector3 position = origin;
        position.y += Mathf.Sin((Time.time + origin.x) * bobSpeed) * bobHeight;
        transform.position = position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player"))
            return;

        if (!other.TryGetComponent(out ScoreCounter score))
            return;

        collected = true;
        score.AddCoin();
        Destroy(gameObject);
    }
}
