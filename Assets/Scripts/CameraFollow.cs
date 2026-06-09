using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float height = 30f;
    public float distance = 30f;
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null)
            return;

        // Fixed isometric position - always behind and above in world space
        Vector3 desiredPosition = new Vector3(
            target.position.x,
            target.position.y + height,
            target.position.z - distance
        );

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(60f, 0f, 0f);
    }
}
