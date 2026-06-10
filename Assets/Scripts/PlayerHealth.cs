using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public Color hitColor = Color.white;
    public float hitFlashDuration = 0.15f;

    private int currentHealth;
    private Renderer[] playerRenderers;
    private Color[] originalColors;
    private Coroutine flashRoutine;

    private int[] healthTiers = { 10, 25, 50 };
    private int currentHealthLevel = 0;

    void Start()
    {
        // Initialize max health to the first tier
        maxHealth = healthTiers[currentHealthLevel];
        currentHealth = maxHealth;
        
        playerRenderers = GetComponentsInChildren<Renderer>();
        if (playerRenderers.Length > 0)
        {
            originalColors = new Color[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i].material.HasProperty("_Color"))
                    originalColors[i] = playerRenderers[i].material.color;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        SoundManager.Instance?.Play(SoundManager.Instance.playerHit);

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashHit());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void UpgradeHealth()
    {
        currentHealthLevel = Mathf.Min(currentHealthLevel + 1, healthTiers.Length - 1);
        maxHealth = healthTiers[currentHealthLevel];
        
        // Fully heal on upgrade
        currentHealth = maxHealth;
        
        Debug.Log($"Health Upgraded! Level: {currentHealthLevel}, Max Health: {maxHealth}");
    }

    private IEnumerator FlashHit()
    {
        if (playerRenderers != null && playerRenderers.Length > 0)
        {
            foreach (var r in playerRenderers)
            {
                if (r.material.HasProperty("_Color"))
                    r.material.color = hitColor;
            }
            yield return new WaitForSeconds(hitFlashDuration);
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i].material.HasProperty("_Color"))
                    playerRenderers[i].material.color = originalColors[i];
            }
        }
    }

    void Die()
    {
        SoundManager.Instance?.Play(SoundManager.Instance.playerDie);
        // TODO: game over screen, respawn, etc.
        gameObject.SetActive(false);
    }

    public int GetCurrentHealth() => currentHealth;
}
