using UnityEngine;

public class Trait_Parasitic : Trait
{
    public override int Id => 20;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.foodPreferences.Add(CreatureBehavior.FoodType.Meat);
        creature.maxFoodLevelMultiplier *= 0.5f;
        Debug.Log($"{creature.name}: Applied Parasitic trait - Eats meat, food capacity halved.");
    }
}