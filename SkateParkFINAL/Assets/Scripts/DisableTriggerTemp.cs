using UnityEngine;
using System.Collections;

public class DisableTriggerTemp : MonoBehaviour
{
    private BoxCollider boxCollider;

    void Start()
    {
        // Get the BoxCollider attached to this GameObject
        boxCollider = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Ensure only the player triggers this
        {
            StartCoroutine(DisableColliderTemporarily());
        }
    }

    private IEnumerator DisableColliderTemporarily()
    {
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
            yield return new WaitForSeconds(10f);
            boxCollider.enabled = true;
        }
    }
}
