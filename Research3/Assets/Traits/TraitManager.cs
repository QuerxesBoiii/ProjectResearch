using System;
using System.Collections.Generic;

public static class TraitManager
{
    public const int MaxPoints = 5;

    private static Dictionary<int, TraitInfo> traitInfos = new Dictionary<int, TraitInfo>()
    {
        { 0, new TraitInfo { name = "Big", cost = 2, type = typeof(Trait_Big) } },
        { 1, new TraitInfo { name = "Small", cost = 1, type = typeof(Trait_Small) } },
        { 2, new TraitInfo { name = "Fast", cost = 2, type = typeof(Trait_Fast) } },
        { 3, new TraitInfo { name = "Hunter", cost = 3, type = typeof(Trait_Hunter) } },
        { 4, new TraitInfo { name = "Fortified", cost = 2, type = typeof(Trait_Fortified) } },
        { 5, new TraitInfo { name = "Immortal", cost = 5, type = typeof(Trait_Immortal) } },
        { 6, new TraitInfo { name = "EfficientEater", cost = 1, type = typeof(Trait_EfficientEater) } },
        { 7, new TraitInfo { name = "Fertile", cost = 1, type = typeof(Trait_Fertile) } }
    };

    public static int GetTraitCost(int id) => traitInfos[id].cost;
    public static string GetTraitName(int id) => traitInfos[id].name;
    public static Type GetTraitType(int id) => traitInfos[id].type;
    public static List<int> GetAllTraitIds() => new List<int>(traitInfos.Keys);
}

public struct TraitInfo
{
    public string name;
    public int cost;
    public Type type;
}