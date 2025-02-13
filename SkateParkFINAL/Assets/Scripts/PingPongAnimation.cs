using UnityEngine;

public class PingPongAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private string[] animations = { "Crouch", "Idle" };
    private int currentIndex = 0;
    private bool isReversing = false;
    private float animationTime = 0f;

    [SerializeField] private float switchSpeed = 1f; // Speed of switching the animation time
    private bool isPlayingPingPong = false;

    private void Update()
    {
        if (isPlayingPingPong)
        {
            // Switch between "Crouch" and "Idle" animations
            animationTime += (isReversing ? -1 : 1) * switchSpeed * Time.deltaTime;

            // Handle switching between the start and end of the animation
            if (animationTime >= animator.GetCurrentAnimatorStateInfo(0).length)
            {
                animationTime = animator.GetCurrentAnimatorStateInfo(0).length;
                isReversing = true; // Reversed direction once we hit the end
            }
            else if (animationTime <= 0f)
            {
                animationTime = 0f;
                isReversing = false; // Reversed direction once we hit the start
            }

            // Play the current animation at the correct time
            animator.Play(animations[currentIndex], 0, animationTime);
        }
    }

    public void StartPingPongAnimation()
    {
        isPlayingPingPong = true; // Start ping-pong between "Crouch" and "Idle"
        currentIndex = 0; // Start with "Crouch"
        isReversing = false; // Start the animation normally
        animationTime = 0f; // Reset the animation time
    }

    public void StopPingPongAnimation()
    {
        isPlayingPingPong = false; // Stop ping-ponging
    }

    public void PlayOtherAnimation(string animationName)
    {
        // Allow other animations to be played
        animator.Play(animationName);
        isPlayingPingPong = false; // Stop ping-pong animation if another animation is playing
    }
}
