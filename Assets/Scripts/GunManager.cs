using UnityEngine;

public class GunManager : MonoBehaviour
{
    [Header("Gun Prefabs")]
    public GameObject[] gunPrefabs;

    private GameObject currentGunModel;
    private int currentGunIndex = 0;

    void Start()
    {
        EquipGun(0);
    }

    public void EquipGun(int index)
    {
        if (gunPrefabs == null || gunPrefabs.Length == 0) return;
        if (index < 0 || index >= gunPrefabs.Length) return;

        if (currentGunModel != null)
            Destroy(currentGunModel);

        currentGunIndex = index;
        currentGunModel = Instantiate(
            gunPrefabs[index],
            transform.position,
            transform.rotation,
            transform
        );
        currentGunModel.transform.localPosition = Vector3.zero;
        currentGunModel.transform.localRotation = Quaternion.identity;

        Transform firePoint = currentGunModel.transform.Find("FirePoint");
        if (firePoint != null)
        {
            ShootingController shooting = GetComponentInParent<ShootingController>();
            if (shooting != null)
            {
                shooting.firePoint = firePoint;
                Debug.Log($"FirePoint assigned for {gunPrefabs[index].name}");
            }
        }
        else
        {
            Debug.LogWarning($"No FirePoint on {gunPrefabs[index].name}!");
        }
    }

    public void EquipNextGun()
    {
        int next = Mathf.Min(currentGunIndex + 1, gunPrefabs.Length - 1);
        EquipGun(next);
    }
}