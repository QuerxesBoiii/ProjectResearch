using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;
using sapra.InfiniteLands;

public class TerrainEnabler : NetworkBehaviour
{
    [SerializeField] private float enableDelay = 2.0f; // Delay before enabling the terrain

    private void Start()
    {   
        // Start the coroutine that enables the terrain after a delay.
        StartCoroutine(EnableTerrainAfterDelay());
    }

    private IEnumerator EnableTerrainAfterDelay()
    {
        // Wait for the specified delay to ensure everything is initialized.
        yield return new WaitForSeconds(enableDelay);

        // Search for the terrain object by name, even if it is inactive.
        GameObject terrainObject = null;
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "Terrain")
            {
                terrainObject = obj;
                break;
            }
        }

        if (terrainObject != null)
        {
            // Enable the terrain.
            terrainObject.SetActive(true);
            Debug.Log("Terrain enabled on owner client.");
        }
        else
        {
            Debug.LogWarning("Terrain object not found! Ensure an object named 'Terrain' exists in the scene.");
        }
    }
}
