using UnityEngine;

public class Trait_Tactician : Trait
{
    public override int Id => 22;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.maxStamina *= 0.85f; // -15% max stamina
    }
}