using UnityEngine;

public class CircleMeleeIndicator : MonoBehaviour
{
    private float windupDuration;
    private float elapsed;
    private int damage;
    private bool hasDealtDamage;
    private float targetRadius;
    private Transform enemy;
    private bool initialized;

    private GameObject fillDisc;

    public void Initialize(float windup, int damage, float radius, Transform enemy)
    {
        this.windupDuration = windup > 0f ? windup : 1f;
        this.damage = damage;
        this.targetRadius = radius > 0f ? radius : 5f;
        this.enemy = enemy;
        this.initialized = true;

        // This object (the outer disc) shows the full attack area in faint pink
        GetComponent<Renderer>().material.color = new Color(1f, 0.7f, 0.7f);
        transform.localScale = new Vector3(targetRadius, 0.15f, targetRadius);

        // Create an inner fill disc that grows from 0 to full — same as the rectangular fill bar
        fillDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fillDisc.name = "FillDisc";

        // Remove its collider
        Collider col = fillDisc.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        // Solid red fill color
        fillDisc.GetComponent<Renderer>().material.color = new Color(0.9f, 0.1f, 0.1f);

        // Start at zero size
        fillDisc.transform.localScale = Vector3.zero;

        UpdateVisual(0f);
    }

    void Update()
    {
        if (!initialized) return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / windupDuration);

        UpdateVisual(progress);

        if (progress >= 1f && !hasDealtDamage)
        {
            hasDealtDamage = true;
            DealDamage();
            Destroy(gameObject);
        }
    }

    void UpdateVisual(float progress)
    {
        if (enemy == null)
        {
            Destroy(gameObject);
            return;
        }

        // Position the outer disc on the ground under the enemy
        float groundY = GetGroundY(enemy.position);
        transform.position = new Vector3(enemy.position.x, groundY + 0.1f, enemy.position.z);

        // Grow the inner fill disc from 0% to 100% of the outer disc
        float fillScale = targetRadius * progress;
        fillDisc.transform.localScale = new Vector3(fillScale, 0.2f, fillScale);

        // Position fill slightly above the outer disc so it renders on top
        fillDisc.transform.position = new Vector3(enemy.position.x, groundY + 0.15f, enemy.position.z);
    }

    void DealDamage()
    {
        float damageRadius = targetRadius * 0.5f;
        // Check at ground level (where the indicator is), not at the boss's body height
        Vector3 damageCenter = transform.position;
        // Use OverlapBox with height so it catches players standing on the ground
        Vector3 halfExtents = new Vector3(damageRadius, 5f, damageRadius);
        Collider[] hits = Physics.OverlapBox(damageCenter, halfExtents);

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

    void OnDestroy()
    {
        if (fillDisc != null) Destroy(fillDisc);
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
