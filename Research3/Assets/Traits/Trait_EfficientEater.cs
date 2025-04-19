using UnityEngine;

public class Trait_EfficientEater : Trait
{
    public override int Id => 6;
    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.HungerDecreaseInterval *= 1.25f; // Slower hunger decrease
    }
}