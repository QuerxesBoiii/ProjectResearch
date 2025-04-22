using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq; // Added for ToList()

public class FoodSource : MonoBehaviour
{
    [SerializeField] public int maxFood = 10;
    [SerializeField] public int currentFood = 10;
    [SerializeField] private float replenishInterval = 15f;
    private float replenishTimer = 0f;
    [SerializeField] private bool hasFood = true;
    [SerializeField] private float foodSatiety = 1f;
    [SerializeField] public bool isNotReplenishable = false;

    [SerializeField] private List<GameObject> foodObjects = new List<GameObject>();
    private List<Coroutine> activeCoroutines;

    [SerializeField] private float shrinkDuration = 1f;
    [SerializeField] private float growDuration = 15f;
    [SerializeField] private float bubbleDuration = 0.5f;

    // Reservation system
    private Dictionary<CreatureBehavior, float> reservations = new Dictionary<CreatureBehavior, float>();
    private Dictionary<CreatureBehavior, float> reservationTimers = new Dictionary<CreatureBehavior, float>();
    public int ReservedFood => reservations.Count;

    void Start()
    {
        if (!isNotReplenishable)
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
            currentFood = Mathf.Clamp(currentFood, 0, maxFood);
        }

        UpdateFoodState();
    }

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

    private void PopulateFromChildren()
    {
        foodObjects.Clear();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        MeshRenderer mainRenderer = renderers.Length > 0 ? renderers[0] : null;
        foreach (Transform child in transform)
        {
            if (mainRenderer != null && child.GetComponent<MeshRenderer>() == mainRenderer && GetComponent<CreatureBehavior>())
            {
                continue;
            }
            if (child.GetComponent<TextMeshPro>())
            {
                continue;
            }
            foodObjects.Add(child.gameObject);
        }
    }

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

    private void StartShrinkAnimation(int index)
    {
        if (!isNotReplenishable && index >= 0 && index < foodObjects.Count && foodObjects[index] != null)
        {
            if (activeCoroutines[index] != null)
                StopCoroutine(activeCoroutines[index]);
            activeCoroutines[index] = StartCoroutine(ShrinkFood(foodObjects[index]));
        }
    }

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

    public int CurrentFood
    {
        get { return currentFood; }
        set
        {
            int oldValue = currentFood;
            currentFood = Mathf.Clamp(value, 0, maxFood);
            UpdateFoodState();

            if (!isNotReplenishable)
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
                Destroy(gameObject);
            }
        }
    }

    public bool HasFood => hasFood;
    public float FoodSatiety => foodSatiety;

    // Reservation system methods
    public void ReserveFood(CreatureBehavior creature, float duration)
    {
        if (!reservations.ContainsKey(creature))
        {
            reservations.Add(creature, 1f);
            reservationTimers.Add(creature, duration);
            Debug.Log($"{creature.name} reserved food at {name}");
        }
    }

    public void ReleaseReservation(CreatureBehavior creature)
    {
        if (reservations.ContainsKey(creature))
        {
            reservations.Remove(creature);
            reservationTimers.Remove(creature);
            Debug.Log($"{creature.name} released reservation at {name}");
        }
    }

    public void UpdateReservations()
    {
        List<CreatureBehavior> toRemove = new List<CreatureBehavior>();
        foreach (var creature in reservationTimers.Keys.ToList())
        {
            reservationTimers[creature] -= Time.deltaTime;
            if (reservationTimers[creature] <= 0 || creature.currentState == CreatureBehavior.State.Dead)
            {
                toRemove.Add(creature);
            }
        }

        foreach (var creature in toRemove)
        {
            ReleaseReservation(creature);
        }
    }
}