using UnityEngine;

public class MeleeAttackIndicator : MonoBehaviour
{
    private float windupDuration;
    private float elapsed;
    private int damage;
    private bool hasDealtDamage;
    private float targetLength;
    private float width;
    private Transform enemy;
    private Vector3 attackDirection;

    private bool initialized;

    public void Initialize(float windup, int damage, float width, float length, Transform enemy, Vector3 direction)
    {
        this.windupDuration = windup > 0f ? windup : 1f;
        this.damage = damage;
        this.width = width;
        this.targetLength = length;
        this.enemy = enemy;
        this.attackDirection = direction;
        this.initialized = true;

        UpdateTransform(0f);
    }

    void Start()
    {
        if (!initialized)
            Destroy(gameObject);
    }

    void Update()
    {
        if (!initialized) return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / windupDuration);

        UpdateTransform(progress);

        if (progress >= 1f && !hasDealtDamage)
        {
            hasDealtDamage = true;
            DealDamage();
            Destroy(gameObject);
        }
    }

    private GameObject fillQuad;

    void UpdateTransform(float progress)
    {
        if (enemy == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = new Vector3(width, targetLength, 1f);

        Vector3 pos = enemy.position + attackDirection * (targetLength * 0.5f);
        pos.y = GetGroundY(enemy.position) + 0.01f;
        transform.position = pos;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = new Color(1f, 0f, 0f, 0.2f);

        if (fillQuad == null)
        {
            fillQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(fillQuad.GetComponent<Collider>());
            fillQuad.transform.SetParent(transform, false);
            fillQuad.transform.localPosition = new Vector3(0f, -0.5f, -0.01f);
            fillQuad.transform.localRotation = Quaternion.identity;
            fillQuad.transform.localScale = new Vector3(1f, 0f, 1f);
            Renderer fillRend = fillQuad.GetComponent<Renderer>();
            if (fillRend != null)
                fillRend.material.color = new Color(1f, 0f, 0f, 0.8f);
        }

        fillQuad.transform.localScale = new Vector3(1f, progress, 1f);
        fillQuad.transform.localPosition = new Vector3(0f, -0.5f + (progress * 0.5f), -0.01f);
    }

    void DealDamage()
    {
        Vector3 center = enemy.position + attackDirection * (targetLength * 0.5f);
        Vector3 halfExtents = new Vector3(width * 0.5f, 1f, targetLength * 0.5f);
        Quaternion rotation = Quaternion.Euler(0f, Mathf.Atan2(attackDirection.x, attackDirection.z) * Mathf.Rad2Deg, 0f);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                    playerHealth.TakeDamage(damage);
            }
        }
    }

    private float GetGroundY(Vector3 origin)
    {
        // Temporarily disable enemy colliders so the ray doesn't hit the enemy itself
        Collider[] enemyCols = null;
        if (enemy != null)
        {
            enemyCols = enemy.GetComponentsInChildren<Collider>();
            foreach (var c in enemyCols) if (c != null) c.enabled = false;
        }

        RaycastHit hit;
        float result = origin.y;
        if (Physics.Raycast(origin + Vector3.up * 10f, Vector3.down, out hit, 50f))
            result = hit.point.y;

        // Re-enable
        if (enemyCols != null)
            foreach (var c in enemyCols) if (c != null) c.enabled = true;

        return result;
    }
}
