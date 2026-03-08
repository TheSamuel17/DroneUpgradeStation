using BepInEx;
using RoR2;
using RoR2.ExpansionManagement;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;
using MonoMod.Cil;
using System;
using UnityEngine.UI;
using UnityEngine.Networking;
using LeTai.Asset.TranslucentImage;

namespace DroneUpgradeStation
{
    // Dependencies
    [BepInDependency(DirectorAPI.PluginGUID)]

    // Metadata
    [BepInPlugin("Samuel17.DroneUpgradeStation", "DroneUpgradeStation", "1.0.2")]

    public class Main : BaseUnityPlugin
    {
        // Load addressables
        public static InteractableSpawnCard iscDroneAssemblyStation = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC3/DroneAssemblyStation/iscDroneAssemblyStation.asset").WaitForCompletion();
        public static Material matDroneAssemblyStation = Addressables.LoadAssetAsync<Material>("RoR2/DLC3/DroneAssemblyStation/matDroneAssemblyStation.mat").WaitForCompletion();
        public static GameObject droneAssemblyStationPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/DroneAssemblyStation/DroneAssemblyStation.prefab").WaitForCompletion();
        public static ExplicitPickupDropTable blacklist = Addressables.LoadAssetAsync<ExplicitPickupDropTable>("RoR2/DLC3/DroneAssemblyStation/ExcludedItemsDropTable.asset").WaitForCompletion();
        public static GameObject pickerPanel = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/DroneAssemblyStation/AssemblyStationPickerPanelv2.prefab").WaitForCompletion();
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

            // Preserve highest item count stack when combining
            On.EntityStates.DroneCombiner.DroneCombinerCombining.StartDroneCombineSequence += HandleItemsWhenCombining;

            // SFX
            On.EntityStates.DroneAssemblyStation.AssemblingDroneState.FixedUpdate += HandleSFXPart1;
            On.EntityStates.DroneAssemblyStation.AssemblingDroneState.OnExit += HandleSFXPart2;

            // Lock to DLC3
            ExpansionRequirementComponent expansionRequirementComponent = droneAssemblyStationPrefab.GetComponent<ExpansionRequirementComponent>();
            if (!expansionRequirementComponent)
            {
                expansionRequirementComponent = droneAssemblyStationPrefab.AddComponent<ExpansionRequirementComponent>();
                expansionRequirementComponent.requiredExpansion = dlc3;
            } 

            // Adjust model color
            matDroneAssemblyStation.color = new Color(128f/255f, 255f/255f, 0f/255f);
            
            // Guaranteed spawn on Computational Exchange
            if (Configs.spawnOnComputationalExchange.Value == true)
            {
                Stage.onStageStartGlobal += OnStageStartGlobal;
            }

            // Functionality
            ItemCatalog.availability.CallWhenAvailable(HandleBlacklist);
            SceneCatalog.availability.CallWhenAvailable(HandleStages);
            OtherStuff();

            // Adjust ISC
            iscDroneAssemblyStation.directorCreditCost = Configs.creditCost.Value;
            iscDroneAssemblyStation.maxSpawnsPerStage = Configs.maxSpawns.Value;
        }

        private void HandleItemsWhenCombining(On.EntityStates.DroneCombiner.DroneCombinerCombining.orig_StartDroneCombineSequence orig, EntityStates.DroneCombiner.DroneCombinerCombining self)
        {
            if (NetworkServer.active)
            {
                CharacterBody drone1 = self.toUpgrade;
                CharacterBody drone2 = self.toDestroy;

                if (drone1 && drone1.inventory && drone2 && drone2.inventory)
                {
                    var drone2collection = drone2.inventory.permanentItemStacks.GetNonZeroIndicesSpan();
                    foreach (ItemIndex itemIndex in drone2collection)
                    {
                        int countDrone1 = drone1.inventory.GetItemCountPermanent(itemIndex);
                        int countDrone2 = drone2.inventory.GetItemCountPermanent(itemIndex);
                        if (countDrone1 < countDrone2)
                        {
                            drone1.inventory.GiveItemPermanent(itemIndex, countDrone2 - countDrone1);
                        }
                    }
                }
            }

            orig(self);
        }

