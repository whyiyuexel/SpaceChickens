using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float dodgeDistance = 4f;
    public float dodgeDuration = 0.1f;
    public float dodgeCooldown = 1f;

    [Header("Animation")]
    public Animator animator;
    public string walkAnimTrigger = "Walk";
    public string idleAnimTrigger = "Idle";
    public string dodgeAnimTrigger = "Dodge";

    private Vector3 movement;
    private Vector3 lastMoveDirection = Vector3.forward;
    private bool isDodging;
    private float dodgeTimer;
    private float nextDodgeTime;
    private Vector3 dodgeDirection;
    private bool wasMoving;

    void Update()
    {
        if (isDodging)
        {
            dodgeTimer += Time.deltaTime;
            transform.position += dodgeDirection * (dodgeDistance / dodgeDuration) * Time.deltaTime;

            if (dodgeTimer >= dodgeDuration)
                isDodging = false;

            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float horizontal = 0f;
        float vertical = 0f;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vertical -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) vertical += 1f;

        movement = new Vector3(horizontal, 0f, vertical).normalized;

        if (movement.sqrMagnitude > 0)
        {
            lastMoveDirection = movement;
            if (!wasMoving && animator && !string.IsNullOrEmpty(walkAnimTrigger))
            {
                animator.SetTrigger(walkAnimTrigger);
            }
            wasMoving = true;
        }
        else
        {
            if (wasMoving && animator && !string.IsNullOrEmpty(idleAnimTrigger))
            {
                animator.SetTrigger(idleAnimTrigger);
            }
            wasMoving = false;
        }

        transform.position += movement * moveSpeed * Time.deltaTime;

        if (kb.leftShiftKey.wasPressedThisFrame && Time.time >= nextDodgeTime)
        {
            isDodging = true;
            dodgeTimer = 0f;
            dodgeDirection = lastMoveDirection;
            nextDodgeTime = Time.time + dodgeCooldown;
            
            if (animator && !string.IsNullOrEmpty(dodgeAnimTrigger))
            {
                animator.SetTrigger(dodgeAnimTrigger);
            }
        }
    }
}
