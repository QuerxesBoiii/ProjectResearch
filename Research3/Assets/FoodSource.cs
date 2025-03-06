using UnityEngine;

public class BerryBush : MonoBehaviour
{
    public int maxFood = 5;
    public int currentFood = 5;
    public float replenishInterval = 30f; // Replenish every 30 seconds
    private float replenishTimer = 0f;

    void Update()
    {
        replenishTimer += Time.deltaTime;
        if (replenishTimer >= replenishInterval && currentFood < maxFood)
        {
            currentFood++;
            replenishTimer = 0f;
        }
    }
}