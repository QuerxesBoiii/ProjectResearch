using UnityEngine;

public class Trait_Immortal : Trait
{
    public override int Id => 6;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.maxAge = float.MaxValue;
        Debug.Log($"{creature.name}: Applied Immortal trait - Cannot die of old age.");
    }
}