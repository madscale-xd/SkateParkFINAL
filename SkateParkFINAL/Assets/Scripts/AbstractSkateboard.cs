﻿using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public abstract class AbstractSkateboard : MonoBehaviour
{
    // Player stats
    public string playerName;
    public float health;
    public float speed;
    public float specialMoveGauge;
    
    private DeathAndRespawn spawner;
    private RedGreen rg;
    private Coroutine resetSpeedCoroutine;

    // Core movement and jump variables
    [SerializeField] protected float moveSpeed = 100f;
    [SerializeField] protected float sprintSpeed = 20f;
    [SerializeField] protected float rotationSpeed = 4f;
    [SerializeField] protected Transform cameraTransform;
    [SerializeField] protected float jumpForce = 5f;
    [SerializeField] protected Transform skateboardD;  // Reference to the player's skateboard model
    [SerializeField] protected Transform CharacterModel; // Reference to the character's body model

    // Try for inertial
    [SerializeField] private float deceleration = 2f;
    private float currentSpeed = 0f;

    // Speed boost
    [SerializeField] private float boostDuration = 2f; // A short burst of speed
    [SerializeField] private float boostIncrement = 5f; // Speed increment for each kick
    [SerializeField] private float boostDecayRate = 2f; // Rate at which the boost decays over time
    private float currentMovementSpeed = 0f; // Track the current movement speed
    [SerializeField] private float boostDelay = 0.2f; // Delay before boosting to sync with animation



    private bool isBoosting = false;

    [SerializeField] private float boostCooldown = 1f; // Cooldown period between boosts
    private bool isOnCooldown = false; // Track if the boost is on cooldown

    [SerializeField] private Animator animator;

    //Jump
    private bool isCrouching = false;

    [SerializeField] private float jumpDelay = 0.2f;


    [SerializeField] private float crouchDelay = 0.2f;
    [SerializeField] private float crouchDuration = 0.2f;
    [SerializeField] private float crouchCooldown = 0f;
    [SerializeField] private float jumpCooldown = 0;

    private bool isBoostOnCooldown = false;
    private bool isCrouchOnCooldown = false;
    private bool isJumpOnCooldown = false;

    // Animator parameters
    [SerializeField] private float fallDuration = 0.2f;
    [SerializeField] private float landDuration = 0.2f;
    [SerializeField] private float impactVelocityThreshold = 10f; // Adjust as needed

    //Break
    [SerializeField] private float brakeDamping = 0.5f; // Adjust the damping value as needed






    // Movement-related fields
    protected Rigidbody rb;
    protected Quaternion targetRotation;
    protected bool isGrounded;
    protected bool groundTouch;

    protected bool hasJumped = false;

    protected bool isGrinding = false; // Track if player is grinding

    protected bool canFlip = true; // Prevent multiple flips without landing

    protected Quaternion originalRotation;

    // For saving/loading player state
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        targetRotation = transform.rotation;

        spawner = GameObject.Find("Skatepark").GetComponent<DeathAndRespawn>();
        rg = GameObject.Find("RedGreen").GetComponent<RedGreen>();

        // Store the original rotation
        originalRotation = skateboardD.rotation;

        rb = GetComponent<Rigidbody>();
        targetRotation = transform.rotation;
        LoadPlayerData();
    }

    public void SavePlayerData()
    {
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetFloat("Health", health);
        PlayerPrefs.SetFloat("SpecialMoveGauge", specialMoveGauge);
        PlayerPrefs.Save();
        Debug.Log("Player data saved!");
    }

    public void LoadPlayerData()
    {
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            playerName = PlayerPrefs.GetString("PlayerName");
            health = PlayerPrefs.GetFloat("Health");
            specialMoveGauge = PlayerPrefs.GetFloat("SpecialMoveGauge");
            Debug.Log("Player data loaded!");
        }
    }

    private void ResetAnimation(string animationName)
    {
        if (animator != null)
        {
            // Ensure that the state name and layer index are correct
            if (animator.HasState(0, Animator.StringToHash(animationName)))
            {
                animator.Play(animationName, 0, 0f); // Reset the animation to the start
            }
            else
            {
                Debug.LogWarning($"Animator does not have a state named '{animationName}' in layer 0.");
            }
        }
    }






    // Core movement handling (WASD, jump)
    protected virtual void HandleMovement()
    {
        Vector3 direction = GetMovementDirection();

        if (direction != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(direction);
        }

        // Handle boosting
        if (Input.GetMouseButtonDown(1) && !isBoosting && !isBoostOnCooldown) // Right-click to boost
        {
            StartCoroutine(Boost());
        }

        // Apply the current movement speed and decay over time
        if (isBoosting || currentMovementSpeed > 0f)
        {
            currentSpeed = currentMovementSpeed;
            currentMovementSpeed = Mathf.MoveTowards(currentMovementSpeed, 0f, boostDecayRate * Time.deltaTime);
        }
        else
        {
            // Decelerate when not boosting
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        MoveCharacter(currentSpeed);
        RotateCharacter();
    }







    private IEnumerator Boost()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Boost");
            animator.Play("Boost", 0, 0f);
        }

        yield return new WaitForSeconds(boostDelay); // Wait for the delay before boosting

        isBoosting = true;
        isBoostOnCooldown = true;
        currentMovementSpeed += boostIncrement; // Add the boost increment to the current movement speed

        yield return new WaitForSeconds(boostDuration);

        isBoosting = false;

        if (animator != null)
        {
            // Allow the animation to complete and reset without re-triggering it
            animator.ResetTrigger("Boost");
        }

        yield return new WaitForSeconds(boostCooldown); // Wait for the cooldown period
        isBoostOnCooldown = false; // Cooldown finished
    }













    private IEnumerator CrouchCoroutine()
    {
        if (animator != null)
        {
            animator.ResetTrigger("CrouchStart");
            animator.Play("CrouchStart", 0, 0f);
            // No need to set the trigger again here as Play already initiates the animation
        }

        yield return new WaitForSeconds(crouchDelay);

        isCrouching = true;
        isCrouchOnCooldown = true;

        yield return new WaitForSeconds(crouchDuration);
        isCrouching = false;

        yield return new WaitForEndOfFrame();

        if (animator != null)
        {
            animator.ResetTrigger("CrouchStart");
        }

        yield return new WaitForSeconds(crouchCooldown);
        isCrouchOnCooldown = false;
    }





    protected virtual void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.C) && isGrounded && !isCrouching && !isCrouchOnCooldown)
        {
            StartCoroutine(CrouchCoroutine()); // Start crouching coroutine
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouching && !isJumpOnCooldown)
        {
            StartCoroutine(JumpCoroutine()); // Start jump coroutine
        }
    }







    private IEnumerator JumpCoroutine()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Jump");
            animator.Play("Jump", 0, 0f);
            // No need to set the trigger again here as Play already initiates the animation
        }

        yield return new WaitForSeconds(jumpDelay);

        Jump(); // Ensure the actual jumping logic is called here
        isJumpOnCooldown = true;

        yield return new WaitForEndOfFrame(); // Ensure the animation fully plays before resetting

        if (animator != null)
        {
            animator.ResetTrigger("Jump");
        }

        yield return new WaitForSeconds(jumpCooldown);
        isJumpOnCooldown = false;
    }
















    private Vector3 GetMovementDirection()
    {
        Vector3 direction = Vector3.zero;

        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = cameraTransform.right;
        right.y = 0f;
        right.Normalize();

        if (Input.GetKey(KeyCode.W)) direction += forward;
        if (Input.GetKey(KeyCode.S)) direction -= forward;
        if (Input.GetKey(KeyCode.A)) direction -= right;
        if (Input.GetKey(KeyCode.D)) direction += right;

        return direction.normalized; // Ensure normalized direction
    }


    private void RotateCharacter()
    {
        if (targetRotation != transform.rotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }


    private void MoveCharacter(float speed)
    {
        Vector3 movement = transform.forward * speed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void Jump()
    {
        hasJumped = true;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    // Abstract method for collision exit logic (to be overridden in concrete class)
    

    // Override OnCollisionEnter to add specific collision behavior for PlayerMovement2
    protected void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Carousel") || collision.gameObject.CompareTag("RedGreen"))
        {
            isGrounded = true; // Reset the grounded flag when touching the ground

            Debug.Log("grounded");
            Debug.Log("can flip");

            if (collision.gameObject.CompareTag("Ground"))
            {
                groundTouch = true;
                if (rg != null)
                {
                    rg.StopRedGreen();
                }

                // Reset the flip state once grounded
                canFlip = true; // Allow the player to flip again after landing
                hasJumped = false; // Reset the jump state, ensuring another jump is required to flip
            }
        }else if (collision.relativeVelocity.magnitude >= impactVelocityThreshold)
        {
            Debug.Log("Collision detected with sufficient velocity!");
            EnableRagdoll();
            StartCoroutine(WaitForRagdollToSettle());
        }
        else if (collision.gameObject.CompareTag("Death"))
        {
            Destroy(gameObject);
            spawner.RespawnPlayer();
        }
        else if (collision.gameObject.CompareTag("FinishLine"))
        {
            SceneManager.LoadScene("EndMenu");
        }
    }

    private void EnableRagdoll()
    {
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        animator.enabled = false; // Disable animator to let the ragdoll take over
    }

    private void DisableRagdoll()
    {
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        animator.enabled = true; // Re-enable animator
    }

    private IEnumerator WaitForRagdollToSettle()
    {
        bool isSettled = false;

        while (!isSettled)
        {
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds

            float totalVelocity = 0f;
            foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            {
                totalVelocity += rb.velocity.magnitude;
            }

            if (totalVelocity < 0.1f) // Adjust the threshold for "settled" as needed
            {
                isSettled = true;
            }
        }

        DisableRagdoll();
        PlayRisingAnimation();
    }

    private void PlayRisingAnimation()
    {
        if (animator != null)
        {
            animator.Play("Rising", 0, 0f);
            Debug.Log("Rising animation playing!");
        }
    }



    protected void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Carousel"))
        {
            moveSpeed = 2.5f;
            sprintSpeed = 4f;

            if (resetSpeedCoroutine != null)
            {
                StopCoroutine(resetSpeedCoroutine);
                resetSpeedCoroutine = null;
            }
        }
        else if (collision.gameObject.CompareTag("RedGreen"))
        {
            moveSpeed = 1.5f;
            sprintSpeed = 3f;

            if (resetSpeedCoroutine != null)
            {
                StopCoroutine(resetSpeedCoroutine);
                resetSpeedCoroutine = null;
            }
        }
    }

    protected void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Carousel"))
        {
            resetSpeedCoroutine = StartCoroutine(ResetSpeedAfterDelay());
        }
        else if (collision.gameObject.CompareTag("RedGreen"))
        {
            resetSpeedCoroutine = StartCoroutine(ResetSpeedAfterDelay());
        }
    }

    private IEnumerator ResetSpeedAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        moveSpeed = 3.5f;
        sprintSpeed = 5.5f;

        resetSpeedCoroutine = null;
    }

    // Abstract methods for tricks and special moves
    
    protected void PerformGrind()
    {
        if (skateboardD != null)
        {
            if (isGrinding)  // While grinding, rotate relative to the target object's forward direction
            {
                // Get the target object's forward direction
                Vector3 targetForward = CharacterModel.forward;

                // Normalize to make sure the direction is uniform
                targetForward.y = 0; // Prevent vertical tilt (we only want horizontal rotation)
                targetForward.Normalize();

                // Rotate the skateboard model 90 degrees relative to the target object's forward direction
                skateboardD.rotation = Quaternion.LookRotation(targetForward) * Quaternion.Euler(0, 90, 0);
            }
            else  // When LeftControl is released, rotate back by 90 degrees from its current direction
            {
                // Get the current forward direction of the skateboard model
                Vector3 currentForward = skateboardD.forward;

                // Rotate back by 90 degrees relative to the current facing direction
                skateboardD.rotation = Quaternion.LookRotation(currentForward) * Quaternion.Euler(0, -90, 0);
            }
        }
    }

    protected abstract void PerformSpecialMove();

    protected virtual void Update()
    {
        HandleMovement();
        HandleJump();

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isGrinding = true;
            PerformGrind();
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isGrinding = false;
            PerformGrind(); // Reset to original rotation when LeftControl is released
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            ApplyBrake(); // Apply brake when Left Control is held down
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            SavePlayerData();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            LoadPlayerData();
        }

        // Ensure the isGrounded flag is being updated correctly
        UpdateGroundedStatus();

        // Detect falling
        if (!isFalling && rb.velocity.y < 0 && !isGrounded)
        {
            isFalling = true;
            Debug.Log("Falling detected!");
            StartCoroutine(FallCoroutine()); // Start falling coroutine
        }

        // Detect landing
        if (isGrounded && rb.velocity.y == 0 && isFalling)
        {
            isFalling = false;
            Debug.Log("Landing detected!");
            StartCoroutine(LandCoroutine()); // Start landing coroutine
        }
    }

    private void ApplyBrake()
    {
        if (currentMovementSpeed > 0f)
        {
            currentMovementSpeed = Mathf.MoveTowards(currentMovementSpeed, 0f, brakeDamping * Time.deltaTime);
            Debug.Log("Applying brake: " + currentMovementSpeed);
        }
    }



    private void UpdateGroundedStatus()
    {
        // Update the isGrounded flag based on collision detection or raycast
        // Example implementation:
        RaycastHit hit;
        float distanceToGround = 1.0f; // Adjust based on your character's height
        if (Physics.Raycast(transform.position, Vector3.down, out hit, distanceToGround))
        {
            isGrounded = hit.collider != null;
        }
        else
        {
            isGrounded = false;
        }
    }

    private bool isFalling = false;

    private IEnumerator FallCoroutine()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Fall");
            animator.Play("Fall", 0, 0f);
            Debug.Log("Fall animation!");
        }

        yield return new WaitForSeconds(fallDuration); // Adjust as needed

        if (animator != null)
        {
            animator.ResetTrigger("Fall");
        }
    }

    private IEnumerator LandCoroutine()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Land");
            animator.Play("Land", 0, 0f);
            Debug.Log("Land animation!");
        }

        yield return new WaitForSeconds(landDuration); // Adjust as needed

        if (animator != null)
        {
            animator.ResetTrigger("Land");
        }
    }


}
