using UnityEngine;

public class UpgradePickup : MonoBehaviour
{
    public float rotateSpeed = 90f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.25f;

    public float fireRateBoost = 0.5f;
    public int bulletBoost = 1;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);

        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        ShootingController shooting = other.GetComponent<ShootingController>();

        if (shooting != null)
        {
            shooting.ApplyUpgrade(fireRateBoost, bulletBoost);
            Destroy(gameObject);
        }
    }
}