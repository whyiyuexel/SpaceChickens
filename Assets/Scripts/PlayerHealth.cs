using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public Color hitColor = Color.white;
    public float hitFlashDuration = 0.15f;

    private int currentHealth;
    private Renderer playerRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;

    void Start()
    {
        currentHealth = maxHealth;
        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer != null)
            originalColor = playerRenderer.material.color;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashHit());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashHit()
    {
        if (playerRenderer != null)
        {
            playerRenderer.material.color = hitColor;
            yield return new WaitForSeconds(hitFlashDuration);
            playerRenderer.material.color = originalColor;
        }
    }

    void Die()
    {
        // TODO: game over screen, respawn, etc.
        gameObject.SetActive(false);
    }

    public int GetCurrentHealth() => currentHealth;
}
