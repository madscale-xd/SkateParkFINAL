
using UnityEngine;

public class InstantiatePlayer : MonoBehaviour
{
    [SerializeField] private GameObject[] playerPrefabs = new GameObject[2]; // Supports two player prefabs
    [SerializeField] private Transform spawnPoint; // Transform used as the spawn position

    public GameObject SpawnPlayer(int index, string customName = "")
    {
        if (index < 0 || index >= playerPrefabs.Length)
        {
            Debug.LogError("Invalid player index: " + index);
            return null;
        }

        if (playerPrefabs[index] == null)
        {
            Debug.LogError("Player prefab at index " + index + " is not assigned!");
            return null;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point is not assigned!");
            return null;
        }

        GameObject playerInstance = Instantiate(playerPrefabs[index], spawnPoint.position, spawnPoint.rotation);

        // Rename the spawned object
        if (!string.IsNullOrEmpty(customName))
        {
            playerInstance.name = customName;
        }

        return playerInstance;
    }
}
