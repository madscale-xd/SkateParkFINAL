using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] audioClips;  // Array of audio clips
    [SerializeField] private float[] audioVolumes;    // Array of volumes for each clip (0.0 to 1.0)

    private Dictionary<int, AudioSource> loopingSources = new Dictionary<int, AudioSource>(); // Stores looping SFX
    private Dictionary<int, GameObject> activeSFXObjects = new Dictionary<int, GameObject>(); // Track active SFX objects

    // Play a sound effect by index with a unique AudioSource (one-shot)
    public void PlaySFX(int index)
    {
        if (index >= 0 && index < audioClips.Length)
        {
            if (index == 2) // Special rule for index 2: Prevent overlapping
            {
                if (activeSFXObjects.ContainsKey(index)) // If an instance is still playing, ignore the new request
                {
                    Debug.Log("SFX(2) is already playing, ignoring new request.");
                    return;
                }

                GameObject sfxObject = new GameObject("SFX_" + index);
                AudioSource audioSource = sfxObject.AddComponent<AudioSource>();

                audioSource.clip = audioClips[index];
                audioSource.volume = (index < audioVolumes.Length) ? audioVolumes[index] : 1.0f;
                audioSource.Play();

                activeSFXObjects[index] = sfxObject; // Store reference

                StartCoroutine(RemoveSFXReference(index, audioClips[index].length));
            }
            else
            {
                // Normal behavior for other SFX
                PlaySFXInstant(index);
            }
        }
        else
        {
            Debug.LogWarning("SFX index out of range!");
        }
    }

    // Instant play (used for all except index 2)
    private void PlaySFXInstant(int index)
    {
        GameObject sfxObject = new GameObject("SFX_" + index);
        AudioSource audioSource = sfxObject.AddComponent<AudioSource>();

        audioSource.clip = audioClips[index];
        audioSource.volume = (index < audioVolumes.Length) ? audioVolumes[index] : 1.0f;
        audioSource.Play();

        Destroy(sfxObject, audioClips[index].length);
    }

    // Coroutine to remove reference when the SFX finishes playing
    private IEnumerator RemoveSFXReference(int index, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (activeSFXObjects.ContainsKey(index))
        {
            Destroy(activeSFXObjects[index]); // Destroy GameObject
            activeSFXObjects.Remove(index); // Remove reference
        }
    }

    // Start playing a looping SFX
    public void LoopSFX(int index)
    {
        if (index >= 0 && index < audioClips.Length)
        {
            if (!loopingSources.ContainsKey(index)) // Prevent duplicate loops
            {
                GameObject sfxObject = new GameObject("LoopSFX_" + index);
                AudioSource audioSource = sfxObject.AddComponent<AudioSource>();

                audioSource.clip = audioClips[index];
                audioSource.volume = (index < audioVolumes.Length) ? audioVolumes[index] : 1.0f;
                audioSource.loop = true;  // Enable looping
                audioSource.Play();

                loopingSources[index] = audioSource; // Store reference
            }
            else
            {
            }
        }
        else
        {
            Debug.LogWarning("SFX index out of range!");
        }
    }

    // Stop a looping SFX
    public void StopSFX(int index)
    {
        if (loopingSources.ContainsKey(index))
        {
            AudioSource audioSource = loopingSources[index];
            audioSource.Stop();
            Destroy(audioSource.gameObject); // Cleanup GameObject

            loopingSources.Remove(index); // Remove from dictionary
        }
        else
        {
            Debug.LogWarning("No looping SFX found at index " + index);
        }
    }
}
