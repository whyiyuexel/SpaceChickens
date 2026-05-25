using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Vector2 inputVector;
    private Vector3 movement;

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float horizontal = 0f;
        float vertical = 0f;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vertical -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) vertical += 1f;

        movement = new Vector3(horizontal, 0f, vertical).normalized;

        transform.position += movement * moveSpeed * Time.deltaTime;
    }
}
