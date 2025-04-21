using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// DO NOT REMOVE OR EDIT THIS COMMENT: Code is using Unity 6 (6000.0.37f1)
// DO NOT REMOVE OR EDIT THIS COMMENT: Make sure the code is easy to understand, and isn't inefficient. Make the code slightly more efficient.
// DO NOT REMOVE OR EDIT THIS COMMENT: This script manages food sources in the simulation, including replenishment and animated scaling for apples, berries, meat, etc.
// DO NOT REMOVE OR EDIT THIS COMMENT: The script supports both replenishable (e.g., trees) and non-replenishable (e.g., dead creatures) food sources.

public class FoodSource : MonoBehaviour
{
    [SerializeField] public int maxFood = 10; // Maximum amount of food, set by CreatureBehavior
    [SerializeField] public int currentFood = 10; // Current amount of food, set by CreatureBehavior
    [SerializeField] private float replenishInterval = 15f;
    private float replenishTimer = 0f;
    [SerializeField] private bool hasFood = true;
    [SerializeField] private float foodSatiety = 1f;
    [SerializeField] public bool isNotReplenishable = false;
    [SerializeField] private int reservedFood = 0; // Tracks reserved food units
    [SerializeField] private List<(CreatureBehavior creature, float timeout)> reservations = new List<(CreatureBehavior, float)>(); // Tracks reservations with timeouts

    [SerializeField] private List<GameObject> foodObjects = new List<GameObject>();
    private List<Coroutine> activeCoroutines;

    [SerializeField] private float shrinkDuration = 1f;
    [SerializeField] private float growDuration = 15f;
    [SerializeField] private float bubbleDuration = 0.5f;

    // Called when the script instance is being loaded
    void Start()
    {
        if (!isNotReplenishable) // Skip foodObjects for non-replenishable sources (e.g., dead creatures)
        {
            if (foodObjects.Count == 0)
            {
                PopulateFromChildren();
            }

            if (foodObjects.Count != maxFood && foodObjects.Count > 0 && !GetComponent<CreatureBehavior>())
            {
                Debug.LogWarning($"FoodSource '{name}' has {foodObjects.Count} food objects but maxFood is {maxFood}. Adjusting maxFood.");
                maxFood = foodObjects.Count;
            }

            currentFood = Mathf.Clamp(currentFood, 0, maxFood);
            activeCoroutines = new List<Coroutine>(new Coroutine[maxFood]);

            for (int i = 0; i < foodObjects.Count; i++)
            {
                if (foodObjects[i] != null)
                {
                    foodObjects[i].transform.localScale = (i < currentFood) ? Vector3.one : Vector3.zero;
                    foodObjects[i].SetActive(true);
                }
            }
        }
        else
        {
            currentFood = Mathf.Clamp(currentFood, 0, maxFood); // Still clamp but skip foodObjects
        }

        UpdateFoodState();
    }

    // Updates the food source state each frame
    void Update()
    {
        if (!isNotReplenishable)
        {
            replenishTimer += Time.deltaTime;
            if (replenishTimer >= replenishInterval && currentFood < maxFood)
            {
                currentFood++;
                UpdateFoodState();
                StartGrowAnimation(currentFood - 1);
                replenishTimer = 0f;
            }
        }
    }

    // Populates foodObjects list from child GameObjects, excluding the creature’s main body and text display
    private void PopulateFromChildren()
    {
        foodObjects.Clear();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        MeshRenderer mainRenderer = renderers.Length > 0 ? renderers[0] : null;
        foreach (Transform child in transform)
        {
            if (mainRenderer != null && child.GetComponent<MeshRenderer>() == mainRenderer && GetComponent<CreatureBehavior>())
            {
                continue; // Skip the creature’s main body
            }
            if (child.GetComponent<TextMeshPro>())
            {
                continue; // Skip text display
            }
            foodObjects.Add(child.gameObject);
            Debug.Log($"{name}: Added child {child.name} to foodObjects list");
        }
    }

    // Updates the hasFood flag based on currentFood level
    private void UpdateFoodState()
    {
        if (currentFood == 0)
        {
            hasFood = false;
        }
        else if (currentFood >= maxFood * 0.5f)
        {
            hasFood = true;
        }
    }

