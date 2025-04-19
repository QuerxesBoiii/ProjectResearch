using UnityEngine;
using System;
using System.Collections.Generic;

public class NoHead : MonoBehaviour
{
    public static event Action<Stats> OnStatsChanged;

    [SerializeField] private float distanceLimit; // Distance limit between "bodyparts" can make for some interesting animations
    [SerializeField] private float speed; // Adjustable speed because it's noice
    [SerializeField] private int thoraxCount; // Number of body parts for later on generation
    [SerializeField] private GameObject bodyPartPrefab; // Prefab for body parts so we can easily switch it out if needed.
    [SerializeField] private Transform centipedeParent; // Reference to the parent object for less clutter

    private List<GameObject> bodyParts = new List<GameObject>(); // Track spawned body parts

    public int ThoraxCount
    {
        get => thoraxCount;
        set
        {
            thoraxCount = Mathf.Max(0, value); // Ensure it's not negative
            AdjustBodyParts(); // Adjust number of body parts
        }
    }

    public float DistanceLimit
    {
        get => distanceLimit;
        set
        {
            distanceLimit = value;
            TriggerUpdate();
        }
    }

    public float Speed
    {
        get => speed;
        set
        {
            speed = value;
            TriggerUpdate();
        }
    }

    private void Start()
    {
        AdjustBodyParts(); // Initialize body parts
        TriggerUpdate();
    }

    private void OnValidate()
    {
        TriggerUpdate();
        AdjustBodyParts(); // Ensure changes apply in the Inspector
    }

    private void TriggerUpdate()
    {
        OnStatsChanged?.Invoke(new Stats(distanceLimit, speed));
    }

    private void AdjustBodyParts()
    {
        // Remove excess body parts
        while (bodyParts.Count > thoraxCount)
        {
            GameObject excessPart = bodyParts[bodyParts.Count - 1];
            bodyParts.RemoveAt(bodyParts.Count - 1);
            Destroy(excessPart);
        }

        // Add missing body parts
        while (bodyParts.Count < thoraxCount)
        {
            GameObject previous = bodyParts.Count == 0 ? this.gameObject : bodyParts[bodyParts.Count - 1];

            GameObject newPart = Instantiate(bodyPartPrefab, previous.transform.position - new Vector3(0, 0, 1), Quaternion.identity);

            // **Set the new body part's parent to the Centipede empty object**
            if (centipedeParent != null)
            {
                newPart.transform.SetParent(centipedeParent);
            }

            bodyParts.Add(newPart);

            Followcode followScript = newPart.GetComponent<Followcode>();
            if (followScript != null)
            {
                followScript.Head = previous;
                followScript.Bodypart = newPart;
            }
        }

        TriggerUpdate();
    }

}
