using TMPro;
using UnityEngine;

public class ScanToolScript : MonoBehaviour
{
    public float fireRate;
    public GameObject viewCamera;
    private float nextFire;
    public TMP_Text textDisplay;
    private CreatureBehavior creature;

    void Update()
    {
        if (nextFire > 0)
            nextFire -= Time.deltaTime;

        if (Input.GetButton("Fire1") && nextFire <= 0)
        {
            nextFire = 1 / fireRate;
            Fire();
        }
    }

    void Fire()
    {
        Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.transform.gameObject.GetComponent<CreatureBehavior>())
            {
                Debug.Log("Hit!");
                creature = hit.transform.gameObject.GetComponent<CreatureBehavior>();
                if (creature != null)
                {
                    int maxHealth = Mathf.CeilToInt(creature._size * 10f);
                    string text = $"ID {creature._creatureTypeId}\n{creature._foodLevel}/{creature._maxFoodLevel} FOOD\n" +
                                  $"{(creature.currentState == CreatureBehavior.State.Dead ? 0 : Mathf.CeilToInt(creature.health))}/{maxHealth} HP";

                    if (creature._gender == CreatureBehavior.Gender.Female && creature._isPregnant)
                    {
                        int reproductionCostCounter = creature._totalFoodLostSincePregnant / 2;
                        text += $"\nPregnant: {reproductionCostCounter}/{creature._ReproductionCost}";
                    }
                    text += $"\n{creature._gender}";

                    textDisplay.text = text;
                }
            }
        }
    }
}