    // Triggers shrink animation for a food object at the specified index
    private void StartShrinkAnimation(int index)
    {
        if (!isNotReplenishable && index >= 0 && index < foodObjects.Count && foodObjects[index] != null)
        {
            if (activeCoroutines[index] != null)
                StopCoroutine(activeCoroutines[index]);
            activeCoroutines[index] = StartCoroutine(ShrinkFood(foodObjects[index]));
        }
    }

    // Triggers grow animation for a food object at the specified index
    private void StartGrowAnimation(int index)
    {
        if (!isNotReplenishable && index >= 0 && index < foodObjects.Count && foodObjects[index] != null)
        {
            if (activeCoroutines[index] != null)
                StopCoroutine(activeCoroutines[index]);
            foodObjects[index].SetActive(true);
            activeCoroutines[index] = StartCoroutine(GrowFood(foodObjects[index]));
        }
    }

    // Coroutine to animate food shrinking from current scale to 0
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
        food.SetActive(false);
    }

    // Coroutine to animate food growing from 0 to 0.75, then bubbling to 1.5 and back to 1
    private IEnumerator GrowFood(GameObject food)
    {
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

        elapsed = 0f;
        Vector3 peakScale = Vector3.one * 1.5f;
        Vector3 endScale = Vector3.one;
        while (elapsed < bubbleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bubbleDuration;
            if (t < 0.5f)
                food.transform.localScale = Vector3.Lerp(midScale, peakScale, t * 2f);
            else
                food.transform.localScale = Vector3.Lerp(peakScale, endScale, (t - 0.5f) * 2f);
            yield return null;
        }
        food.transform.localScale = endScale;
    }

    // Property to get/set currentFood, handling animations and destruction
    public int CurrentFood
    {
        get { return currentFood; }
        set
        {
            int oldValue = currentFood;
            currentFood = Mathf.Clamp(value, 0, maxFood);
            UpdateFoodState();

            // Ensure reservedFood doesn't exceed currentFood
            reservedFood = Mathf.Min(reservedFood, currentFood);
            if (reservedFood < reservations.Count)
            {
                // Clear excess reservations if currentFood decreased
                reservations.RemoveRange(reservedFood, reservations.Count - reservedFood);
            }

            if (!isNotReplenishable) // Only animate for replenishable sources
            {
                if (currentFood < oldValue)
                {
                    int foodLost = oldValue - currentFood;
                    for (int i = 0; i < foodLost; i++)
                    {
                        int index = currentFood + i;
                        StartShrinkAnimation(index);
                    }
                }
                else if (currentFood > oldValue)
                {
                    int foodGained = currentFood - oldValue;
                    for (int i = 0; i < foodGained; i++)
                    {
                        int index = oldValue + i;
                        StartGrowAnimation(index);
                    }
                }
            }

            if (isNotReplenishable && currentFood <= 0)
            {
                // Clear all reservations before destroying
                reservations.Clear();
                reservedFood = 0;
                Destroy(gameObject);
            }
        }
    }

    // Add method to release a reservation
    public void ReleaseReservation(CreatureBehavior creature)
    {
        var reservation = reservations.Find(r => r.creature == creature);
        if (reservation.creature != null)
        {
            reservations.Remove(reservation);
            reservedFood = Mathf.Max(0, reservedFood - 1); // Decrease reserved food (1 unit per creature)
            Debug.Log($"{creature.name}: Released reservation on {name}. Reserved food: {reservedFood}/{CurrentFood}");
        }
    }

    // Add method to update reservations and handle timeouts
    public void UpdateReservations()
    {
        for (int i = reservations.Count - 1; i >= 0; i--)
        {
            var reservation = reservations[i];
            if (Time.time > reservation.timeout)
            {
                reservations.RemoveAt(i);
                reservedFood = Mathf.Max(0, reservedFood - 1);
                Debug.Log($"{reservation.creature.name}: Reservation on {name} timed out. Reserved food: {reservedFood}/{CurrentFood}");
            }
        }
    }

    // Add method to reserve food
    public void ReserveFood(CreatureBehavior creature, float timeout)
    {
        reservedFood++;
        reservations.Add((creature, Time.time + timeout));
        Debug.Log($"{creature.name}: Reserved food at {name} (Available: {CurrentFood - ReservedFood})");
    }

    // Add property to access reserved food
    public int ReservedFood => reservedFood;

    public bool HasFood => hasFood;
    public float FoodSatiety => foodSatiety;
}