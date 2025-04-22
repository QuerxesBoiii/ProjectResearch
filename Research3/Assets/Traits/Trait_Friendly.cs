using UnityEngine;

public class Trait_Friendly : Trait
{
    public override int Id => 9;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.combatBehavior = CreatureBehavior.CombatBehavior.Friendly;
        Debug.Log($"{creature.name}: Applied Friendly trait - Will not attack unless attacked.");
    }
}