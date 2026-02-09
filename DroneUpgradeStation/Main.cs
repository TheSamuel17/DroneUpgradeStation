using BepInEx;
using RoR2;
using RoR2.ExpansionManagement;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;
using MonoMod.Cil;
using System;

namespace DroneUpgradeStation
{
    // Dependencies
    [BepInDependency(DirectorAPI.PluginGUID)]

    // Metadata
    [BepInPlugin("Samuel17.DroneUpgradeStation", "DroneUpgradeStation", "1.0.0")]

    public class Main : BaseUnityPlugin
    {
        // Load addressables
        public static InteractableSpawnCard iscDroneAssemblyStation = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC3/DroneAssemblyStation/iscDroneAssemblyStation.asset").WaitForCompletion();
        public static Material matDroneAssemblyStation = Addressables.LoadAssetAsync<Material>("RoR2/DLC3/DroneAssemblyStation/matDroneAssemblyStation.mat").WaitForCompletion();
        public static GameObject droneAssemblyStationPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/DroneAssemblyStation/DroneAssemblyStation.prefab").WaitForCompletion();
        public static ExplicitPickupDropTable blacklist = Addressables.LoadAssetAsync<ExplicitPickupDropTable>("RoR2/DLC3/DroneAssemblyStation/ExcludedItemsDropTable.asset").WaitForCompletion();
        public static ExpansionDef dlc3 = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC3/DLC3.asset").WaitForCompletion();

        public void Awake()
        {
            // Logging!
            Log.Init(Logger);

            // Load configs
            Configs.Init(Config);

            // Swap required tag from Drone to Mechanical
            if (Configs.bodyFlagMechanical.Value == true)
            {
                bool hookFailed = true;
                IL.EntityStates.DroneAssemblyStation.AssemblingDroneState.OnEnter += (il) =>
                {
                    ILCursor c = new(il);

                    if (
                        c.TryGotoNext(MoveType.Before,
                        x => x.MatchLdfld(typeof(CharacterBody), nameof(CharacterBody.bodyFlags))
                    ))
                    {
                        c.RemoveRange(3);
                        c.EmitDelegate<Func<CharacterBody, bool>>((body) =>
                        {
                            return (body.bodyFlags & CharacterBody.BodyFlags.Mechanical) != CharacterBody.BodyFlags.None;
                        });
                        hookFailed = false;
                    }

                    if (hookFailed == true)
                    {
                        Log.Error("Drone Upgrade Station BodyFlag hook failed!");
                    }
                };

                bool hook2Failed = true;
                IL.EntityStates.DroneAssemblyStation.AssemblingDroneState.TransferItem += (il) =>
                {
                    ILCursor c = new(il);

                    if (
                        c.TryGotoNext(MoveType.Before,
                        x => x.MatchLdfld(typeof(CharacterBody), nameof(CharacterBody.bodyFlags))
                    ))
                    {
                        c.RemoveRange(3);
                        c.EmitDelegate<Func<CharacterBody, bool>>((body) =>
                        {
                            return (body.bodyFlags & CharacterBody.BodyFlags.Mechanical) != CharacterBody.BodyFlags.None;
                        });
                        hook2Failed = false;
                    }

                    if (hook2Failed == true)
                    {
                        Log.Error("Drone Upgrade Station BodyFlag hook (2/2) failed!");
                    }
                };
            } 

            // Lock to DLC3
            ExpansionRequirementComponent expansionRequirementComponent = droneAssemblyStationPrefab.GetComponent<ExpansionRequirementComponent>();
            if (!expansionRequirementComponent)
            {
                expansionRequirementComponent = droneAssemblyStationPrefab.AddComponent<ExpansionRequirementComponent>();
                expansionRequirementComponent.requiredExpansion = dlc3;
            } 

            // Adjust model color
            matDroneAssemblyStation.color = new Color(128f/255f, 255f/255f, 0f/255f);

            // Functionality
            OtherStuff();

            // Adjust ISC
            iscDroneAssemblyStation.directorCreditCost = Configs.creditCost.Value;
            iscDroneAssemblyStation.maxSpawnsPerStage = Configs.maxSpawns.Value;

            RoR2Application.onLoad += () =>
            {
                // Sort configs & add to stages
                AddToStages(Configs.sceneList.Value);

                // Item blacklists
                if (Configs.blacklistCannotCopy.Value == true)
                {
                    HG.ReadOnlyArray<ItemIndex> cannotCopyItems = ItemCatalog.GetItemsWithTag(ItemTag.CannotCopy);
                    foreach (ItemIndex itemIndex in cannotCopyItems)
                    {
                        ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                        if (itemDef)
                        {
                            AddToBlacklist(itemDef);
                        }
                    }
                }

                BlacklistExtraItems(Configs.blacklistExtraList.Value);
            };
        }

        private void OtherStuff()
        {
            // Remove unnecessary PurchaseInteraction; it works fine without it.
            var purchaseInteract = droneAssemblyStationPrefab.GetComponent<PurchaseInteraction>();
            if (purchaseInteract)
            {
                Utils.RemoveComponent<PurchaseInteraction>(droneAssemblyStationPrefab);
            }
        }

        private void AddToStages(string sceneList)
        {
            sceneList = new string(sceneList.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray());
            string[] sceneStringList = sceneList.Split(',');

            foreach (string scene in sceneStringList)
            {
                SceneDef sceneDef = SceneCatalog.FindSceneDef(scene);
                if (sceneDef)
                {
                    DirectorAPI.DirectorCardHolder droneAssemblyStationSpawnCard = new()
                    {
                        Card = new()
                        {
                            minimumStageCompletions = Configs.minStage.Value - 1,
                            preventOverhead = false,
                            selectionWeight = Configs.weight.Value,
                            spawnCard = iscDroneAssemblyStation,
                        },
                        InteractableCategory = DirectorAPI.InteractableCategory.Duplicator,
                    };

                    DirectorAPI.Helpers.AddNewInteractableToStage(droneAssemblyStationSpawnCard, DirectorAPI.GetStageEnumFromSceneDef(sceneDef), scene);

                    Log.Message("Drone Upgrade Station added to " + sceneDef.baseSceneName + ".");
                }  
            }
        }

        private void AddToBlacklist(ItemDef itemDef)
        {
            HG.ArrayUtils.ArrayAppend(ref blacklist.pickupEntries, new ExplicitPickupDropTable.PickupDefEntry()
            {
                pickupDef = itemDef,
            });
        }

        private void BlacklistExtraItems(string itemList)
        {
            itemList = new string(itemList.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray());
            string[] itemStringList = itemList.Split(',');

            foreach (string item in itemStringList)
            {
                ItemIndex itemIndex = ItemCatalog.FindItemIndex(item);
                if (itemIndex != ItemIndex.None)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                    if (itemDef)
                    {
                        AddToBlacklist(itemDef);
                    }
                }
            }
        }
    }
}
