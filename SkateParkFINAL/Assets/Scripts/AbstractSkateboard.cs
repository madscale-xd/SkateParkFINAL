using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public abstract class AbstractSkateboard : MonoBehaviour
{
    public float knockbackForce = 1000f;
    private bool playerControlsEnabled = true;
    private float previousYRotation;
    private float accumulatedRotation = 0f;
    private bool hasStartedTrackingRotation = false;
    protected ScoreManager scoreMan;
    private bool jumpPressed = false;
    private float upwardForce = 50f;
    private Vector3 grindDirection;
    private float grindSpeed;
    private int groundContacts = 0;
    private int grindContacts = 0;
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
    [SerializeField] private float jumpCooldown = 2f;

    private bool isBoostOnCooldown = false;
    private bool isCrouchOnCooldown = false;
    private bool isJumpOnCooldown = false;

    // Animator parameters
    [SerializeField] private float fallDuration = 0.2f;
    [SerializeField] private float landDuration = 0.2f;
    [SerializeField] private float impactVelocityThreshold = 100000f; // Adjust as needed

    //Break
    [SerializeField] private float brakeDamping = 0.5f; // Adjust the damping value as needed

    // Movement-related fields
    protected Rigidbody rb;
    protected Quaternion targetRotation;
    protected bool isGrounded;
    protected bool groundTouch;

    protected bool hasJumped = false;

    protected bool isGrinding = false; // Track if player is grinding
    private float grindTimer = 0f;
    private float grindInterval = 0.5f; // Calls ScoreGrind() every 0.5s

    private float airTimer = 0f;

    private float airInterval = 0.25f; // Calls ScoreAir() every 0.25s

    protected bool canFlip = true; // Prevent multiple flips without landing

    protected Quaternion originalRotation;

    // For saving/loading player state
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        targetRotation = transform.rotation;
        originalRotation = transform.rotation;

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
        if (Input.GetMouseButtonDown(1) && !isBoosting && !isBoostOnCooldown && isGrounded) // Right-click to boost
        {
            Debug.Log("Current Speed: " + currentSpeed);
            StartCoroutine(Boost());
        }

        // Only apply movement decay when NOT grinding
        if (!isGrinding)
        {
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

        if (Input.GetKeyDown(KeyCode.Space) && (isGrounded || isGrinding) && !jumpPressed && !isCrouching && !isJumpOnCooldown)
        {
            jumpPressed = true; // Prevent extra jumps
            StartCoroutine(JumpCoroutine());
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
        // Move forward
        Vector3 movement = transform.forward * speed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);

        // Adjust rotation only when grounded
        if (isGrounded)
        {
            AdjustRotationToGround();
        }
    }

    private void AdjustRotationToGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
        {
            // Get ground normal
            Vector3 groundNormal = hit.normal;

            // Calculate new rotation
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;

            // Smoothly rotate towards the target rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 30f);
        }
    }

    private void Jump()
    {
        hasJumped = true;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    

    // Override OnCollisionEnter to add specific collision behavior for PlayerMovement2
    protected void OnCollisionEnter(Collision collision)
    {
       if (collision.gameObject.CompareTag("Ragdoller") && currentSpeed >= 7)
        {
            Debug.Log("Ragdoller bangga");
            Debug.Log($"Collision detected! currentSpeed: {currentSpeed}");

            // Disable movement input
            currentMovementSpeed = 0;
            currentSpeed = 0;

            // Reset velocity to prevent unwanted momentum
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Ensure Rigidbody isn't kinematic
            rb.isKinematic = false;
            
            // Calculate knockback direction
            Vector3 knockbackDirection = (transform.position - collision.GetContact(0).point).normalized;
            knockbackDirection.y += 0.5f; // Add slight upward force

            // Apply knockback force
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
            rb.WakeUp(); // Ensure physics updates immediately

            // Disable controls for 2 seconds
            StartCoroutine(DisableControlsForSeconds(2f));
        }


        if (collision.gameObject.CompareTag("Grinder"))
        {
            grindContacts++; // Increase contact count

            if (grindContacts == 1) // Start grinding only on the first contact
            {
                StartGrinding();
            }
        }
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Carousel") || collision.gameObject.CompareTag("RedGreen"))
        {
            if(isGrinding == true){
                StopGrinding();
            }
            isGrounded = true; // Reset the grounded flag when touching the ground

            Debug.Log("grounded");
            Debug.Log("can flip");
            StartCoroutine(LandCoroutine());

            if (collision.gameObject.CompareTag("Ground"))
            {
                jumpPressed = false;
                groundContacts++;
                groundTouch = true;
                if (rg != null)
                {
                    rg.StopRedGreen();
                }

                // Reset the flip state once grounded
                canFlip = true; // Allow the player to flip again after landing
                hasJumped = false; // Reset the jump state, ensuring another jump is required to flip
            }
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
       if (collision.gameObject.CompareTag("Grinder"))
        {
            rb.useGravity = true;
            grindContacts--; // Decrease contact count

            if (grindContacts <= 0 && !isGrinding) // Stop grinding only when all contacts are lost
            {
                StartCoroutine(DelayedGrindExit());
            }
        }
        if (collision.gameObject.CompareTag("Ground"))
        {
            canFlip = true;
            groundContacts--; // Decrease ground contact count

            if (groundContacts <= 0) // Only trigger falling if no ground objects remain
            {
                StartCoroutine(FallCoroutine());
            }
        }
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

    protected abstract void PerformSpecialMove();

    protected virtual void Update()
    {
        DetectAirRotation();
        if(playerControlsEnabled){
            HandleMovement();
            HandleJump();
        }else{

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

    void StartGrinding()
    {
        if (isGrinding) return; // Prevent duplicate calls
        grindTimer = 0f;
        scoreMan.ScoreTrick("Grind",0);
        scoreMan.ScoreMult(2);
        isGrinding = true;
        rb.useGravity = false;
        jumpPressed = false;

        Vector3 targetForward = CharacterModel.forward;

                // Normalize to make sure the direction is uniform
                targetForward.y = 0; // Prevent vertical tilt (we only want horizontal rotation)
                targetForward.Normalize();

                // Rotate the skateboard model 90 degrees relative to the target object's forward direction
                skateboardD.rotation = Quaternion.LookRotation(targetForward) * Quaternion.Euler(0, 90, 0);

        // Capture the forward direction
        grindDirection = transform.forward;
        grindDirection.y = 0; // Flatten the movement
        grindDirection.Normalize();

        // Capture initial momentum
        grindSpeed = rb.velocity.magnitude;
        if (grindSpeed < 1f) grindSpeed = 5f; // Ensure a minimum speed

        Debug.Log($"Started Grinding! Direction: {grindDirection}, Speed: {grindSpeed}");
    }

    void StopGrinding()
    {
        if (!isGrinding) return;
        grindTimer = 0f;
         // Get the current forward direction of the skateboard model
        Vector3 currentForward = skateboardD.forward;

        // Rotate back by 90 degrees relative to the current facing direction
        skateboardD.rotation = Quaternion.LookRotation(currentForward) * Quaternion.Euler(0, -90, 0);
        isGrinding = false;
        rb.useGravity = true;

        // Reset position and rotation to original values
        rb.velocity = Vector3.zero; // Stop movement

        Debug.Log("Stopped Grinding! Resetting position.");
    }

    void FixedUpdate()
    {
        scoreMan = GameObject.Find("CanvasUI").GetComponent<ScoreManager>();
        if (isGrinding)
        {
            grindTimer += Time.deltaTime;

            if (grindTimer >= grindInterval)
            {
                grindTimer = 0f; // Reset timer
                scoreMan.ScoreGrind(); // Call the grind scoring method
                Debug.Log("Grind Scored");
            }
        }
        
        if(!isGrounded){
            airTimer += Time.deltaTime;

            if (airTimer >= airInterval)
            {
                airTimer = 0f; // Reset timer
                scoreMan.ScoreAir(); // Call the grind scoring method
                Debug.Log("Air Scored");
            }
        }

        if (isGrinding) return; // Skip if grinding

        RaycastHit hit;
        float distance = rb.velocity.magnitude * Time.fixedDeltaTime; // Predict next frame movement

        // SphereCast to predict collision in a wider range
        if (Physics.SphereCast(rb.position, 0.5f, rb.velocity.normalized, out hit, distance))
        {
            Debug.Log("Collision predicted with: " + hit.collider.name);
            
            // Stop movement at hit point
            rb.position = hit.point - rb.velocity.normalized * 0.1f; // Slight offset to prevent sticking
            rb.velocity = Vector3.zero; // Prevent further movement through objects
        }
    }


    IEnumerator DelayedGrindExit()
    {
        yield return new WaitForSeconds(0.2f); // Small delay to prevent instant exit

        if (grindContacts <= 0) // Double-check if still off the grinder
        {
            StopGrinding();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Upwards"))
        {
            ApplyUpwardForce();
        }
    }

    private void ApplyUpwardForce()
    {
        scoreMan.ScoreTrick("Halfpipe",50);
        scoreMan.ScoreMult(3);
        rb.velocity = new Vector3(rb.velocity.x, 2f, rb.velocity.z); // Reset vertical velocity to ensure consistent jump
        rb.AddForce(Vector3.up * upwardForce, ForceMode.Impulse); // Apply instant upward force
    }

    private void DetectAirRotation()
    {
        if (!isGrounded)
        {
            float currentYRotation = transform.eulerAngles.y;
            float rotationDelta = Mathf.DeltaAngle(previousYRotation, currentYRotation); // Handles wrap-around

            accumulatedRotation += Mathf.Abs(rotationDelta);
            previousYRotation = currentYRotation;

            Debug.Log($"Rotation Delta: {rotationDelta}, Accumulated Rotation: {accumulatedRotation}");

            if (accumulatedRotation >= 250f)
            {
                Debug.Log("✅ 360 Rotation Detected!");
                scoreMan.ScoreTrick("AirSpin",100);
                scoreMan.ScoreMult(4);
                accumulatedRotation = 0f;
            }
        }
        else
        {
            accumulatedRotation = 0f; // Reset on landing
        }
    }

    private IEnumerator DisableControlsForSeconds(float duration)
    {
        Debug.Log("Disabling movement");
        playerControlsEnabled = false;
        yield return new WaitForSeconds(duration);
        Debug.Log("Enabling movement");
        playerControlsEnabled = true;
    }
}