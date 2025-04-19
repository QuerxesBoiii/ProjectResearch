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
                    int maxHealth = Mathf.CeilToInt(creature.Size * 10f);
                    string text = $"ID {creature.CreatureTypeId}\n{creature.FoodLevel}/{creature.MaxFoodLevel} FOOD\n" +
                                  $"{(creature.currentState == CreatureBehavior.State.Dead ? 0 : Mathf.CeilToInt(creature.Health))}/{maxHealth} HP";

                    if (creature.gender == CreatureBehavior.Gender.Female && creature.IsPregnant)
                    {
                        int reproductionCostCounter = creature.TotalFoodLostSincePregnant / 2;
                        text += $"\nPregnant: {reproductionCostCounter}/{creature.ReproductionCostProperty}";
                    }
                    text += $"\n{creature.gender}";

                    textDisplay.text = text;
                }
            }
        }
    }
}