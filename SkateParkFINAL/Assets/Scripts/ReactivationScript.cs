using UnityEngine;

public class ReactivationTrigger : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToReactivate; // Assign objects with BoxColliders

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Ensure only the player triggers this
        {
            foreach (GameObject obj in objectsToReactivate)
            {
                if (obj != null)
                {
                    BoxCollider boxCollider = obj.GetComponent<BoxCollider>();
                    if (boxCollider != null && !boxCollider.enabled)
                    {
                        boxCollider.enabled = true;
                        Debug.Log(obj.name + " collider re-enabled.");
                    }
                }
            }
        }
    }
}
