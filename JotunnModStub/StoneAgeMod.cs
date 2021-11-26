#define xxxCUSTOM_INPUT

// JotunnModStub
// a Valheim mod skeleton using Jötunn
// 
// File:    JotunnModStub.cs
// Project: JotunnModStub

using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace StoneAgeMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class StoneAgeMod : BaseUnityPlugin
    {
        public const string PluginGUID = "com.bugshake.stoneagemod";
        public const string PluginName = "StoneAgeMod";
        public const string PluginVersion = "0.0.1";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            // Jotunn comes with MonoMod Detours enabled for hooking Valheim's code
            // https://github.com/MonoMod/MonoMod
            On.FejdStartup.Awake += FejdStartup_Awake;
            
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("ModStub(StoneAgeMod) has landed");

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html

            PrefabManager.OnVanillaPrefabsAvailable += AddClonedItems;
        }

        private void FejdStartup_Awake(On.FejdStartup.orig_Awake orig, FejdStartup self)
        {
            // This code runs before Valheim's FejdStartup.Awake
            Jotunn.Logger.LogInfo("FejdStartup is going to awake");

            // Call this method so the original game method is invoked
            orig(self);

            // This code runs after Valheim's FejdStartup.Awake
            Jotunn.Logger.LogInfo("FejdStartup has awoken");
        }

        // Implementation of cloned items
        private void AddClonedItems()
        {
            try
            {
                // Create the main custom item "throwable rock"
                CustomItem CI = new CustomItem("ThrowStone", "BombOoze");
                ItemManager.Instance.AddItem(CI);

                // Replace vanilla properties of the custom item
                var itemDrop = CI.ItemDrop;
                itemDrop.m_itemData.m_shared.m_name = "$item_throwstone";
                itemDrop.m_itemData.m_shared.m_description = "$item_throwstone_desc";
                itemDrop.m_itemData.m_shared.m_skillType = Skills.SkillType.Unarmed;            // makes you level up i hope
                itemDrop.m_itemData.m_shared.m_damages.m_blunt = 12;                            // makes you do more dmg as you level up i hope
                itemDrop.m_itemData.m_shared.m_damagesPerLevel.m_blunt = 0.5f;                  // makes you do more dmg as you level up i hope
                itemDrop.m_itemData.m_shared.m_attack.m_projectileAccuracy = 2;                 // more accurate, but not as accurate as a bow
                {
                    // copy some properties from normal stone 
                    var stonePrefab = PrefabManager.Cache.GetPrefab<GameObject>("Stone");
                    Jotunn.Logger.LogInfo($"Is dit de echte stone prefab?: {stonePrefab.name}");

                    var stoneItem = new CustomItem("TempStone", "Stone").ItemDrop;
                    itemDrop.m_itemData.m_shared.m_icons = stoneItem.m_itemData.m_shared.m_icons;
                    itemDrop.m_itemData.m_shared.m_weight = stoneItem.m_itemData.m_shared.m_weight;
                }

                // Create the custom projectile based on the projectile greydwarves throw
                var stoneProjectile = new CustomItem("TempStoneProjectile", "Greydwarf_throw_projectile");
                {
                    var bowArrowPrefab = new CustomItem("TempArrow", "bow_projectile").ItemPrefab;
                    var bowtrail = bowArrowPrefab.transform.Find("trail").gameObject;
                    var stonetrail = Instantiate(bowtrail, stoneProjectile.ItemPrefab.transform);
                    itemDrop.m_itemData.m_shared.m_attack.m_attackProjectile = stoneProjectile.ItemPrefab;
                }

                // Change the prefab, which is what the item looks like in your hand or on the floor
                {
                    var prefab = CI.ItemPrefab;

                    // disable fumes
                    var ooz = prefab.transform.Find("attach/bomb/ooz");
                    ooz.gameObject.SetActive(false);

                    // copy stone mesh
                    var stoneMeshFilter = stoneProjectile.ItemPrefab.GetComponentInChildren<MeshFilter>();
                    var meshfilter = prefab.GetComponentInChildren<MeshFilter>();
                    meshfilter.sharedMesh = stoneMeshFilter.sharedMesh;
                    meshfilter.transform.localScale = 0.04f * Vector3.one;

                    // copy stone material
                    var stoneMeshRenderer = stoneProjectile.ItemPrefab.GetComponentInChildren<MeshRenderer>();
                    var meshrenderer = prefab.GetComponentInChildren<MeshRenderer>();
                    meshrenderer.sharedMaterial = stoneMeshRenderer.sharedMaterial;
                }

                // create recipe
                RecipeThrowStone(itemDrop);

#if CUSTOM_INPUT
                // Show a different KeyHint for the sword.
                KeyHintsEvilSword();
#endif
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Error while adding cloned item: {ex.Message}");
            }
            finally
            {
                // You want that to run only once, Jotunn has the item cached for the game session
                PrefabManager.OnVanillaPrefabsAvailable -= AddClonedItems;
            }
        }

        // Implementation of assets via using manual recipe creation and prefab cache's
        private void RecipeThrowStone(ItemDrop itemDrop)
        {
            // Create and add a recipe for the copied item
            Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.name = "Recipe_ThrowStone";
            recipe.m_item = itemDrop;
            //recipe.m_craftingStation = PrefabManager.Cache.GetPrefab<CraftingStation>("piece_workbench");
            recipe.m_resources = new Piece.Requirement[]
            {
                new Piece.Requirement()
                {
                    m_resItem = PrefabManager.Cache.GetPrefab<ItemDrop>("Stone"),
                    m_amount = 10
                },
            };
            recipe.m_amount = 10;
            // Since we got the vanilla prefabs from the cache, no referencing is needed
            CustomRecipe CR = new CustomRecipe(recipe, fixReference: false, fixRequirementReferences: false);
            ItemManager.Instance.AddRecipe(CR);
        }

#if CUSTOM_INPUT
        // Implementation of key hints replacing vanilla keys and using custom keys.
        // KeyHints appear in the same order in which they are defined in the config.
        private void KeyHintsEvilSword()
        {
            // Create custom KeyHints for the item
            KeyHintConfig KHC = new KeyHintConfig
            {
                Item = "EvilSword",
                ButtonConfigs = new[]
                {
                    // Override vanilla "Attack" key text
                    new ButtonConfig { Name = "Attack", HintToken = "$evilsword_shwing" },
                    // User our custom button defined earlier, syncs with the backing config value
                    EvilSwordSpecialButton,
                    // Override vanilla "Mouse Wheel" text
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$evilsword_scroll" }
                }
            };
            KeyHintManager.Instance.AddKeyHint(KHC);
        }
#endif
    }
}