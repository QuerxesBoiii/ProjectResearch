using UnityEngine;

public class Trait_Venomous : Trait
{
    public override int Id => 13;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        // Assumes CreatureCombat has a method to apply poison
        creature.creatureCombat.canPoison = true;
        Debug.Log($"{creature.name}: Applied Venomous trait - Attacks can poison.");
    }
}