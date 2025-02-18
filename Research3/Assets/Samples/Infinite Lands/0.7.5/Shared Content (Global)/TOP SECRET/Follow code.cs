using UnityEngine;

public class Followcode : MonoBehaviour
{
    public GameObject Head;
    public GameObject Bodypart;
    public float speed;
    public float distancelimit;
    private float distance;

    void Update()
    {
        distance = Vector3.Distance(Head.transform.position, Bodypart.transform.position);
        if (distance >= distancelimit)
        {
            Bodypart.transform.position = Vector3.MoveTowards(Bodypart.transform.position, Head.transform.position, speed);
        }
    }
}
