using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private bool firedByPlayer;

    public int damage = 1;

    public void Initialize(Vector3 direction, float speed, float lifetime, bool firedByPlayer)
    {
        this.direction = direction;
        this.speed = speed;
        this.firedByPlayer = firedByPlayer;
        
        // Orient the bullet to face its travel direction
        if (direction != Vector3.zero)
        {
            // By default, Quaternion.LookRotation aligns the Z-axis with the direction.
            // If you used a Unity Cylinder or Capsule, its length is along the Y-axis.
            // We multiply by Euler(90, 0, 0) to pitch it forward so the Y-axis points in the travel direction.
            transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
        }

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (firedByPlayer)
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
        else
        {
            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}