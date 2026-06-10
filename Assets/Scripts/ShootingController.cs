using UnityEngine;
using UnityEngine.InputSystem;

public class ShootingController : MonoBehaviour
{
    public GunData currentGun;
    public Transform firePoint;

    [Header("Animation")]
    public Animator animator;
    public string shootAnimTrigger = "Shoot";

    [Header("Model Orientation")]
    public Vector3 modelRotationOffset = Vector3.zero;

    private float nextFireTime;
    private Camera mainCam;

    [Header("Upgrade System")]
    public GunTier[] gunTiers; // Array of 3 (Pistol, Machine Gun, Minigun)
    private int currentGunLevel = 0; // 0=Pistol, 1=MachineGun, 2=Minigun
    private int currentBulletLevel = 0; // 0=Basic, 1=Upgraded, 2=Max

    void Start()
    {
        mainCam = Camera.main;
        
        // Ensure starting gun is properly equipped if the user populated the tiers
        UpdateEquippedGun();
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || currentGun == null) return;

        float upgradedFireRate = currentGun.fireRate; // Old multiplier removed

        Vector3 aimDirection = GetAimDirection();
        if (aimDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(aimDirection) * Quaternion.Euler(modelRotationOffset);
        }

        if (mouse.leftButton.isPressed && Time.time >= nextFireTime)
        {
            Shoot(aimDirection);
            if (animator && !string.IsNullOrEmpty(shootAnimTrigger))
            {
                animator.SetTrigger(shootAnimTrigger);
            }
            nextFireTime = Time.time + 1f / upgradedFireRate;
        }
    }

    void Shoot(Vector3 aimDirection)
    {
        SoundManager.Instance?.Play(SoundManager.Instance.playerShoot);
        
        int totalBullets = currentGun.bulletsPerShot; // Old bonus removed

        for (int i = 0; i < totalBullets; i++)
        {
            float spread = Random.Range(-currentGun.spreadAngle, currentGun.spreadAngle);
            Quaternion rotation = Quaternion.Euler(0f, spread, 0f);
            Vector3 direction = rotation * aimDirection;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
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

    public void UpgradeGun()
{
    if (gunTiers == null || gunTiers.Length == 0) return;
    
    currentGunLevel = Mathf.Min(currentGunLevel + 1, gunTiers.Length - 1);
    UpdateEquippedGun();

    // Tell GunManager to swap the visual model
    GunManager gunManager = GetComponentInChildren<GunManager>();
    if (gunManager != null)
        gunManager.EquipGun(currentGunLevel);
    else
        Debug.LogWarning("No GunManager found in children!");

    Debug.Log($"Gun Upgraded! Gun Level: {currentGunLevel}, Bullet Level: {currentBulletLevel}, Now Using: {currentGun.name}");
}

    public void UpgradeBullet()
    {
        if (gunTiers == null || gunTiers.Length == 0) return;

        // Increase bullet type level (stay on same gun type)
        currentBulletLevel = Mathf.Min(currentBulletLevel + 1, 2);
        UpdateEquippedGun();
        Debug.Log($"Bullet Upgraded! Gun Level: {currentGunLevel}, Bullet Level: {currentBulletLevel}, Now Using: {currentGun.name}");
    }

    private void UpdateEquippedGun()
    {
        if (gunTiers == null || gunTiers.Length == 0) return;
        
        GunTier activeTier = gunTiers[Mathf.Clamp(currentGunLevel, 0, gunTiers.Length - 1)];
        
        switch(currentBulletLevel)
        {
            case 0: EquipGun(activeTier.basicBullet); break;
            case 1: EquipGun(activeTier.upgradedBullet); break;
            case 2: EquipGun(activeTier.maxBullet); break;
        }
    }
}

[System.Serializable]
public class GunTier
{
    public GunData basicBullet;
    public GunData upgradedBullet;
    public GunData maxBullet;
}