using UnityEngine;
using System.Collections.Generic;

public class BerryBushFinder : MonoBehaviour
{
    // List of berry bushes in range, visible in the Inspector
    public List<Transform> berryBushesInRange = new List<Transform>();

    // Detection range
    [SerializeField] private float detectionRadius = 5f;

    // Layer mask to filter only berry bushes (optional, set in Inspector)
    [SerializeField] private LayerMask foodLayer;

    void Update()
    {
        // Check for berry bushes within the detection radius
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, foodLayer);
        List<Transform> currentBushes = new List<Transform>();

        // Process all detected colliders
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("BerryBush"))
            {
                Transform bushTransform = hit.transform;
                currentBushes.Add(bushTransform);

                // Add new bushes to the list
                if (!berryBushesInRange.Contains(bushTransform))
                {
                    berryBushesInRange.Add(bushTransform);
                    Debug.Log("BerryBush entered range: " + bushTransform.name);
                }
            }
        }

        // Remove bushes that are no longer in range
        for (int i = berryBushesInRange.Count - 1; i >= 0; i--)
        {
            Transform bush = berryBushesInRange[i];
            if (!currentBushes.Contains(bush))
            {
                berryBushesInRange.Remove(bush);
                Debug.Log("BerryBush exited range: " + bush.name);
            }
        }
    }

    void OnDrawGizmos()
    {
        // Visualize the detection sphere in the editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}