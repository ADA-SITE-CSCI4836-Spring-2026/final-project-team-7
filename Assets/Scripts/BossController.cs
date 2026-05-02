using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BossController : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Animator anim;

    [Header("Movement Settings")]
    public float speed = 5f;
    public float rotationSpeed = 15f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    // NEW — public toggle so GameManager can freeze the player
    public bool CanMove { get; set; } = true;

    private Vector3 movement;
    private Vector3 velocity;
    private readonly int speedHash = Animator.StringToHash("Speed");

    void Update()
    {
        // Gravity & Grounding (always runs so player doesn't float when frozen)
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // NEW — if frozen, skip input but still drop with gravity
        if (!CanMove)
        {
            movement = Vector3.zero;
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            if (anim != null) anim.SetFloat(speedHash, 0f);
            return;
        }

        // Input
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        movement.Set(x, 0f, z);
        movement.Normalize();

        // Movement & Rotation
        if (movement.sqrMagnitude >= 0.01f)
        {
            float targetAngle = Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            controller.Move(movement * speed * Time.deltaTime);
        }

        // Jumping
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Animator
        if (anim != null)
        {
            anim.SetFloat(speedHash, movement.magnitude);
        }
    }
}