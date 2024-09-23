using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LongshipUpgrades
{
    [BepInPlugin(pluginID, pluginName, pluginVersion)]
    public class LongshipUpgrades : BaseUnityPlugin
    {
        public const string pluginID = "shudnal.LongshipUpgrades";
        public const string pluginName = "Longship Upgrades";
        public const string pluginVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(pluginID);

        internal static readonly ConfigSync configSync = new ConfigSync(pluginID) { DisplayName = pluginName, CurrentVersion = pluginVersion, MinimumRequiredVersion = pluginVersion };

        internal static LongshipUpgrades instance;

        internal static ConfigEntry<bool> configLocked;
        internal static ConfigEntry<bool> loggingEnabled;

        internal static ConfigEntry<Color> hintStationColor;
        internal static ConfigEntry<Color> hintColor;
        internal static ConfigEntry<Color> hintAmountColor;
        internal static ConfigEntry<Color> hintItemColor;

        internal static ConfigEntry<bool> containerEnabled;
        internal static ConfigEntry<int> containerHeight;
        internal static ConfigEntry<int> containerWidth;
        internal static ConfigEntry<string> containerLvl1Station;
        internal static ConfigEntry<int> containerLvl1StationLvl;
        internal static ConfigEntry<int> containerLvl1StationRange;
        internal static ConfigEntry<string> containerLvl1UpgradeRecipe;
        internal static ConfigEntry<string> containerLvl2Station;
        internal static ConfigEntry<int> containerLvl2StationLvl;
        internal static ConfigEntry<int> containerLvl2StationRange;
        internal static ConfigEntry<string> containerLvl2UpgradeRecipe;

        internal static ConfigEntry<bool> healthEnabled;
        internal static ConfigEntry<int> healthUpgradeLvl1;
        internal static ConfigEntry<int> healthUpgradeLvl2;
        internal static ConfigEntry<bool> ashlandsProtection;
        internal static ConfigEntry<string> healthLvl1Station;
        internal static ConfigEntry<int> healthLvl1StationLvl;
        internal static ConfigEntry<int> healthLvl1StationRange;
        internal static ConfigEntry<string> healthUpgradeRecipe;
        internal static ConfigEntry<string> healthLvl2Station;
        internal static ConfigEntry<int> healthLvl2StationLvl;
        internal static ConfigEntry<int> healthLvl2StationRange;
        internal static ConfigEntry<string> ashlandsUpgradeRecipe;

        internal static ConfigEntry<bool> lanternEnabled;
        internal static ConfigEntry<bool> lanternRemovable;
        internal static ConfigEntry<bool> lanternAutoSwtich;
        internal static ConfigEntry<bool> lanternSwitchable;
        internal static ConfigEntry<Color> lanternLightColor;
        internal static ConfigEntry<string> lanternStation;
        internal static ConfigEntry<int> lanternStationLvl;
        internal static ConfigEntry<int> lanternStationRange;
        internal static ConfigEntry<string> lanternUpgradeRecipe;

        internal static ConfigEntry<bool> turretsEnabled;
        internal static ConfigEntry<string> turretsStation;
        internal static ConfigEntry<int> turretsStationLvl;
        internal static ConfigEntry<int> turretsStationRange;
        internal static ConfigEntry<string> turretsUpgradeRecipe;

        internal static ConfigEntry<bool> itemStandEnabled;
        internal static ConfigEntry<bool> itemStandDisableSpeaking;
        internal static ConfigEntry<string> itemStandTrophyRescale;

        internal static ConfigEntry<bool> mastEnabled;
        internal static ConfigEntry<bool> mastRemovable;
        internal static ConfigEntry<string> mastStation;
        internal static ConfigEntry<int> mastStationLvl;
        internal static ConfigEntry<int> mastStationRange;
        internal static ConfigEntry<string> mastUpgradeRecipe;

        internal static ConfigEntry<bool> tentEnabled;
        internal static ConfigEntry<bool> tentHeat;
        internal static ConfigEntry<bool> tentRemovable;
        internal static ConfigEntry<string> tentStation;
        internal static ConfigEntry<int> tentStationLvl;
        internal static ConfigEntry<int> tentStationRange;
        internal static ConfigEntry<string> tentUpgradeRecipe;

        internal static ConfigEntry<bool> changeHead;
        internal static ConfigEntry<bool> changeShields;
        internal static ConfigEntry<bool> changeTent;
        internal static ConfigEntry<bool> changeSail;
        internal static ConfigEntry<int> maxShields;
        internal static ConfigEntry<int> maxTents;
        internal static ConfigEntry<int> maxSails;

        public static string configDirectory;
        public const string tentsDirectory = "tents";
        public const string sailsDirectory = "sails";
        public const string shieldsDirectory = "shields";

        private void Awake()
        {
            harmony.PatchAll();

            instance = this;

            ConfigInit();
            _ = configSync.AddLockingConfigEntry(configLocked);

            configDirectory = Path.Combine(Paths.ConfigPath, pluginID);

            Game.isModded = true;

            LoadTextures();

            LocalizationManager.Localizer.Load();
        }

        public void ConfigInit()
        {
            config("General", "NexusID", 2885, "Nexus mod ID for updates", false);

            configLocked = config("General", "Lock Configuration", defaultValue: true, "Configuration is locked and can be changed by server admins only.");
            loggingEnabled = config("General", "Logging enabled", defaultValue: false, "Enable logging. [Not Synced with Server]", false);
            
            hintStationColor = config("Hint", "Station color", defaultValue: new Color(0.75f, 1f, 0.75f, 1f), "Color of hint in upgrade tooltip. [Not Synced with Server]", false);
            hintColor = config("Hint", "Hint color", defaultValue: new Color(0.678f, 0.847f, 0.902f, 1f), "Color of hint in upgrade tooltip. [Not Synced with Server]", false);
            hintAmountColor = config("Hint", "Entry amount color", defaultValue: Color.yellow, "Color for amount. [Not Synced with Server]", false);
            hintItemColor = config("Hint", "Entry item name color", defaultValue: new Color(0.85f, 0.85f, 0.85f, 1f), "Color for item name. [Not Synced with Server]", false);

            containerEnabled = config("Container", "Enable upgrades", defaultValue: true, "Container upgrades. Pls be aware items in upgraded slots will be unavailable after mod disabling. But it will drop on ship destruction.");
            containerWidth = config("Container", "Upgrade - Lvl 1 - Container Width", defaultValue: 7, "Width of ship container after first upgrade.");
            containerLvl1Station = config("Container", "Upgrade - Lvl 1 - Station name", defaultValue: "$piece_workbench", "Station name token. The center of the ship is the starting point of the check.");
            containerLvl1StationLvl = config("Container", "Upgrade - Lvl 1 - Station level", defaultValue: 4, "Station level. At least one station in the range must meet the level requirement.");
            containerLvl1StationRange = config("Container", "Upgrade - Lvl 1 - Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");
            containerLvl1UpgradeRecipe = config("Container", "Upgrade - Lvl 1 - Recipe", defaultValue: "Silver:10", "Container lvl 1 upgrade recipe. World restart or ship rebuild required to apply changes.");
            containerHeight = config("Container", "Upgrade - Lvl 2 - Container Height", defaultValue: 4, "Height of ship container after second upgrade.");
            containerLvl2Station = config("Container", "Upgrade - Lvl 2 - Station name", defaultValue: "$piece_artisanstation", "Station name token. The center of the ship is the starting point of the check.");
            containerLvl2StationLvl = config("Container", "Upgrade - Lvl 2 - Station level", defaultValue: 1, "Station level. At least one station in the range must meet the level requirement.");
            containerLvl2StationRange = config("Container", "Upgrade - Lvl 2 - Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");
            containerLvl2UpgradeRecipe = config("Container", "Upgrade - Lvl 2 - Recipe", defaultValue: "BlackMetal:10", "Container lvl 2 upgrade recipe. World restart or ship rebuild required to apply changes.");

            healthEnabled = config("Hull", "Enable upgrades", defaultValue: true, "Health upgrades.");
            healthUpgradeLvl1 = config("Hull", "Upgrade - Lvl 1 - Health", defaultValue: 1500, "Health of ship hull after first upgrade.");
            healthLvl1Station = config("Hull", "Upgrade - Lvl 1 - Station name", defaultValue: "$piece_forge", "Station name token. The center of the ship is the starting point of the check.");
            healthLvl1StationLvl = config("Hull", "Upgrade - Lvl 1 - Station level", defaultValue: 7, "Station level. At least one station in the range must meet the level requirement.");
            healthLvl1StationRange = config("Hull", "Upgrade - Lvl 1 - Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");
            healthUpgradeRecipe = config("Hull", "Upgrade - Lvl 1 - Recipe", defaultValue: "SerpentScale:20", "Hull lvl 1 upgrade recipe. World restart or ship rebuild required to apply changes.");
            healthUpgradeLvl2 = config("Hull", "Upgrade - Lvl 2 - Health", defaultValue: 2000, "Health of ship hull after second upgrade. Set to 0 to disable upgrade.");
            ashlandsProtection = config("Hull", "Upgrade - Lvl 2 - Ashlands protection", defaultValue: true, "Should ship be protected from ashlands ocean after second upgrade. If disabled - second upgrade will not be available.");
            healthLvl2Station = config("Hull", "Upgrade - Lvl 2 - Station name", defaultValue: "$piece_blackforge", "Station name token. The center of the ship is the starting point of the check.");
            healthLvl2StationLvl = config("Hull", "Upgrade - Lvl 2 - Station level", defaultValue: 3, "Station level. At least one station in the range must meet the level requirement.");
            healthLvl2StationRange = config("Hull", "Upgrade - Lvl 2 - Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");
            ashlandsUpgradeRecipe = config("Hull", "Upgrade - Lvl 2 - Recipe", defaultValue: "CeramicPlate:20,Tar:30,YggdrasilWood:20,IronNails:40", "Hull lvl 2 upgrade recipe. World restart or ship rebuild required to apply changes.");

            lanternEnabled = config("Lantern", "Enable upgrades", defaultValue: true, "Lantern upgrades requires mast to be upgraded.");
            lanternRemovable = config("Lantern", "Make removable", defaultValue: true, "Make lantern removable. World restart or ship rebuild required to apply changes.");
            lanternAutoSwtich = config("Lantern", "Light auto enabled and disabled", defaultValue: true, "Light will be automatically enabled in night time or dark environments and automatically disabled in day light");
            lanternSwitchable = config("Lantern", "Light switch enabled", defaultValue: true, "Enable manual light switch. World restart or ship rebuild required to apply changes.");
            lanternLightColor = config("Lantern", "Light color", defaultValue: new Color(0.96f, 0.78f, 0.68f, 1f), "Color of lantern light. Switch light to apply changes.");
            lanternUpgradeRecipe = config("Lantern", "Recipe", defaultValue: "SurtlingCore:3,BronzeNails:10,FineWood:4,Chain:1", "Lantern upgrade recipe. World restart or ship rebuild required to apply changes.");
            lanternStation = config("Lantern", "Station name", defaultValue: "$piece_forge", "Station name token. The center of the ship is the starting point of the check.");
            lanternStationLvl = config("Lantern", "Station level", defaultValue: 4, "Station level. At least one station in the range must meet the level requirement.");
            lanternStationRange = config("Lantern", "Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");

            mastEnabled = config("Mast", "Enable upgrades", defaultValue: true, "Mast upgrade makes lantern and tent upgrades possible.");
            mastRemovable = config("Mast", "Make removable", defaultValue: true, "Enable mast removal. World restart or ship rebuild required to apply changes.");
            mastUpgradeRecipe = config("Mast", "Recipe", defaultValue: "Wood:10", "Mast upgrade recipe. World restart or ship rebuild required to apply changes.");
            mastStation = config("Mast", "Station name", defaultValue: "$piece_workbench", "Station name token. The center of the ship is the starting point of the check.");
            mastStationLvl = config("Mast", "Station level", defaultValue: 4, "Station level. At least one station in the range must meet the level requirement.");
            mastStationRange = config("Mast", "Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");

            tentEnabled = config("Tent", "Enable upgrades", defaultValue: true, "Tent upgrades requires mast to be upgraded.");
            tentHeat = config("Tent", "Heat enabled", defaultValue: true, "Enable heat zone under the tent to get place to rest. Enabled lantern required.");
            tentRemovable = config("Tent", "Make removable", defaultValue: true, "Enable tent removal. World restart or ship rebuild required to apply changes.");
            tentUpgradeRecipe = config("Tent", "Recipe", defaultValue: "JuteRed:2", "Tent upgrade recipe. World restart or ship rebuild required to apply changes.");
            tentStation = config("Tent", "Station name", defaultValue: "$piece_workbench", "Station name token. The center of the ship is the starting point of the check.");
            tentStationLvl = config("Tent", "Station level", defaultValue: 4, "Station level. At least one station in the range must meet the level requirement.");
            tentStationRange = config("Tent", "Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");

            turretsEnabled = config("Turrets", "Enable upgrades", defaultValue: true, "Enable turrets upgrades.");
            turretsUpgradeRecipe = config("Turrets", "Recipe", defaultValue: "BlackMetal:15,YggdrasilWood:15,MechanicalSpring:5", "Turrets upgrade recipe. World restart or ship rebuild required to apply changes.");
            turretsStation = config("Turrets", "Station name", defaultValue: "$piece_artisanstation", "Station name token. The center of the ship is the starting point of the check.");
            turretsStationLvl = config("Turrets", "Station level", defaultValue: 1, "Station level. At least one station in the range must meet the level requirement.");
            turretsStationRange = config("Turrets", "Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");

            itemStandEnabled = config("Item stand", "Enabled", defaultValue: true, "Enable item stand on bow for trophy. Boss trophies brings an option to cast another Forsaken power while on ship.");
            itemStandDisableSpeaking = config("Item stand", "Trophy speaking disabled", defaultValue: false, "Trophis will not do random speak. World restart or ship rebuild required to apply changes.");
            itemStandTrophyRescale = config("Item stand", "Trophy rescale", defaultValue: "TrophyBonemass:0,7;TrophyBonemawSerpent:0,7;TrophySeekerQueen:0,7;TrophyGoblinKing:0,7", "Some trophies are ginormous. Set smaller scale for them. Trophy rehook required to apply changes.");

            changeHead = config("Style", "Change heads", defaultValue: true, "Change ship's head style.");
            changeShields = config("Style", "Change shields color", defaultValue: true, "Change shields colors. World restart or ship rebuild required to apply changes.");
            changeTent = config("Style", "Change tent color", defaultValue: true, "Change tent colors. World restart or ship rebuild required to apply changes.");
            changeSail = config("Style", "Change sail color", defaultValue: true, "Change sail colors. World restart or ship rebuild required to apply changes.");
            maxShields = config("Style", "Max shields amount", defaultValue: 0, "Maximum amount of shield variants. If 0 - amount is taken from custom textures count."+
                                                                            "\n By default every custom texture counts as 3 shields. World restart or ship rebuild required to apply changes.");
            maxTents = config("Style", "Max tents amount", defaultValue: 0, "Maximum amount of tent variants. If 0 - amount is taken from custom textures count. World restart or ship rebuild required to apply changes.");
            maxSails = config("Style", "Max sails amount", defaultValue: 0, "Maximum amount of sail variants. If 0 - amount is taken from custom textures count. ");
        }

        private void OnDestroy()
        {
            Config.Save();
            instance = null;
            harmony?.UnpatchSelf();
        }

        public static void LogInfo(object data)
        {
            if (loggingEnabled.Value)
                instance.Logger.LogInfo(data);
        }

        public static void LogWarning(object data)
        {
            instance.Logger.LogWarning(data);
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, defaultValue, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, string description, bool synchronizedSetting = true) => config(group, name, defaultValue, new ConfigDescription(description), synchronizedSetting);

        private void LoadTextures()
        {
            LoadTexture("ashlands_hull.png", ref LongshipCustomizableParts.s_ashlandsHull);

            Directory.CreateDirectory(configDirectory);

            string sailOriginal = Path.Combine(configDirectory, "sail_original.png");
            string tentOriginal = Path.Combine(configDirectory, "tent_original.png");
            string shieldsOriginal = Path.Combine(configDirectory, "shields_original.png");
            if (!File.Exists(sailOriginal) || !File.Exists(tentOriginal) || !File.Exists(shieldsOriginal))
            {
                File.WriteAllBytes(sailOriginal, GetEmbeddedFileData("sail_original.png"));
                File.WriteAllBytes(tentOriginal, GetEmbeddedFileData("tent_original.png"));
                File.WriteAllBytes(shieldsOriginal, GetEmbeddedFileData("shields_original.png"));
            }

            string tents = Path.Combine(configDirectory, tentsDirectory);
            if (!Directory.Exists(tents))
            {
                Directory.CreateDirectory(tents);

                File.WriteAllBytes(Path.Combine(tents, "tent_01.png"), GetEmbeddedFileData("tent_01.png"));
                File.WriteAllBytes(Path.Combine(tents, "tent_02.png"), GetEmbeddedFileData("tent_02.png"));
            }

            string sails = Path.Combine(configDirectory, sailsDirectory);
            if (!Directory.Exists(sails))
            {
                Directory.CreateDirectory(sails);

                File.WriteAllBytes(Path.Combine(sails, "sail_01.png"), GetEmbeddedFileData("sail_01.png"));
                File.WriteAllBytes(Path.Combine(sails, "sail_02.png"), GetEmbeddedFileData("sail_02.png"));
            }

            string shields = Path.Combine(configDirectory, shieldsDirectory);
            Directory.CreateDirectory(shields);

            foreach (FileInfo tent in new DirectoryInfo(tents).EnumerateFiles().OrderBy(file => file.Name))
                LongshipCustomizableParts.AddCustomTent(Path.Combine(tentsDirectory, tent.Name));

            foreach (FileInfo sail in new DirectoryInfo(sails).EnumerateFiles().OrderBy(file => file.Name))
                LongshipCustomizableParts.AddCustomSail(Path.Combine(sailsDirectory, sail.Name));

            foreach (FileInfo shield in new DirectoryInfo(shields).EnumerateFiles().OrderBy(file => file.Name))
                LongshipCustomizableParts.AddCustomShields(Path.Combine(shieldsDirectory, shield.Name));
        }

        internal static void LoadIcon(string filename, ref Sprite icon)
        {
            Texture2D tex = new Texture2D(2, 2);
            if (LoadTexture(filename, ref tex))
                icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }

        internal static bool LoadTextureFromConfigDirectory(string filename, ref Texture2D tex)
        {
            string fileInConfigFolder = Path.Combine(configDirectory, filename);
            if (!File.Exists(fileInConfigFolder))
                return false;

            LogInfo($"Loaded image from config folder: {filename}");
            return tex.LoadImage(File.ReadAllBytes(fileInConfigFolder));
        }

        internal static bool LoadTexture(string filename, ref Texture2D tex)
        {
            if (LoadTextureFromConfigDirectory(filename, ref tex))
                return true;

            tex.name = Path.GetFileNameWithoutExtension(filename);
            return tex.LoadImage(GetEmbeddedFileData(filename), true);
        }

        internal static byte[] GetEmbeddedFileData(string filename)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            string name = executingAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));

            Stream resourceStream = executingAssembly.GetManifestResourceStream(name);

            byte[] data = new byte[resourceStream.Length];
            resourceStream.Read(data, 0, data.Length);

            return data;
        }

        internal static Piece.Requirement[] ParseRequirements(string recipe)
        {
            List<Piece.Requirement> requirements = new List<Piece.Requirement>();
            foreach (string requirement in recipe.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] req = requirement.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (req.Length != 2)
                    continue;

                int amount = int.Parse(req[1]);
                if (amount <= 0)
                    continue;

                var prefab = ObjectDB.instance.GetItemPrefab(req[0].Trim());
                if (prefab == null)
                    continue;

                requirements.Add(new Piece.Requirement()
                {
                    m_amount = amount,
                    m_resItem = prefab.GetComponent<ItemDrop>(),
                    m_recover = true
                });
            };

            return requirements.ToArray();
        }

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
        public static class ZoneSystem_Start_FixCustomizableParts
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                LongshipCustomizableParts.OnGlobalStart();
            }
        }

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.OnDestroy))]
        public static class ZoneSystem_OnDestroy_ClearValues
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                LongshipCustomizableParts.OnGlobalDestroy();
            }
        }

        [HarmonyPatch(typeof(Ship))]
        public static class Ship_OnTrigger_IgnoreFireWarmthCollider
        {
            private static readonly Dictionary<Ship, Dictionary<Collider, int>> triggerCounter = new Dictionary<Ship, Dictionary<Collider, int>>();
            
            [HarmonyPostfix]
            [HarmonyPatch(nameof(Ship.OnEnable))]
            public static void OnEnablePostfix(Ship __instance) => triggerCounter.Add(__instance, new Dictionary<Collider, int>());

            [HarmonyPostfix]
            [HarmonyPatch(nameof(Ship.OnDisable))]
            public static void OnDisablePostfix(Ship __instance) => triggerCounter.Remove(__instance);

            [HarmonyPrefix]
            [HarmonyPatch(nameof(Ship.OnTriggerEnter))]
            public static bool PrefixEnter(Ship __instance, Collider collider)
            {
                if (!collider.GetComponent<Character>())
                    return true;

                if (!triggerCounter.TryGetValue(__instance, out Dictionary<Collider, int> colliderTriggered))
                    return true;

                if (!colliderTriggered.ContainsKey(collider))
                {
                    colliderTriggered[collider] = 1;
                    return true;
                }

                colliderTriggered[collider]++;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(Ship.OnTriggerExit))]
            public static bool PrefixExit(Ship __instance, Collider collider)
            {
                if (!collider.GetComponent<Character>())
                    return true;

                if (!triggerCounter.TryGetValue(__instance, out Dictionary<Collider, int> colliderTriggered))
                    return true;

                if (!colliderTriggered.TryGetValue(collider, out int counter))
                    return true;

                if (counter == 1)
                {
                    colliderTriggered.Remove(collider);
                    return true;
                }

                colliderTriggered[collider]--;
                return false;
            }
        }

        private static bool IsControlledComponent(Component component)
        {
            return Utils.GetPrefabName(component.transform.root.gameObject) == LongshipCustomizableParts.prefabName;
        }

        [HarmonyPatch(typeof(Ship), nameof(Ship.UpdateControlls))]
        public static class Ship_UpdateControlls_SpeedLimitWithoutMast
        {
            public static void Prefix(Ship __instance)
            {
                if (!IsControlledComponent(__instance))
                    return;

                if (__instance.m_nview && __instance.m_nview.IsValid() && (int)__instance.m_speed > 2 && __instance.m_nview.GetZDO().GetBool(LongshipCustomizableParts.s_mastRemoved))
                    __instance.m_speed = Ship.Speed.Slow;
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        public static class Container_Awake_CargoUpgrades
        {
            private static bool CheckAccess(Container container)
            {
                return (!container.m_checkGuardStone || PrivateArea.CheckAccess(container.transform.position, 0f, flash: false)) && container.CheckAccess(Game.instance.GetPlayerProfile().GetPlayerID());
            }

            public static void Prefix(Container __instance)
            {
                if (!containerEnabled.Value)
                    return;

                if (!IsControlledComponent(__instance))
                    return;

                ZNetView m_nview = (__instance.m_rootObjectOverride ? __instance.m_rootObjectOverride.GetComponent<ZNetView>() : __instance.GetComponent<ZNetView>());
                if (m_nview == null || !m_nview.IsValid())
                    return;

                if (m_nview.GetZDO().GetBool(LongshipCustomizableParts.s_containerUpgradedLvl1) && __instance.m_width < containerWidth.Value)
                    __instance.m_width = containerWidth.Value;

                if (m_nview.GetZDO().GetBool(LongshipCustomizableParts.s_containerUpgradedLvl2) && __instance.m_height < containerHeight.Value)
                    __instance.m_height = containerHeight.Value;
            }
        }

        private static bool IsBoxLadder(Ladder ladder)
        {
            return ladder.m_name == "$lu_box_name";
        }

        [HarmonyPatch(typeof(Ladder), nameof(Ladder.GetHoverText))]
        public static class Ladder_GetHoverText_BoxClimb
        {
            public static bool Prefix(Ladder __instance, ref string __result)
            {
                if (!IsBoxLadder(__instance))
                    return true;

                if (__instance.InUseDistance(Player.m_localPlayer))
                    __result = Localization.instance.Localize(__instance.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $lu_box_climb");
                else
                    __result = "";
                
                return false;
            }
        }

        [HarmonyPatch(typeof(Ladder), nameof(Ladder.Interact))]
        public static class Ladder_Interact_BoxClimb
        {
            public static void Postfix(Ladder __instance, Humanoid character, bool hold)
            {
                if (!IsBoxLadder(__instance))
                    return;

                if (!hold && __instance.InUseDistance(character))
                {
                    character.SetMoveDir(__instance.m_targetPos.forward);
                    character.UpdateWalking(Time.deltaTime);
                }
            }
        }
    }
}
