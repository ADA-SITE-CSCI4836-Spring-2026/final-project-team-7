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

    // Cache variables to prevent garbage collection
    private Vector3 movement;
    private Vector3 velocity;
    private readonly int speedHash = Animator.StringToHash("Speed"); // More efficient than string lookups

    void Update()
    {
        // 1. Handle Gravity & Grounding
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Keep grounded
        }

        // 2. Get Input
        float x = Input.GetAxisRaw("Horizontal"); // Raw is snappier for Jams
        float z = Input.GetAxisRaw("Vertical");
        movement.Set(x, 0f, z);
        movement.Normalize();

        // 3. Movement & Rotation
        if (movement.sqrMagnitude >= 0.01f)
        {
            // Smooth Rotation
            float targetAngle = Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // Move relative to world
            controller.Move(movement * speed * Time.deltaTime);
        }

        // 4. Handle Jumping
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 5. Apply Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 6. Optimized Animation Update
        if (anim != null)
        {
            anim.SetFloat(speedHash, movement.magnitude);
        }
    }
}