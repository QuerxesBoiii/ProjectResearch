using UnityEngine;

public class Trait_Keen : Trait
{
    public override int Id => 28;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.maxStamina *= 0.9f; // -10% max stamina
    }
}