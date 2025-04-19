using UnityEngine;

public class Trait_Hunter : Trait
{
    public override int Id => 3;
    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.combatBehavior = CreatureBehavior.CombatBehavior.Hunter;
        creature.foodPreferences.Add(CreatureBehavior.FoodType.Meat); // Prefers meat
    }
}