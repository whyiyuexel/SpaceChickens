using UnityEngine;
using UnityEngine.InputSystem;

public class ShootingController : MonoBehaviour
{
    public GunData currentGun;
    public Transform firePoint;

    private float nextFireTime;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || currentGun == null) return;

        if (mouse.leftButton.isPressed && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / currentGun.fireRate;
        }
    }

    void Shoot()
    {
        Vector3 aimDirection = GetAimDirection();

        for (int i = 0; i < currentGun.bulletsPerShot; i++)
        {
            float spread = Random.Range(-currentGun.spreadAngle, currentGun.spreadAngle);
            Quaternion rotation = Quaternion.Euler(0f, spread, 0f);
            Vector3 direction = rotation * aimDirection;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject bullet = Instantiate(currentGun.bulletPrefab, spawnPos, Quaternion.identity);

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(direction, currentGun.bulletSpeed, currentGun.bulletLifetime);
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
}
