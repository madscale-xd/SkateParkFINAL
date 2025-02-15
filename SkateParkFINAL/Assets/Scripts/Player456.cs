using UnityEngine;
using System.Collections;

public class Player456 : AbstractSkateboard
{
    public Transform WholeCharacter; // Reference to the whole character (used for flipping)

    [SerializeField]
    private float flipSpeed = 720f;  // Speed of the 360 flip in degrees per second

    void LateUpdate()
    {
        // Check if 'R' is pressed to perform a special move (360 flip + jump)
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!isGrounded && canFlip)
            {
                PerformSpecialMove();
                scoreMan.ScoreTrick("Kickflip",50);
                scoreMan.ScoreMult(3);
                canFlip = false; // Prevent further flips until the player lands
            }
        }
    }

    // Perform a special move: 360 flip in the SIDE direction + jump
    protected override void PerformSpecialMove()
    {
        if (WholeCharacter != null)
        {
            StartCoroutine(Flip360AndJump());
        }
    }

    private IEnumerator Flip360AndJump()
    {
        float rotationAmount = 0f;

        // Rotate the player model in the **Z-axis** for a kickflip-style rotation
        Vector3 rotationAxis = Vector3.forward; // Forward (Z-axis) for a kickflip, or change to Vector3.up for a pop shove-it

        // Rotate the player model in the selected axis while jumping
        while (rotationAmount < 360f)
        {
            WholeCharacter.Rotate(rotationAxis * flipSpeed * Time.deltaTime);
            rotationAmount += flipSpeed * Time.deltaTime;

            yield return null;
        }

        // Ensure the final rotation is exactly 360 degrees
        WholeCharacter.rotation = Quaternion.Euler(
            WholeCharacter.rotation.eulerAngles.x,
            WholeCharacter.rotation.eulerAngles.y,
            WholeCharacter.rotation.eulerAngles.z + 360f // Change this depending on the chosen axis
        );
    }
}
