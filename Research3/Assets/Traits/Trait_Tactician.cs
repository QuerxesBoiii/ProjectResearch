using UnityEngine;

public class Trait_Tactician : Trait
{
    public override int Id => 22;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.baseDetectionRadius *= 1.3f;
        creature.creatureCombat.attackDamageMultiplier *= 1.2f;
        Debug.Log($"{creature.name}: Applied Tactician trait - Detection radius +30%, attack damage +20%.");
    }
}