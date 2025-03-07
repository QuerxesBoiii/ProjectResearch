using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// DO NOT REMOVE OR EDIT THIS COMMENT: Code is using Unity 6 (6000.0.37f1)
// DO NOT REMOVE OR EDIT THIS COMMENT: Make sure the code is easy to understand, and isn't inefficient. Make the code slightly more efficient.
public class FoodSource : MonoBehaviour
{
    [SerializeField] private int maxFood = 10;
    [SerializeField] public int currentFood = 10;
    [SerializeField] private float replenishInterval = 15f; // Adjusted to 15s per your request
    private float replenishTimer = 0f;
    [SerializeField] private bool hasFood = true;
    [SerializeField] private float foodSatiety = 1f; // How much hunger each apple restores

    [SerializeField] private List<GameObject> foodObjects = new List<GameObject>();
    private List<Coroutine> activeCoroutines; // Tracks ongoing animations for each food object

    [SerializeField] private float shrinkDuration = 1f; // Duration to shrink from 1 to 0
    [SerializeField] private float growDuration = 15f; // Duration to grow from 0 to 0.75 (matches replenishInterval)
    [SerializeField] private float bubbleDuration = 0.5f; // Duration for the 0.75 -> 1.5 -> 1.0 bubble effect

    void Start()
    {
        if (foodObjects.Count == 0)
        {
            PopulateFromChildren();
        }

        if (foodObjects.Count != maxFood)
        {
            Debug.LogWarning($"FoodSource '{name}' has {foodObjects.Count} food objects but maxFood is {maxFood}. Adjusting maxFood.");
            maxFood = foodObjects.Count;
        }

        currentFood = Mathf.Clamp(currentFood, 0, maxFood);
        activeCoroutines = new List<Coroutine>(new Coroutine[maxFood]); // Initialize with null coroutines
        UpdateFoodState();

        // Set initial state: all food objects start at scale 1
        for (int i = 0; i < foodObjects.Count; i++)
        {
            if (i < currentFood)
                foodObjects[i].transform.localScale = Vector3.one;
            else
                foodObjects[i].transform.localScale = Vector3.zero;
            foodObjects[i].SetActive(true); // Start with all enabled
        }
    }

    void Update()
    {
        replenishTimer += Time.deltaTime;
        if (replenishTimer >= replenishInterval && currentFood < maxFood)
        {
            currentFood++;
            UpdateFoodState();
            StartGrowAnimation(currentFood - 1); // Start growing the newly added food
            replenishTimer = 0f;
        }
    }

    private void PopulateFromChildren()
    {
        foodObjects.Clear();
        foreach (Transform child in transform)
        {
            foodObjects.Add(child.gameObject);
            Debug.Log($"{name}: Added child {child.name} to foodObjects list");
        }
    }

    private void UpdateFoodState()
    {
        if (currentFood == 0)
        {
            hasFood = false;
        }
        else if (currentFood >= maxFood * 0.3f)
        {
            hasFood = true;
        }
    }

    // Starts the shrink animation for the food object at the given index
    private void StartShrinkAnimation(int index)
    {
        if (index >= 0 && index < foodObjects.Count && foodObjects[index] != null)
        {
            if (activeCoroutines[index] != null)
                StopCoroutine(activeCoroutines[index]);
            activeCoroutines[index] = StartCoroutine(ShrinkFood(foodObjects[index]));
        }
    }

    // Starts the grow animation for the food object at the given index
    private void StartGrowAnimation(int index)
    {
        if (index >= 0 && index < foodObjects.Count && foodObjects[index] != null)
        {
            if (activeCoroutines[index] != null)
                StopCoroutine(activeCoroutines[index]);
            foodObjects[index].SetActive(true); // Enable before growing
            activeCoroutines[index] = StartCoroutine(GrowFood(foodObjects[index]));
        }
    }

    // Coroutine to shrink food from scale 1 to 0
    private IEnumerator ShrinkFood(GameObject food)
    {
        float elapsed = 0f;
        Vector3 startScale = food.transform.localScale;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            food.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        food.transform.localScale = Vector3.zero;
        food.SetActive(false); // Disable after reaching 0
    }

    // Coroutine to grow food from 0 to 0.75, then bubble to 1.5 and back to 1
    private IEnumerator GrowFood(GameObject food)
    {
        // Phase 1: Grow from 0 to 0.75 over growDuration (15s)
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 midScale = Vector3.one * 0.75f;
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / growDuration;
            food.transform.localScale = Vector3.Lerp(startScale, midScale, t);
            yield return null;
        }
        food.transform.localScale = midScale;

        // Phase 2: Bubble effect from 0.75 to 1.5 and back to 1 over bubbleDuration
        elapsed = 0f;
        Vector3 peakScale = Vector3.one * 1.5f;
        Vector3 endScale = Vector3.one;
        while (elapsed < bubbleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bubbleDuration;
            if (t < 0.5f) // First half: 0.75 to 1.5
                food.transform.localScale = Vector3.Lerp(midScale, peakScale, t * 2f);
            else // Second half: 1.5 to 1
                food.transform.localScale = Vector3.Lerp(peakScale, endScale, (t - 0.5f) * 2f);
            yield return null;
        }
        food.transform.localScale = endScale;
    }

    public int CurrentFood
    {
        get { return currentFood; }
        set
        {
            int oldValue = currentFood;
            currentFood = Mathf.Clamp(value, 0, maxFood);
            UpdateFoodState();

            // If food was eaten, start shrink animation for the lost food
            if (currentFood < oldValue)
            {
                int foodLost = oldValue - currentFood;
                for (int i = 0; i < foodLost; i++)
                {
                    int index = currentFood + i; // Index of the food that was just eaten
                    StartShrinkAnimation(index);
                }
            }
            // If food was added (e.g., externally), start grow animation
            else if (currentFood > oldValue)
            {
                int foodGained = currentFood - oldValue;
                for (int i = 0; i < foodGained; i++)
                {
                    int index = oldValue + i; // Index of the newly added food
                    StartGrowAnimation(index);
                }
            }
        }
    }

    public bool HasFood => hasFood;
    public float FoodSatiety => foodSatiety; // Public getter for satiety
}