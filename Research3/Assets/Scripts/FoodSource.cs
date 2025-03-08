using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FoodSource : MonoBehaviour
{
    [SerializeField] public int maxFood = 10; // Set in Inspector, not modified by script
    [SerializeField] private int currentFood = 10; // Set in Inspector, only changed via property
    [SerializeField] private float replenishInterval = 15f;
    [SerializeField] public bool isNotReplenishable = false;
    private float replenishTimer = 0f;
    [SerializeField] private bool hasFood = true;
    [SerializeField] private float foodSatiety = 1f;

    [SerializeField] private List<GameObject> foodObjects = new List<GameObject>();
    private List<Coroutine> activeCoroutines;

    [SerializeField] private float shrinkDuration = 1f;
    [SerializeField] private float growDuration = 15f;
    [SerializeField] private float bubbleDuration = 0.5f;

    void Start()
    {
        if (foodObjects.Count == 0)
        {
            PopulateFromChildren();
        }

        // No automatic adjustment of maxFood or currentFood
        activeCoroutines = new List<Coroutine>(new Coroutine[maxFood]);
        UpdateFoodState();

        // Initialize food object visuals based on currentFood
        for (int i = 0; i < foodObjects.Count; i++)
        {
            if (i < currentFood)
                foodObjects[i].transform.localScale = Vector3.one;
            else
                foodObjects[i].transform.localScale = Vector3.zero;
            foodObjects[i].SetActive(true);
        }
    }

    void Update()
    {
        if (!isNotReplenishable)
        {
            replenishTimer += Time.deltaTime;
            if (replenishTimer >= replenishInterval && currentFood < maxFood)
            {
                CurrentFood++;
                replenishTimer = 0f;
            }
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
        hasFood = currentFood > 0 && currentFood >= maxFood * 0.3f;
    }

    private void StartShrinkAnimation(int index)
    {
        if (index >= 0 && index < foodObjects.Count && foodObjects[index] != null)
        {
            if (activeCoroutines[index] != null)
                StopCoroutine(activeCoroutines[index]);
            activeCoroutines[index] = StartCoroutine(ShrinkFood(foodObjects[index]));
        }
    }

    private void StartGrowAnimation(int index)
    {
        if (index >= 0 && index < foodObjects.Count && foodObjects[index] != null)
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
        get => currentFood;
        set
        {
            int oldValue = currentFood;
            currentFood = Mathf.Clamp(value, 0, maxFood);
            UpdateFoodState();

            if (currentFood < oldValue)
            {
                int foodLost = oldValue - currentFood;
                for (int i = 0; i < foodLost; i++)
                {
                    StartShrinkAnimation(currentFood + i);
                }
            }
            else if (currentFood > oldValue)
            {
                int foodGained = currentFood - oldValue;
                for (int i = 0; i < foodGained; i++)
                {
                    StartGrowAnimation(oldValue + i);
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
}