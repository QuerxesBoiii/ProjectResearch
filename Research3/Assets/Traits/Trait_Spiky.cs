using UnityEngine;

public class Trait_Spiky : Trait
{
    public override int Id => 29;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.creatureCombat.canReflectDamage = true;
        Debug.Log($"{creature.name}: Applied Spiky trait - Reflects damage when attacked.");
    }
}