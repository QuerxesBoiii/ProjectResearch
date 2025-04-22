using UnityEngine;

public class Trait_Fortified : Trait
{
    public override int Id => 3;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.healthMultiplier *= 2f;
        Debug.Log($"{creature.name}: Applied Fortified trait - Health doubled.");
    }
}