using UnityEngine;

public class Trait_Climber : Trait
{
    public override int Id => 10;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.canClimb = true;
        Debug.Log($"{creature.name}: Applied Climbing trait - Can climb.");
    }
}