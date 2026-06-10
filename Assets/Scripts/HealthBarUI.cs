using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public Image healthBarFill;

    [Header("Player Reference")]
    public PlayerHealth playerHealth;

    [Header("Colors")]
    public Color fullHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;

    void Update()
    {
        if (playerHealth == null) return;

        int current = playerHealth.GetCurrentHealth();
        int max = playerHealth.maxHealth;

        float fillAmount = (float)current / max;
        healthBarFill.fillAmount = fillAmount;

        if (fillAmount > 0.6f)
            healthBarFill.color = fullHealthColor;
        else if (fillAmount > 0.3f)
            healthBarFill.color = midHealthColor;
        else
            healthBarFill.color = lowHealthColor;
    }
}