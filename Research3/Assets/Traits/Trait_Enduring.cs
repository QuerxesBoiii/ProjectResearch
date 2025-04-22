using UnityEngine;

public class Trait_Enduring : Trait
{
    public override int Id => 18;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.maxStamina *= 2f;
        creature.staminaRegenRate *= 1.5f;
        Debug.Log($"{creature.name}: Applied Enduring trait - Stamina doubled, regen rate +50%.");
    }
}