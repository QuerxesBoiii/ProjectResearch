using UnityEngine;

public class Trait_Glutton : Trait
{
    public override int Id => 15;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.maxFoodLevelMultiplier *= 2f;
        creature.hungerDecreaseIntervalMultiplier *= 0.5f;
        Debug.Log($"{creature.name}: Applied Gluttonous trait - Food capacity doubled, hunger decreases twice as fast.");
    }
}