using UnityEngine;

public class Trait_Adrenaline : Trait
{
    public override int Id => 19;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        // Speed boost when health < 30% is handled in CreatureBehavior's sprintSpeed
        Debug.Log($"{creature.name}: Applied Adrenaline trait - Speed +50% when health below 30%.");
    }
}