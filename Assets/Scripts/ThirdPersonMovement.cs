using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public float moveSpeed = 5f; 
    public float sprintSpeed = 8f; // New sprint speed 
    public float zoomedMoveSpeed = 2.5f; 
    public float turnSpeed = 10f; 
    public float zoomTurnSpeed = 5f; 
    public float gravity = -9.81f; 
    public float groundCheckDistance = 0.4f; 
    public LayerMask groundMask;
    // For making the player not having constant switch between sprint and walk
    private bool canSprint = true; // Tracks if sprinting is allowed
    private float sprintStaminaThreshold = 0.25f; // Must reach 25% stamina to sprint again



    // Stamina variables
    public float maxStamina = 100f;
    private float currentStamina;
    public float staminaDepletionRate = 20f; // Stamina per second while sprinting
    public float staminaRegenRate = 10f; // Stamina per second when not sprinting
    private bool isSprinting = false;

    private float currentMoveSpeed;
    private float currentTurnSpeed;
    private CharacterController controller;
    private Animator animator;
    private bool weaponEquipped = false;
    private bool isZoomed = false;
    private Vector3 velocity;
    private bool isGrounded;
    private Camera mainCamera;

    //For the footsteps
    private FootstepAudio footstepAudio; // Reference to footstep audio component
    
void Start()
{
    controller = GetComponent<CharacterController>();
    animator = GetComponent<Animator>();
    currentMoveSpeed = moveSpeed;
    currentTurnSpeed = turnSpeed;
    mainCamera = Camera.main;
    currentStamina = maxStamina;
    footstepAudio = GetComponent<FootstepAudio>(); // Get footstep audio component

}

void Update()
{
    // Check if the character is grounded
    isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, groundMask);

    if (isGrounded && velocity.y < 0)
    {
        velocity.y = -2f;
    }

    // Handle movement input
    float horizontal = Input.GetAxis("Horizontal");
    float vertical = Input.GetAxis("Vertical");
    Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

    // Handle sprinting
    bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && direction.magnitude > 0.1f && !isZoomed;
    if (wantsToSprint && canSprint && currentStamina > 0)
    {
        isSprinting = true;
        currentStamina = Mathf.Max(0, currentStamina - staminaDepletionRate * Time.deltaTime);
        currentMoveSpeed = sprintSpeed;
        if (currentStamina <= 0)
        {
            canSprint = false; // Disable sprinting until threshold is reached
        }
    }
    else
    {
        isSprinting = false;
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
        currentMoveSpeed = isZoomed ? zoomedMoveSpeed : moveSpeed;
        // Re-enable sprinting when stamina reaches threshold
        if (!canSprint && currentStamina >= maxStamina * sprintStaminaThreshold)
        {
            canSprint = true;
        }
    }

    if (direction.magnitude > 0.1f)
    {
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        controller.Move(moveDir.normalized * currentMoveSpeed * Time.deltaTime);

        if (isZoomed)
        {
            float yawRotation = mainCamera.transform.eulerAngles.y;
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                                                 Quaternion.Euler(0, yawRotation, 0), 
                                                 currentTurnSpeed * Time.deltaTime);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                                                 Quaternion.Euler(0, targetAngle, 0), 
                                                 currentTurnSpeed * Time.deltaTime);
        }

        // Animation handling
        if (weaponEquipped)
        {
            if (isZoomed)
            {
                animator.SetBool("isRunningArmed", false);
                animator.SetBool("isSprinting", false);
            }
            else if (isSprinting)
            {
                animator.SetBool("isRunningArmed", false);
                animator.SetBool("isSprinting", true);
            }
            else
            {
                animator.SetBool("isRunningArmed", true);
                animator.SetBool("isSprinting", false);
            }
            animator.SetBool("isRunning", false);
        }
        else
        {
            animator.SetBool("isRunningArmed", false);
            animator.SetBool("isSprinting", isSprinting);
            animator.SetBool("isRunning", !isSprinting);
        }
    }
    else
    {
        if (isZoomed)
        {
            float yawRotation = mainCamera.transform.eulerAngles.y;
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                                                 Quaternion.Euler(0, yawRotation, 0), 
                                                 currentTurnSpeed * Time.deltaTime);
        }

        // Handle idle animations
        if (weaponEquipped)
        {
            animator.SetBool("isRunningArmed", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("isSprinting", false);
            animator.SetBool("hasWeapon", true);
        }
        else
        {
            animator.SetBool("isRunningArmed", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("isSprinting", false);
            animator.SetBool("hasWeapon", false);
        }
    }

    // Apply gravity
    velocity.y += gravity * Time.deltaTime;
    controller.Move(velocity * Time.deltaTime);

    // Notify UI of stamina changes
    StaminaUI staminaUI = GetComponent<StaminaUI>();
    if (staminaUI != null)
    {
        staminaUI.UpdateStamina(currentStamina / maxStamina);
    }

    // Update footstep audio with current movement state
    if (footstepAudio != null)
    {
        footstepAudio.UpdateMovementState(isSprinting, isZoomed, direction.magnitude > 0.1f);
    }

}

public void SetWeaponEquipped(bool equipped)
{
    weaponEquipped = equipped;
    animator.SetBool("hasWeapon", equipped);
}

public void SetZoomSpeed()
{
    isZoomed = true;
    currentMoveSpeed = zoomedMoveSpeed;
    currentTurnSpeed = zoomTurnSpeed;
    isSprinting = false;
    animator.SetBool("isSprinting", false);
}

public void ResetSpeed()
{
    isZoomed = false;
    currentMoveSpeed = moveSpeed;
    currentTurnSpeed = turnSpeed;
    animator.SetBool("isSprinting", false);
}

public float GetStaminaPercentage()
{
    return currentStamina / maxStamina;
}


// Called by animation events
public void PlayFootstepSound()
{
    if (footstepAudio != null)
    {
        footstepAudio.PlayFootstep();
    }
}


}