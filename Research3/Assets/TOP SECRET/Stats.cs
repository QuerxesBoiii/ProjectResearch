using UnityEngine;

[System.Serializable]
public struct Stats
{
    public float distanceLimit;
    public float speed;

    public Stats(float distanceLimit, float speed)
    {
        this.distanceLimit = distanceLimit;
        this.speed = speed;
    }
}