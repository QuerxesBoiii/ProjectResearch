using UnityEngine;

public class Trait_NomadLeader : Trait { public override int Id => 100;

public override void ApplyTrait(CreatureBehavior creature)
{
    if (creature.owningBase != null && creature.owningBase.Leader == creature)
    {
        creature.sizeMultiplier = 1.5f; // Set leader to 2x size
        Debug.Log($"{creature.name}: Applied Nomad Leader trait - Size multiplier set to 2x as group leader.");
    }
    else
    {
        Debug.Log($"{creature.name}: Nomad Leader trait applied but not the leader, no size change.");
    }
}

}