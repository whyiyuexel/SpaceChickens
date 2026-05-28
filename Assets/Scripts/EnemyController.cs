using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public EnemyData data;
    public Transform firePoint;
    public GameObject meleeIndicatorPrefab;
    public Color hitColor = Color.white;
    public float hitFlashDuration = 0.15f;

    private Transform player;
    private float nextRangedTime;
    private float nextMeleeTime;
    private int currentHealth;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (data != null)
            currentHealth = data.health;

        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
            originalColor = enemyRenderer.material.color;
    }

    void Update()
    {
        if (player == null || data == null) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0f;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        float approachRange = GetApproachRange();
        if (distanceToPlayer > approachRange)
        {
            transform.position += directionToPlayer * data.moveSpeed * Time.deltaTime;
        }

        bool canRanged = data.attackType == AttackType.Ranged || data.attackType == AttackType.Both;
        bool canMelee = data.attackType == AttackType.Melee || data.attackType == AttackType.Both;

        if (canRanged && data.gun != null && distanceToPlayer <= data.attackRange && Time.time >= nextRangedTime)
        {
            Shoot(directionToPlayer);
            nextRangedTime = Time.time + 1f / data.gun.fireRate;
        }

        if (canMelee && distanceToPlayer <= data.meleeRange && Time.time >= nextMeleeTime)
        {
            MeleeAttack(directionToPlayer);
            nextMeleeTime = Time.time + data.meleeCooldown;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashHit());

        if (currentHealth <= 0)
        {
            ScoreManager.Instance.AddScore(100);
            Destroy(gameObject);
        }
    }

    private IEnumerator FlashHit()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = hitColor;
            yield return new WaitForSeconds(hitFlashDuration);
            enemyRenderer.material.color = originalColor;
        }
    }

    float GetApproachRange()
    {
        switch (data.attackType)
        {
            case AttackType.Melee: return data.meleeRange;
            case AttackType.Ranged: return data.attackRange;
            case AttackType.Both: return Mathf.Min(data.meleeRange, data.attackRange);
            default: return data.attackRange;
        }
    }

    void MeleeAttack(Vector3 direction)
    {
        if (meleeIndicatorPrefab == null) return;

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(90f, angle, 0f);
        GameObject indicator = Instantiate(meleeIndicatorPrefab, transform.position, rotation);

        MeleeAttackIndicator script = indicator.GetComponent<MeleeAttackIndicator>();
        if (script != null)
        {
            script.Initialize(data.meleeWindup, data.meleeDamage, data.meleeIndicatorWidth, data.meleeIndicatorLength, transform, direction);
        }
    }

    void Shoot(Vector3 direction)
    {
        if (data.gun.bulletPrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        for (int i = 0; i < data.gun.bulletsPerShot; i++)
        {
            float spread = Random.Range(-data.gun.spreadAngle, data.gun.spreadAngle);
            Quaternion rotation = Quaternion.Euler(0f, spread, 0f);
            Vector3 dir = rotation * direction;

            GameObject bullet = Instantiate(data.gun.bulletPrefab, spawnPos, Quaternion.identity);
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(dir, data.gun.bulletSpeed, data.gun.bulletLifetime, false);
            }
        }
    }
}
