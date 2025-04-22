using UnityEngine;

public class Trait_EfficientEater : Trait
{
    public override int Id => 5;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.hungerDecreaseIntervalMultiplier *= 2f;
        Debug.Log($"{creature.name}: Applied Efficient Eater trait - Hunger decreases half as fast.");
    }
}