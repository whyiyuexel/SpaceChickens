using UnityEngine;

public enum UpgradeType { Gun, Bullet, Health }

public class UpgradePickup : MonoBehaviour
{
    public UpgradeType upgradeType = UpgradeType.Gun;

    public float rotateSpeed = 90f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.25f;

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
        if (!other.CompareTag("Player")) return;

        ShootingController shooting = other.GetComponentInParent<ShootingController>();
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

        bool collected = false;

        if (upgradeType == UpgradeType.Gun || upgradeType == UpgradeType.Bullet)
        {
            if (shooting != null)
            {
                if (upgradeType == UpgradeType.Gun) shooting.UpgradeGun();
                else if (upgradeType == UpgradeType.Bullet) shooting.UpgradeBullet();
                collected = true;
            }
        }
        else if (upgradeType == UpgradeType.Health)
        {
            if (health != null)
            {
                health.UpgradeHealth();
                collected = true;
            }
        }

        if (collected)
        {
            Destroy(gameObject);
        }
    }
}