        private void HandleSFXPart1(On.EntityStates.DroneAssemblyStation.AssemblingDroneState.orig_FixedUpdate orig, EntityStates.DroneAssemblyStation.AssemblingDroneState self)
        {
            bool canPlaySound = self.fixedAge < 1.5f;

            orig(self);

            if (canPlaySound && self.fixedAge >= 1.5f && self.gameObject)
            {
                Util.PlaySound("Play_GG_INTER_DroneAssembly_Working", self.gameObject);
            }
        }

        private void HandleSFXPart2(On.EntityStates.DroneAssemblyStation.AssemblingDroneState.orig_OnExit orig, EntityStates.DroneAssemblyStation.AssemblingDroneState self)
        {
            if (self.gameObject)
            {
                Util.PlaySound("Play_GG_INTER_DroneAssembly_DroneReady", self.gameObject);
            }

            orig(self);
        }

        private void OnStageStartGlobal(Stage stage)
        {
            if (!SceneInfo.instance) return;
            if (SceneInfo.instance.sceneDef != SceneCatalog.FindSceneDef("computationalexchange")) return;

            Vector3 position = new Vector3(-47.6f, 121f, -50f);
            Vector3 rotation = new Vector3(5f, 165f, 0f);

            GameObject station = Instantiate(droneAssemblyStationPrefab, position, Quaternion.Euler(rotation));
            station.transform.eulerAngles = rotation;
            NetworkServer.Spawn(station);
        }

        private void OtherStuff()
        {
            // Remove unnecessary PurchaseInteraction; it works fine without it.
            var purchaseInteract = droneAssemblyStationPrefab.GetComponent<PurchaseInteraction>();
            if (purchaseInteract)
            {
                Utils.RemoveComponent<PurchaseInteraction>(droneAssemblyStationPrefab);
            }

            // Improve UI
            On.RoR2.UI.PickupPickerPanel.SetPickupOptions += PickupPickerPanel_SetPickupOptions;

            RectTransform rt = pickerPanel.GetComponent<RectTransform>();
            if (rt)
            {
                rt.localScale = new Vector3(.75f, .75f, .75f);
                rt.anchorMax = new Vector2(1.1f, 1f);
            }
            
            pickerPanel.GetComponent<TranslucentImage>().enabled = false;

            RoR2.UI.PickupPickerPanel ppp = pickerPanel.GetComponent<RoR2.UI.PickupPickerPanel>();
            if (ppp)
            {
                ppp.maxColumnCount = 12;
                GridLayoutGroup glg = ppp.gridlayoutGroup;
                if (glg)
                {
                    glg.GetComponent<RectTransform>().localScale = new Vector3(.8f, .8f, .8f);
                }
            }
        }

        private void HandleStages()
        {
            // Sort configs & add to stages
            AddToStages(Configs.sceneList.Value);
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

        private void HandleBlacklist()
        {
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

        private void PickupPickerPanel_SetPickupOptions(On.RoR2.UI.PickupPickerPanel.orig_SetPickupOptions orig, RoR2.UI.PickupPickerPanel self, PickupPickerController.Option[] options) // BROUGHT TO YOU BY LOOKINGGLASS
        {
            if (!self.name.StartsWith("AssemblyStationPickerPanelv2"))
            {
                orig(self, options);
                return;
            }

            Transform t = self.transform.Find("MainPanel");
            if (t is not null)
            {
                Transform background = t.Find("Juice/BG");
                if (background is not null)
                {
                    Color originalColor = background.GetComponent<Image>().color;
                    background.GetComponent<Image>().color = new Color(originalColor.r, originalColor.g, originalColor.b, .5f);
                }
            }

            int itemCount = options.Length;

            int maxHeight = 12;
            int value = Mathf.CeilToInt((Mathf.Sqrt(itemCount) + 2));
            GridLayoutGroup gridLayoutGroup = self.transform.GetComponentInChildren<GridLayoutGroup>();
            if (gridLayoutGroup)
            {
                gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayoutGroup.constraintCount = 12;
                self.maxColumnCount = gridLayoutGroup.constraintCount;
                self.automaticButtonNavigation = true;
            }

            orig(self, options);

            if (t is not null)
            {
                RectTransform r = t.GetComponent<RectTransform>();

                float height = Mathf.Min(value, maxHeight) * (r.sizeDelta.x / 8f);
                value = value <= maxHeight ? value : value + 1 + value - maxHeight;
                float width = (value) * (r.sizeDelta.x / 8f);
                width = Mathf.Max(width, 340f);
                height = Mathf.Max(height, 340f);
                r.sizeDelta = new Vector2(width, height);
            }
        }
    }
}
