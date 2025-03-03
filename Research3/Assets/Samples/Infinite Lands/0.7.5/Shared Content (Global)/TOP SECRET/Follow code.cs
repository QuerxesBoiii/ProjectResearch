using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class Followcode : MonoBehaviour
{
    public GameObject Head; // The object this follows
    public GameObject Bodypart; // This object itself
    private float speed;
    private float distancelimit;

    private void OnEnable()
    {
        NoHead.OnStatsChanged += HandleStatsChange;
    }

    private void OnDisable()
    {
        NoHead.OnStatsChanged -= HandleStatsChange;
    }

    private void HandleStatsChange(Stats stats)
    {
        distancelimit = stats.distanceLimit;
        speed = stats.speed;
    }

    void Update()
    {
        if (Head == null) return; // Ensure there's a reference

        float distance = Vector3.Distance(Head.transform.position, Bodypart.transform.position);
        Vector3 direction = (Bodypart.transform.position - Head.transform.position).normalized;

        if (distance >= distancelimit)
        {
            // Move towards the Head if too far
            Bodypart.transform.position = Vector3.MoveTowards(Bodypart.transform.position, Head.transform.position, speed * Time.deltaTime);
        }
        else if (distance <= distancelimit - (distancelimit / 10))
        {
            // Move away if too close
            Bodypart.transform.position += direction * speed * Time.deltaTime;
        }
    }
}
