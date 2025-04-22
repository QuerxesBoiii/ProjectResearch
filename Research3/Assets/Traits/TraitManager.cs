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
        public string HexColor;
    }

    // General Traits (existing traits, removed Bioluminescent, Burrower, Loyal)
    private static readonly Dictionary<int, TraitInfo> generalTraits = new Dictionary<int, TraitInfo>
    {
        { 1, new TraitInfo { Name = "Small", Cost = 1, Type = typeof(Trait_Small), HexColor = "#e2ffc9" } },
        { 2, new TraitInfo { Name = "Big", Cost = 4, Type = typeof(Trait_Big), HexColor = "#783506" } },
        { 3, new TraitInfo { Name = "Fortified", Cost = 4, Type = typeof(Trait_Fortified), HexColor = "#4795d6" } },
        { 4, new TraitInfo { Name = "Fast", Cost = 3, Type = typeof(Trait_Fast), HexColor = "#fff700" } },
        { 5, new TraitInfo { Name = "Efficient Eater", Cost = 2, Type = typeof(Trait_EfficientEater), HexColor = "#228B22" } },
        { 6, new TraitInfo { Name = "Immortal", Cost = 5, Type = typeof(Trait_Immortal), HexColor = "#ffd700" } },
        { 7, new TraitInfo { Name = "Fertile", Cost = 3, Type = typeof(Trait_Fertile), HexColor = "#FF69B4" } },
        { 8, new TraitInfo { Name = "Hunting", Cost = 3, Type = typeof(Trait_Hunter), HexColor = "#FF0000" } },
        { 9, new TraitInfo { Name = "Friendly", Cost = 2, Type = typeof(Trait_Friendly), HexColor = "#2fff00" } },
        { 10, new TraitInfo { Name = "Climbing", Cost = 1, Type = typeof(Trait_Climber), HexColor = "#dbd9d9" } },
        { 11, new TraitInfo { Name = "Scout", Cost = 2, Type = typeof(Trait_Scout), HexColor = "#abd100" } },
        { 12, new TraitInfo { Name = "Wandering", Cost = 2, Type = typeof(Trait_Wanderer), HexColor = "#87CEEB" } },
        { 13, new TraitInfo { Name = "Venomous", Cost = 4, Type = typeof(Trait_Venomous), HexColor = "#55ff00" } },
        { 14, new TraitInfo { Name = "Berserk", Cost = 3, Type = typeof(Trait_Berserker), HexColor = "#DC143C" } },
        { 15, new TraitInfo { Name = "Gluttonous", Cost = 3, Type = typeof(Trait_Glutton), HexColor = "#FFA500" } },
        { 16, new TraitInfo { Name = "Prolific", Cost = 2, Type = typeof(Trait_Prolific), HexColor = "#ffa1e4" } },
        { 17, new TraitInfo { Name = "Altruistic", Cost = 3, Type = typeof(Trait_Altruist), HexColor = "#98FB98" } },
        { 18, new TraitInfo { Name = "Enduring", Cost = 2, Type = typeof(Trait_Enduring), HexColor = "#4e6e0c" } },
        { 19, new TraitInfo { Name = "Adrenaline", Cost = 3, Type = typeof(Trait_Adrenaline), HexColor = "#FF4500" } },
        { 20, new TraitInfo { Name = "Parasitic", Cost = 4, Type = typeof(Trait_Parasitic), HexColor = "#eb024c" } },
        { 21, new TraitInfo { Name = "Migratory", Cost = 3, Type = typeof(Trait_Migratory), HexColor = "#1E90FF" } },
        { 22, new TraitInfo { Name = "Tactician", Cost = 3, Type = typeof(Trait_Tactician), HexColor = "#6a85a1" } },
        { 23, new TraitInfo { Name = "Ambush", Cost = 3, Type = typeof(Trait_Ambusher), HexColor = "#212121" } },
        { 24, new TraitInfo { Name = "Swift Breeder", Cost = 2, Type = typeof(Trait_SwiftBreeder), HexColor = "#ff7dfd" } },
        { 27, new TraitInfo { Name = "Pheromonal", Cost = 2, Type = typeof(Trait_Pheromonal), HexColor = "#FF00FF" } },
        { 28, new TraitInfo { Name = "Keen", Cost = 2, Type = typeof(Trait_Keen), HexColor = "#20B2AA" } },
        { 29, new TraitInfo { Name = "Spiky", Cost = 5, Type = typeof(Trait_Spiky), HexColor = "#FF4500" } },
        { 30, new TraitInfo { Name = "TrapMaker", Cost = 3, Type = typeof(Trait_TrapMaker), HexColor = "#4f7d00" } },
        { 31, new TraitInfo { Name = "Giant", Cost = 6, Type = typeof(Trait_Giant), HexColor = "#5A2D0C" } }
    };

    // Social Traits (new category, exactly one required, max one allowed)
    private static readonly Dictionary<int, TraitInfo> socialTraits = new Dictionary<int, TraitInfo>
    {
        { 100, new TraitInfo { Name = "Nomad Leader", Cost = 0, Type = typeof(Trait_NomadLeader), HexColor = "#FFFFFF" } }
    };

    // Helper to combine both trait dictionaries for lookups
    private static Dictionary<int, TraitInfo> GetAllTraits()
    {
        return generalTraits.Concat(socialTraits).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static string GetTraitName(int traitId)
    {
        var traits = GetAllTraits();
        return traits.ContainsKey(traitId) ? traits[traitId].Name : null;
    }

    public static string GetTraitPrefix(int traitId)
    {
        return GetTraitName(traitId) ?? "";
    }

    public static System.Type GetTraitType(int traitId)
    {
        var traits = GetAllTraits();
        return traits.ContainsKey(traitId) ? traits[traitId].Type : null;
    }

    public static int GetTraitCost(int traitId)
    {
        var traits = GetAllTraits();
        return traits.ContainsKey(traitId) ? traits[traitId].Cost : 0;
    }

    public static string GetTraitHexColor(int traitId)
    {
        var traits = GetAllTraits();
        return traits.ContainsKey(traitId) ? traits[traitId].HexColor : "#FFFFFF";
    }

    public static int[] GetAllGeneralTraitIds()
    {
        return generalTraits.Keys.ToArray();
    }

    public static int[] GetAllSocialTraitIds()
    {
        return socialTraits.Keys.ToArray();
    }

    public static int[] GetAllTraitIds()
    {
        return GetAllTraits().Keys.ToArray();
    }
}