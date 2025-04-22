using UnityEngine;

public class Trait_Hunter : Trait
{
    public override int Id => 8;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.combatBehavior = CreatureBehavior.CombatBehavior.Hunter;
        creature.foodPreferences.Add(CreatureBehavior.FoodType.Meat);
        Debug.Log($"{creature.name}: Applied Hunting trait - Now a hunter, prefers meat.");
    }
}