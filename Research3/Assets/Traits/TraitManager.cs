using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TraitManager : MonoBehaviour
{
    public class TraitInfo
    {
        public string Name;
        public int Cost;
        public System.Type Type;
    }

    private static readonly Dictionary<int, TraitInfo> traits = new Dictionary<int, TraitInfo>
    {
        { 1, new TraitInfo { Name = "Small", Cost = 1, Type = typeof(Trait_Small) } }, // -25% size, +15% speed, -10% health, -20% max food
        { 2, new TraitInfo { Name = "Big", Cost = 2, Type = typeof(Trait_Big) } }, // +25% size, +25% health, +20% max food, -10% speed, -10% hunger interval
        { 3, new TraitInfo { Name = "Fortified", Cost = 3, Type = typeof(Trait_Fortified) } }, // +50% health, +10% size, -15% speed, -20% reproduction frequency
        { 4, new TraitInfo { Name = "Fast", Cost = 2, Type = typeof(Trait_Fast) } }, // +25% speed, -10% health, -10% hunger interval
        { 5, new TraitInfo { Name = "Efficient Eater", Cost = 2, Type = typeof(Trait_EfficientEater) } }, // +30% hunger interval, +20% max food, -10% speed, -15% reproduction frequency
        { 6, new TraitInfo { Name = "Immortal", Cost = 4, Type = typeof(Trait_Immortal) } }, // +100% lifespan, +20% health, -50% reproduction frequency, -10% hunger interval
        { 7, new TraitInfo { Name = "Fertile", Cost = 2, Type = typeof(Trait_Fertile) } }, // +30% reproduction frequency, -10% health
        { 8, new TraitInfo { Name = "Hunting", Cost = 3, Type = typeof(Trait_Hunter) } }, // Enables hunting, +20% speed, -20% hunger interval, -10% max food, -25% max stamina, -10% stamina regen
        { 9, new TraitInfo { Name = "Friendly", Cost = 2, Type = typeof(Trait_Friendly) } }, // Sets combat to Friendly, +20% reproduction frequency, -10% health, +25% max stamina
        { 10, new TraitInfo { Name = "Climbing", Cost = 1, Type = typeof(Trait_Climber) } }, // Enables climbing, -5% speed
        { 11, new TraitInfo { Name = "Scout", Cost = 2, Type = typeof(Trait_Scout) } }, // +25% detection radius, -10% health
        { 12, new TraitInfo { Name = "Wandering", Cost = 2, Type = typeof(Trait_Wanderer) } }, // +50% wander radius, -20% reproduction frequency
        { 13, new TraitInfo { Name = "Venomous", Cost = 4, Type = typeof(Trait_Venomous) } }, // Attacks apply poison (0.5 damage/s for 5s)
        { 14, new TraitInfo { Name = "Berserk", Cost = 3, Type = typeof(Trait_Berserker) } }, // -10% max food; +25% damage when health < 30% (handled in CreatureCombat)
        { 15, new TraitInfo { Name = "Gluttonous", Cost = 3, Type = typeof(Trait_Glutton) } }, // +25% max food, +10% health, -20% hunger interval, -5% speed, +5% size
        { 16, new TraitInfo { Name = "Prolific", Cost = 2, Type = typeof(Trait_Prolific) } }, // -10% health; 10% twin chance (handled in BirthBaby)
        { 17, new TraitInfo { Name = "Altruistic", Cost = 3, Type = typeof(Trait_Altruist) } }, // -20% max food; shares food with others (handled in TryShareFood)
        { 18, new TraitInfo { Name = "Enduring", Cost = 2, Type = typeof(Trait_Enduring) } }, // +50% max stamina, +10% stamina regen, -20% speed
        { 19, new TraitInfo { Name = "Adrenaline", Cost = 3, Type = typeof(Trait_Adrenaline) } }, // -20% stamina regen; +50% sprint speed when health < 30% (handled in sprintSpeed)
        { 20, new TraitInfo { Name = "Parasitic", Cost = 4, Type = typeof(Trait_Parasitic) } }, // Steals 1 food per attack (handled in CreatureCombat)
        { 21, new TraitInfo { Name = "Migratory", Cost = 3, Type = typeof(Trait_Migratory) } }, // +5% hunger rate; migrates every 2 min (handled in Update)
        { 22, new TraitInfo { Name = "Tactician", Cost = 3, Type = typeof(Trait_Tactician) } }, // -15% max stamina; +25% damage if target health % < own (handled in CreatureCombat)
        { 23, new TraitInfo { Name = "Ambush", Cost = 3, Type = typeof(Trait_Ambusher) } }, // -20% attack speed, +50% first attack damage (handled in CreatureCombat)
        { 24, new TraitInfo { Name = "Swift Breeder", Cost = 2, Type = typeof(Trait_SwiftBreeder) } }, // -10% health; -20% reproduction cost (handled in ReproductionCost)
        { 25, new TraitInfo { Name = "Burrowing", Cost = 2, Type = typeof(Trait_Burrower) } }, // -10% speed; burrows when hit (handled in CreatureCombat)
        { 26, new TraitInfo { Name = "Cannibal", Cost = 3, Type = typeof(Trait_Cannibal) } }, // -10% reproduction frequency; eats same-type meat (handled in SearchForFood)
        { 27, new TraitInfo { Name = "Pheromonal", Cost = 2, Type = typeof(Trait_Pheromonal) } }, // -10% max food; +50% mate detection range (handled in FindMateForReproduction)
        { 28, new TraitInfo { Name = "Keen", Cost = 2, Type = typeof(Trait_Keen) } }, // -10% max stamina; +25% food detection radius (handled in GetFoodDetectionRadius)
        { 29, new TraitInfo { Name = "Spiky", Cost = 5, Type = typeof(Trait_Spiky) } }, // -10% health, -20% speed, -20% max stamina; reflects 20% damage (handled in CreatureCombat)
        { 30, new TraitInfo { Name = "TrapMaker", Cost = 3, Type = typeof(Trait_TrapMaker) } } // -15% speed; immobilizes target for 3s on first attack (handled in CreatureCombat)
    };

    public static string GetTraitName(int traitId)
    {
        return traits.ContainsKey(traitId) ? traits[traitId].Name : null;
    }

    public static System.Type GetTraitType(int traitId)
    {
        return traits.ContainsKey(traitId) ? traits[traitId].Type : null;
    }

    public static int GetTraitCost(int traitId)
    {
        return traits.ContainsKey(traitId) ? traits[traitId].Cost : 0;
    }

    public static int[] GetAllTraitIds()
    {
        return traits.Keys.ToArray();
    }
}