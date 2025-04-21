using UnityEngine;

public class Trait_NomadLeader : Trait
{
    public override int Id => 100;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        // No direct modifications needed; behavior is handled in CreatureBase and CreatureBehavior
        Debug.Log($"{creature.name}: Applied Nomad Leader trait");
    }
}