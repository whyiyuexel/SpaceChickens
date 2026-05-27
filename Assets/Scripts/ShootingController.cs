using UnityEngine;
using UnityEngine.InputSystem;

public class ShootingController : MonoBehaviour
{
    public GunData currentGun;
    public Transform firePoint;

    private float nextFireTime;
    private Camera mainCam;

    private float fireRateMultiplier = 1f;
    private int bonusBullets = 0;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || currentGun == null) return;

        float upgradedFireRate = currentGun.fireRate * fireRateMultiplier;

        if (mouse.leftButton.isPressed && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / upgradedFireRate;
        }
    }

    void Shoot()
    {
        Vector3 aimDirection = GetAimDirection();

        int totalBullets = currentGun.bulletsPerShot + bonusBullets;

        for (int i = 0; i < totalBullets; i++)
        {
            float spread = Random.Range(-currentGun.spreadAngle, currentGun.spreadAngle);
            Quaternion rotation = Quaternion.Euler(0f, spread, 0f);
            Vector3 direction = rotation * aimDirection;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject bullet = Instantiate(currentGun.bulletPrefab, spawnPos, Quaternion.identity);

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(direction, currentGun.bulletSpeed, currentGun.bulletLifetime, true);
            }
        }
    }

    Vector3 GetAimDirection()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            Vector3 direction = (worldPoint - transform.position).normalized;
            direction.y = 0f;
            return direction;
        }

        return transform.forward;
    }

    public void EquipGun(GunData newGun)
    {
        currentGun = newGun;
    }

    public void ApplyUpgrade(float fireRateBoost, int bulletBoost)
    {
        fireRateMultiplier += fireRateBoost;
        bonusBullets += bulletBoost;

        Debug.Log("Upgrade applied! Fire rate multiplier: " + fireRateMultiplier + ", bonus bullets: " + bonusBullets);
    }
}