using BepInEx.Configuration;

namespace DroneUpgradeStation
{
    public static class Configs
    {
        public static ConfigEntry<bool> blacklistCannotCopy { get; private set; }
        public static ConfigEntry<string> blacklistExtraList { get; private set; }
        public static ConfigEntry<bool> bodyFlagMechanical { get; private set; }
        public static ConfigEntry<int> creditCost { get; private set; }
        public static ConfigEntry<int> maxSpawns { get; private set; }
        public static ConfigEntry<int> minStage { get; private set; }
        public static ConfigEntry<string> sceneList { get; private set; }
        public static ConfigEntry<int> weight { get; private set; }
        public static ConfigEntry<bool> spawnOnComputationalExchange { get; private set; }

        public static string defaultBlacklist = "TreasureCache, FreeChest, Firework, Squid, LowerPricedChests, ExtraShrineItem, Feather, JumpBoost, Duplicator, BarrageOnBoss, ExtraEquipment";
        public static string defaultSceneList = "dampcavesimple, shipgraveyard, rootjungle, repurposedcrater, conduitcanyon, skymeadow, helminthroost";

        public static void Init(ConfigFile cfg)
        {
            blacklistCannotCopy = cfg.Bind("Functionality", "Blacklist CannotCopy Items", true, "Blacklist all items with the CannotCopy tag.\nOr, in other words, items that turrets don't inherit.");
            blacklistExtraList = cfg.Bind("Functionality", "Blacklist Extra Items", defaultBlacklist, "Blacklist additional items of your choice by listing their internal names.\nMake sure to separate them with commas.");
            bodyFlagMechanical = cfg.Bind("Functionality", "Upgrade Mechanical Minions", true, "Allow all Mechanical minions to benefit from the interactable.\nSetting to false will only allow Drones to benefit.");

            creditCost = cfg.Bind("Spawn Parameters", "Credit Cost", 30, "Set the director credit cost.\nFor reference, a regular Chest costs 15.");
            maxSpawns = cfg.Bind("Spawn Parameters", "Max Spawns", 1, "Set the maximum amount of times it can spawn per stage. Set to negative for no limit.");
            minStage = cfg.Bind("Spawn Parameters", "Minimum Stage", 1, "Set the earliest stage number it can spawn in.");
            sceneList = cfg.Bind("Spawn Parameters", "Scene List", defaultSceneList, "List the internal names of the stages it'll spawn in.\nMake sure to separate them with commas.");
            weight = cfg.Bind("Spawn Parameters", "Selection Weight", 4, "Set the odds of it being selected to spawn.\nBy default, it is as rare as Drone Combiner Stations, which is to say, pretty uncommon.");
            spawnOnComputationalExchange = cfg.Bind("Spawn Parameters", "Spawn on Computational Exchange", true, "A guaranteed Drone Upgrade Station will appear on Computational Exchange.");
        }
    }
}
