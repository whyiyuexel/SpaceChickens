using UnityEngine;

[CreateAssetMenu(fileName = "NewGun", menuName = "Guns/Gun Data")]
public class GunData : ScriptableObject
{
    public string gunName = "Pistol";
    public GameObject bulletPrefab;
    public float fireRate = 10f;
    public float bulletSpeed = 20f;
    public float bulletLifetime = 3f;
    public int bulletsPerShot = 1;
    public float spreadAngle = 0f;
}
