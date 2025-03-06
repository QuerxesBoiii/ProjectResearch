using UnityEngine;

public class BerryBush : MonoBehaviour
{
    public int maxFood = 10;
    private int currentFood;
    public float regenInterval = 30f; // Time in seconds to regenerate one unit of food
    private float regenTimer = 0f;

    void Start()
    {
        currentFood = maxFood;
    }

    void Update()
    {
        if (currentFood < maxFood)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= regenInterval)
            {
                currentFood++;
                regenTimer = 0f;
            }
        }
    }

    public bool TakeFood(int amount)
    {
        if (currentFood >= amount)
        {
            currentFood -= amount;
            return true;
        }
        return false;
    }

    public bool HasFood()
    {
        return currentFood > 0;
    }
}
