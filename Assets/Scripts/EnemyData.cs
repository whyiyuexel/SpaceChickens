using UnityEngine;

public enum AttackType { Ranged, Melee, Both }

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "Basic Chicken";
    public int health = 3;
    public float moveSpeed = 2f;
    public AttackType attackType = AttackType.Ranged;

    [Header("Score")]
    public int scoreValue = 50; // points awarded when this enemy is killed

    [Header("Ranged")]
    public float attackRange = 10f;
    public GunData gun;

    [Header("Melee")]
    public float meleeRange = 3f;
    public float meleeCooldown = 2f;
    public int meleeDamage = 1;
    public float meleeWindup = 1f;
    public float meleeIndicatorWidth = 2f;
    public float meleeIndicatorLength = 4f;
}