using UnityEngine;

public class Trait_TrapMaker : Trait
{
    public override int Id => 30;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.creatureCombat.canImmobilize = true;
        Debug.Log($"{creature.name}: Applied TrapMaker trait - Attacks can immobilize enemies.");
    }
}