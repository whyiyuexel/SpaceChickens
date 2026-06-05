using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public EnemyData data;
    public Transform firePoint;
    public GameObject meleeIndicatorPrefab;
    public Color hitColor = Color.white;
    public float hitFlashDuration = 0.15f;

    [Header("Boss Jump Attack (set jumpHeight > 0 to enable)")]
    public float jumpHeight = 0f;
    public float jumpDuration = 0.6f;

    [Header("Model Orientation")]
    [Tooltip("Rotation offset to fix model orientation. For Blender models lying on their side, try X=-90.")]
    public Vector3 modelRotationOffset = Vector3.zero;

    [Header("Animation Triggers")]
    public string meleeAnimTrigger = "MeleeAttack";
    public string rangedAnimTrigger = "RangeAttack";
    public string walkAnimTrigger = "";

    private Animator animator;
    private Transform player;
    private float nextRangedTime;
    private float nextMeleeTime;
    private int currentHealth;
    private Renderer[] enemyRenderers;
    private Color[] originalColors;
    private Coroutine flashRoutine;
    private bool isJumping = false;
    private float baseY;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        animator = GetComponent<Animator>();

        if (data != null)
            currentHealth = data.health;

        // FBX models have renderers on child objects
        enemyRenderers = GetComponentsInChildren<Renderer>();
        if (enemyRenderers.Length > 0)
        {
            originalColors = new Color[enemyRenderers.Length];
            for (int i = 0; i < enemyRenderers.Length; i++)
            {
                if (enemyRenderers[i].material.HasProperty("_Color"))
                    originalColors[i] = enemyRenderers[i].material.color;
            }
        }

        baseY = transform.position.y;
    }

    void Update()
    {
        if (player == null || data == null || isJumping) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0f;

        // Face the player
        if (directionToPlayer.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(directionToPlayer) * Quaternion.Euler(modelRotationOffset);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        float approachRange = GetApproachRange();
        bool isMoving = false;
        if (distanceToPlayer > data.meleeRange)
        {
            transform.position += directionToPlayer * data.moveSpeed * Time.deltaTime;
            isMoving = true;
        }

        bool canRanged = data.attackType == AttackType.Ranged || data.attackType == AttackType.Both;
        bool canMelee = data.attackType == AttackType.Melee || data.attackType == AttackType.Both;
        bool didAttack = false;

        // Ranged Attack
        if (canRanged && data.gun != null && distanceToPlayer <= data.attackRange && Time.time >= nextRangedTime)
        {
            Shoot(directionToPlayer);
            if (animator) 
            {
                if (!string.IsNullOrEmpty(meleeAnimTrigger)) animator.ResetTrigger(meleeAnimTrigger);
                if (!string.IsNullOrEmpty(rangedAnimTrigger)) animator.SetTrigger(rangedAnimTrigger);
            }
            nextRangedTime = Time.time + 1f / data.gun.fireRate;
            didAttack = true;
        }

        // Melee Attack
        if (!didAttack && canMelee && distanceToPlayer <= data.meleeRange && Time.time >= nextMeleeTime)
        {
            if (jumpHeight > 0f)
            {
                StartCoroutine(JumpMeleeAttack(directionToPlayer));
            }
            else
            {
                MeleeAttack(directionToPlayer);
                if (animator) 
                {
                    if (!string.IsNullOrEmpty(rangedAnimTrigger)) animator.ResetTrigger(rangedAnimTrigger);
                    if (!string.IsNullOrEmpty(walkAnimTrigger)) animator.ResetTrigger(walkAnimTrigger);
                    if (!string.IsNullOrEmpty(meleeAnimTrigger)) animator.SetTrigger(meleeAnimTrigger);
                }
                StartCoroutine(GroundMeleePause());
            }
            nextMeleeTime = Time.time + data.meleeCooldown;
            didAttack = true;
        }

        // Walk Animation
        if (isMoving && !didAttack && !string.IsNullOrEmpty(walkAnimTrigger) && animator)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            {
                animator.SetTrigger(walkAnimTrigger);
            }
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
        if (enemyRenderers != null && enemyRenderers.Length > 0)
        {
            foreach (var r in enemyRenderers)
            {
                if (r.material.HasProperty("_Color"))
                    r.material.color = hitColor;
            }
            yield return new WaitForSeconds(hitFlashDuration);
            for (int i = 0; i < enemyRenderers.Length; i++)
            {
                if (enemyRenderers[i].material.HasProperty("_Color"))
                    enemyRenderers[i].material.color = originalColors[i];
            }
        }
    }

    private IEnumerator JumpMeleeAttack(Vector3 direction)
    {
        isJumping = true;
        if (animator) 
        {
            if (!string.IsNullOrEmpty(rangedAnimTrigger)) animator.ResetTrigger(rangedAnimTrigger);
            if (!string.IsNullOrEmpty(meleeAnimTrigger)) animator.SetTrigger(meleeAnimTrigger);
        }

        Vector3 startPos = transform.position;

        // Jump up and down in a parabolic arc
        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / jumpDuration;

            // Parabola: peaks at jumpHeight at the midpoint
            float yOffset = jumpHeight * 4f * progress * (1f - progress);
            transform.position = new Vector3(startPos.x, baseY + yOffset, startPos.z);

            yield return null;
        }

        // Snap back to ground
        transform.position = new Vector3(startPos.x, baseY, startPos.z);

        // Deal damage on landing
        MeleeAttack(direction);

        isJumping = false;
    }

    private IEnumerator GroundMeleePause()
    {
        isJumping = true; // Reuse the jump lock to pause update logic
        yield return new WaitForSeconds(0.5f); // Give the Hit animation half a second to play
        isJumping = false;
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
