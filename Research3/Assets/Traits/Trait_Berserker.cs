using UnityEngine;

public class Trait_Berserker : Trait
{
    public override int Id => 14;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.creatureCombat.attackDamageMultiplier *= 1.5f;
        Debug.Log($"{creature.name}: Applied Berserk trait - Attack damage +50%.");
    }
}