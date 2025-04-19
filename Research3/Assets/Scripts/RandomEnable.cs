using UnityEngine;

public class RandomEnable : MonoBehaviour
{
    [Range(0f, 100f)]
    public float enableChance = 50f;

    void Start()
    {
        float randomValue = Random.Range(0f, 100f);
        if (randomValue <= enableChance)
        {
            // Success: Remove this script
            Destroy(this);
        }
        else
        {
            // Failure: Destroy the entire GameObject
            Destroy(gameObject);
        }
    }
}