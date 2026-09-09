using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ConditionalConfigSync;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LongshipUpgrades
{
    [BepInPlugin(pluginID, pluginName, pluginVersion)]
    [BepInDependency("_shudnal.ConditionalConfigSync", "1.0.5")]
    public class LongshipUpgrades : BaseUnityPlugin
    {
        public const string pluginID = "shudnal.LongshipUpgrades";
        public const string pluginName = "Longship Upgrades";
        public const string pluginVersion = "1.0.18";

        private readonly Harmony harmony = new Harmony(pluginID);

        internal static readonly ConfigSync configSync = new ConfigSync(pluginID)
        {
            DisplayName = pluginName,
            CurrentVersion = pluginVersion,
            MinimumRequiredVersion = pluginVersion,
            ModRequired = true
        };

        internal static LongshipUpgrades instance;

        internal static ConfigEntry<bool> configLocked;
        internal static ConfigEntry<bool> loggingEnabled;
        internal static ConfigEntry<bool> onlyCreatorUpgrades;
        internal static ConfigEntry<bool> onlyCreatorStand;

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
        internal static ConfigEntry<bool> itemStandForsakenPower;

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

        internal static ConfigEntry<bool> wispEnabled;
        internal static ConfigEntry<string> wispStation;
        internal static ConfigEntry<int> wispStationLvl;
        internal static ConfigEntry<int> wispStationRange;
        internal static ConfigEntry<string> wispUpgradeRecipe;

        internal static ConfigEntry<bool> mapTableEnabled;
        internal static ConfigEntry<string> mapTableStation;
        internal static ConfigEntry<int> mapTableStationLvl;
        internal static ConfigEntry<int> mapTableStationRange;
        internal static ConfigEntry<string> mapTableUpgradeRecipe;

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

            StartCoroutine(LocalizationManager.Localizer.Load());
        }

        public void ConfigInit()
        {

            configLocked = serverConfig("General", "Lock Configuration", defaultValue: true, "Configuration is locked and can be changed by server admins only.");
            loggingEnabled = config("General", "Logging enabled", defaultValue: false, "Enable logging.", false);
            onlyCreatorUpgrades = serverConfig("General", "Only creator can upgrade ship", defaultValue: true, "Only ship's creator can upgrade it.");
            onlyCreatorStand = serverConfig("General", "Only creator can change trophy", defaultValue: true, "Only ship's creator can put and get trophy from stand.");

            hintStationColor = config("Hint", "Station color", defaultValue: new Color(0.75f, 1f, 0.75f, 1f), "Color of hint in upgrade tooltip.", false);
            hintColor = config("Hint", "Hint color", defaultValue: new Color(0.678f, 0.847f, 0.902f, 1f), "Color of hint in upgrade tooltip.", false);
            hintAmountColor = config("Hint", "Entry amount color", defaultValue: Color.yellow, "Color for amount.", false);
            hintItemColor = config("Hint", "Entry item name color", defaultValue: new Color(0.85f, 0.85f, 0.85f, 1f), "Color for item name.", false);

            containerEnabled = serverConfig("Container", "Enable upgrades", defaultValue: true, "Container upgrades. Pls be aware items in upgraded slots will be unavailable after mod disabling. But it will drop on ship destruction.");
            containerWidth = serverConfig("Container", "Upgrade - Lvl 1 - Container Width", defaultValue: 7, "Width of ship container after first upgrade.");
            containerLvl1Station = serverConfig("Container", "Upgrade - Lvl 1 - Station name", defaultValue: "$piece_workbench", "Station localization token or prefab name. Matching is case-insensitive. The center of the ship is the starting point of the check.");
            containerLvl1StationLvl = serverConfig("Container", "Upgrade - Lvl 1 - Station level", defaultValue: 4, "Station level. At least one station in the range must meet the level requirement.");
            containerLvl1StationRange = serverConfig("Container", "Upgrade - Lvl 1 - Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");
            containerLvl1UpgradeRecipe = serverConfig("Container", "Upgrade - Lvl 1 - Recipe", defaultValue: "ElderBark:20,Silver:5,Obsidian:10", "Container lvl 1 upgrade recipe. Item identifiers accept prefab names or localization tokens without case sensitivity. World restart or ship rebuild required to apply changes.");
            containerHeight = serverConfig("Container", "Upgrade - Lvl 2 - Container Height", defaultValue: 4, "Height of ship container after second upgrade.");
            containerLvl2Station = serverConfig("Container", "Upgrade - Lvl 2 - Station name", defaultValue: "$piece_artisanstation", "Station localization token or prefab name. Matching is case-insensitive. The center of the ship is the starting point of the check.");
            containerLvl2StationLvl = serverConfig("Container", "Upgrade - Lvl 2 - Station level", defaultValue: 1, "Station level. At least one station in the range must meet the level requirement.");
            containerLvl2StationRange = serverConfig("Container", "Upgrade - Lvl 2 - Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");
            containerLvl2UpgradeRecipe = serverConfig("Container", "Upgrade - Lvl 2 - Recipe", defaultValue: "BlackMetal:20,YggdrasilWood:20,FineWood:20", "Container lvl 2 upgrade recipe. Item identifiers accept prefab names or localization tokens without case sensitivity. World restart or ship rebuild required to apply changes.");

            healthEnabled = serverConfig("Hull", "Enable upgrades", defaultValue: true, "Health upgrades.");
            healthUpgradeLvl1 = serverConfig("Hull", "Upgrade - Lvl 1 - Health", defaultValue: 1500, "Health of ship hull after first upgrade.");
            healthLvl1Station = serverConfig("Hull", "Upgrade - Lvl 1 - Station name", defaultValue: "$piece_forge", "Station localization token or prefab name. Matching is case-insensitive. The center of the ship is the starting point of the check.");
            healthLvl1StationLvl = serverConfig("Hull", "Upgrade - Lvl 1 - Station level", defaultValue: 7, "Station level. At least one station in the range must meet the level requirement.");
            healthLvl1StationRange = serverConfig("Hull", "Upgrade - Lvl 1 - Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");
            healthUpgradeRecipe = serverConfig("Hull", "Upgrade - Lvl 1 - Recipe", defaultValue: "FineWood:20,BlackMetal:10,Raspberry:2,Blueberries:2,Coal:2", "Hull lvl 1 upgrade recipe. Item identifiers accept prefab names or localization tokens without case sensitivity. World restart or ship rebuild required to apply changes.");
            healthUpgradeLvl2 = serverConfig("Hull", "Upgrade - Lvl 2 - Health", defaultValue: 2000, "Health of ship hull after second upgrade. Set to 0 to disable upgrade.");
            ashlandsProtection = serverConfig("Hull", "Upgrade - Lvl 2 - Ashlands protection", defaultValue: true, "Should ship be protected from ashlands ocean after second upgrade. If disabled - second upgrade will not be available.");
            healthLvl2Station = serverConfig("Hull", "Upgrade - Lvl 2 - Station name", defaultValue: "$piece_blackforge", "Station localization token or prefab name. Matching is case-insensitive. The center of the ship is the starting point of the check.");
            healthLvl2StationLvl = serverConfig("Hull", "Upgrade - Lvl 2 - Station level", defaultValue: 3, "Station level. At least one station in the range must meet the level requirement.");
            healthLvl2StationRange = serverConfig("Hull", "Upgrade - Lvl 2 - Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");
            ashlandsUpgradeRecipe = serverConfig("Hull", "Upgrade - Lvl 2 - Recipe", defaultValue: "CeramicPlate:20,Tar:20,YggdrasilWood:20,IronNails:50", "Hull lvl 2 upgrade recipe. Item identifiers accept prefab names or localization tokens without case sensitivity. World restart or ship rebuild required to apply changes.");

            lanternEnabled = serverConfig("Lantern", "Enable upgrades", defaultValue: true, "Lantern upgrades requires mast to be upgraded.");
            lanternRemovable = serverConfig("Lantern", "Make removable", defaultValue: true, "Make lantern removable. World restart or ship rebuild required to apply changes.");
            lanternAutoSwtich = serverConfig("Lantern", "Light auto enabled and disabled", defaultValue: true, "Light will be automatically enabled in night time or dark environments and automatically disabled in day light");
            lanternSwitchable = serverConfig("Lantern", "Light switch enabled", defaultValue: true, "Enable manual light switch. World restart or ship rebuild required to apply changes.");
            lanternLightColor = config("Lantern", "Light color", defaultValue: new Color(0.96f, 0.78f, 0.68f, 1f), "Color of lantern light. Switch light to apply changes.");
            lanternUpgradeRecipe = serverConfig("Lantern", "Recipe", defaultValue: "SurtlingCore:3,BronzeNails:10,FineWood:6,Chain:1", "Lantern upgrade recipe. Item identifiers accept prefab names or localization tokens without case sensitivity. World restart or ship rebuild required to apply changes.");
            lanternStation = serverConfig("Lantern", "Station name", defaultValue: "$piece_forge", "Station localization token or prefab name. Matching is case-insensitive. The center of the ship is the starting point of the check.");
            lanternStationLvl = serverConfig("Lantern", "Station level", defaultValue: 4, "Station level. At least one station in the range must meet the level requirement.");
            lanternStationRange = serverConfig("Lantern", "Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");

            mastEnabled = serverConfig("Mast", "Enable upgrades", defaultValue: true, "Mast upgrade also makes lantern, tent and wisp torch upgrades possible.");
            mastRemovable = serverConfig("Mast", "Make removable", defaultValue: true, "Enable mast removal. World restart or ship rebuild required to apply changes.");
            mastUpgradeRecipe = serverConfig("Mast", "Recipe", defaultValue: "RoundLog:2,IronNails:10,FineWood:4,Raspberry:2,Blueberries:2,Coal:2", "Mast upgrade recipe. Item identifiers accept prefab names or localization tokens without case sensitivity. World restart or ship rebuild required to apply changes.");
            mastStation = serverConfig("Mast", "Station name", defaultValue: "$piece_workbench", "Station localization token or prefab name. Matching is case-insensitive. The center of the ship is the starting point of the check.");
            mastStationLvl = serverConfig("Mast", "Station level", defaultValue: 4, "Station level. At least one station in the range must meet the level requirement.");
            mastStationRange = serverConfig("Mast", "Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");

            tentEnabled = serverConfig("Tent", "Enable upgrades", defaultValue: true, "Tent upgrades requires mast to be upgraded.");
            tentHeat = serverConfig("Tent", "Heat enabled", defaultValue: true, "Enable heat zone under the tent to get place to rest. Enabled lantern required.");
            tentRemovable = serverConfig("Tent", "Make removable", defaultValue: true, "Enable tent removal. World restart or ship rebuild required to apply changes.");
            tentUpgradeRecipe = serverConfig("Tent", "Recipe", defaultValue: "JuteRed:2,LoxPelt:2,LinenThread:6,RoundLog:2", "Tent upgrade recipe. Item identifiers accept prefab names or localization tokens without case sensitivity. World restart or ship rebuild required to apply changes.");
            tentStation = serverConfig("Tent", "Station name", defaultValue: "$piece_workbench", "Station localization token or prefab name. Matching is case-insensitive. The center of the ship is the starting point of the check.");
            tentStationLvl = serverConfig("Tent", "Station level", defaultValue: 4, "Station level. At least one station in the range must meet the level requirement.");
            tentStationRange = serverConfig("Tent", "Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");
            
            wispEnabled = serverConfig("Wisp torch", "Enable upgrades", defaultValue: true, "Wisp torch pushes away the surrounding magic mist.");
            wispUpgradeRecipe = serverConfig("Wisp torch", "Recipe", defaultValue: "YggdrasilWood:10,Wisp:5,Eitr:5", "Wisp Torch upgrade recipe. Item identifiers accept prefab names or localization tokens without case sensitivity. World restart or ship rebuild required to apply changes.");
            wispStation = serverConfig("Wisp torch", "Station name", defaultValue: "$piece_magetable", "Station localization token or prefab name. Matching is case-insensitive. The center of the ship is the starting point of the check.");
            wispStationLvl = serverConfig("Wisp torch", "Station level", defaultValue: 1, "Station level. At least one station in the range must meet the level requirement.");
            wispStationRange = serverConfig("Wisp torch", "Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");

            turretsEnabled = serverConfig("Turrets", "Enable upgrades", defaultValue: true, "Enable turrets upgrades.");
            turretsUpgradeRecipe = serverConfig("Turrets", "Recipe", defaultValue: "BlackMetal:15,YggdrasilWood:15,MechanicalSpring:5", "Turrets upgrade recipe. Item identifiers accept prefab names or localization tokens without case sensitivity. World restart or ship rebuild required to apply changes.");
            turretsStation = serverConfig("Turrets", "Station name", defaultValue: "$piece_artisanstation", "Station localization token or prefab name. Matching is case-insensitive. The center of the ship is the starting point of the check.");
            turretsStationLvl = serverConfig("Turrets", "Station level", defaultValue: 1, "Station level. At least one station in the range must meet the level requirement.");
            turretsStationRange = serverConfig("Turrets", "Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");

            itemStandEnabled = serverConfig("Item stand", "Enabled", defaultValue: true, "Enable item stand on bow for trophy.");
            itemStandDisableSpeaking = config("Item stand", "Trophy speaking disabled", defaultValue: false, "Trophis will not do random speak. World restart or ship rebuild required to apply changes.");
            itemStandTrophyRescale = config("Item stand", "Trophy rescale", defaultValue: "TrophyBonemass:0,7;TrophyBonemawSerpent:0,7;TrophySeekerQueen:0,7;TrophyGoblinKing:0,7", "Some trophies are ginormous. Set smaller scale for them. Trophy rehook required to apply changes.");
            itemStandForsakenPower = serverConfig("Item stand", "Forsaken power enabled", defaultValue: true, "Boss trophies brings an option to cast another Forsaken power while on ship.");

            mapTableEnabled = serverConfig("Map table", "Enable upgrades", defaultValue: true, "Cartography table allows to exchange map data between players.");
            mapTableUpgradeRecipe = serverConfig("Map table", "Recipe", defaultValue: "FineWood:10,Bronze:2,LeatherScraps:5,Raspberry:4", "Map Table upgrade recipe. Item identifiers accept prefab names or localization tokens without case sensitivity. World restart or ship rebuild required to apply changes.");
            mapTableStation = serverConfig("Map table", "Station name", defaultValue: "$piece_forge", "Station localization token or prefab name. Matching is case-insensitive. The center of the ship is the starting point of the check.");
            mapTableStationLvl = serverConfig("Map table", "Station level", defaultValue: 3, "Station level. At least one station in the range must meet the level requirement.");
            mapTableStationRange = serverConfig("Map table", "Station range", defaultValue: 100, "Station range check. You don't have to park the ship inside your main house to be able to upgrade it.");

            changeHead = serverConfig("Style", "Change heads", defaultValue: true, "Change ship's head style.");
            changeShields = serverConfig("Style", "Change shields color", defaultValue: true, "Change shields colors. World restart or ship rebuild required to apply changes.");
            changeTent = serverConfig("Style", "Change tent color", defaultValue: true, "Change tent colors. World restart or ship rebuild required to apply changes.");
            changeSail = serverConfig("Style", "Change sail color", defaultValue: true, "Change sail colors. World restart or ship rebuild required to apply changes.");
            maxShields = serverConfig("Style", "Max shields amount", defaultValue: 0, "Maximum amount of shield variants. If 0 - amount is taken from custom textures count."+
                                                                            "\n By default every custom texture counts as 3 shields. World restart or ship rebuild required to apply changes.");
            maxTents = serverConfig("Style", "Max tents amount", defaultValue: 0, "Maximum amount of tent variants. If 0 - amount is taken from custom textures count. World restart or ship rebuild required to apply changes.");
            maxSails = serverConfig("Style", "Max sails amount", defaultValue: 0, "Maximum amount of sail variants. If 0 - amount is taken from custom textures count. ");
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

        private ConfigEntry<T> config<T>(string group, string name, T defaultValue, ConfigDescription description, bool synchronizedSetting = true) =>
            configSync.AddConfigEntry(
                Config,
                group,
                name,
                defaultValue,
                description,
                syncMode: ConfigSyncMode.Conditional,
                serverControlledByDefault: synchronizedSetting).SourceConfig;

        private ConfigEntry<T> config<T>(string group, string name, T defaultValue, string description, bool synchronizedSetting = true) =>
            config(group, name, defaultValue, new ConfigDescription(description), synchronizedSetting);

        private ConfigEntry<T> serverConfig<T>(string group, string name, T defaultValue, ConfigDescription description) =>
            configSync.AddConfigEntry(
                Config,
                group,
                name,
                defaultValue,
                description,
                syncMode: ConfigSyncMode.AlwaysServerControlled).SourceConfig;

        private ConfigEntry<T> serverConfig<T>(string group, string name, T defaultValue, string description) =>
            serverConfig(group, name, defaultValue, new ConfigDescription(description));

        private void LoadTextures()
        {
            LoadTexture("ashlands_hull.png", ref LongshipCustomizableParts.s_ashlandsHull);
            LoadTexture("ashlands_hull_damaged.png", ref LongshipCustomizableParts.s_ashlandsHullDamaged);

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
                File.WriteAllBytes(Path.Combine(tents, "tent_03.png"), GetEmbeddedFileData("tent_03.png"));
                File.WriteAllBytes(Path.Combine(tents, "tent_04.png"), GetEmbeddedFileData("tent_04.png"));
                File.WriteAllBytes(Path.Combine(tents, "tent_05.png"), GetEmbeddedFileData("tent_05.png"));
            }

            string sails = Path.Combine(configDirectory, sailsDirectory);
            if (!Directory.Exists(sails))
            {
                Directory.CreateDirectory(sails);

                File.WriteAllBytes(Path.Combine(sails, "sail_01.png"), GetEmbeddedFileData("sail_01.png"));
                File.WriteAllBytes(Path.Combine(sails, "sail_02.png"), GetEmbeddedFileData("sail_02.png"));
                File.WriteAllBytes(Path.Combine(sails, "sail_03.png"), GetEmbeddedFileData("sail_03.png"));
                File.WriteAllBytes(Path.Combine(sails, "sail_04.png"), GetEmbeddedFileData("sail_04.png"));
                File.WriteAllBytes(Path.Combine(sails, "sail_05.png"), GetEmbeddedFileData("sail_05.png"));
            }

            string shields = Path.Combine(configDirectory, shieldsDirectory);
            if (!Directory.Exists(shields))
            {
                Directory.CreateDirectory(shields);

                File.WriteAllBytes(Path.Combine(shields, "shields_01.png"), GetEmbeddedFileData("shields_01.png"));
            }

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
                if (req.Length != 2 || !int.TryParse(req[1].Trim(), out int amount) || amount <= 0)
                    continue;

                string itemIdentifier = req[0].Trim();
                if (!ItemNameTokens.TryGetItemPrefab(itemIdentifier, out GameObject prefab) || !prefab.TryGetComponent(out ItemDrop itemDrop))
                {
                    LogWarning($"Upgrade recipe item '{itemIdentifier}' was not found.");
                    continue;
                }

                requirements.Add(new Piece.Requirement
                {
                    m_amount = amount,
                    m_resItem = itemDrop,
                    m_recover = true
                });
            }

            return requirements.ToArray();
        }

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
        public static class ZoneSystem_Start_FixCustomizableParts
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                ItemNameTokens.UpdateRegisters();
                RegisterShipMapRpcs();
                LongshipCustomizableParts.OnGlobalStart();
            }
        }

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.OnDestroy))]
        public static class ZoneSystem_OnDestroy_ClearValues
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                ClearShipMapRuntime();
                LongshipCustomizableParts.OnGlobalDestroy();
            }
        }

        [HarmonyPatch(typeof(Ship))]
        public static class Ship_OnTrigger_IgnoreFireWarmthCollider
        {
            private static readonly Dictionary<Ship, Dictionary<Collider, int>> triggerCounter = new Dictionary<Ship, Dictionary<Collider, int>>();
            
            private static bool IsHeatActive(Ship ship) => LongshipCustomizableParts.TryGetShipComponent(ship, out LongshipCustomizableParts parts) && parts.IsHeatActive();

            [HarmonyPostfix]
            [HarmonyPatch(nameof(Ship.OnEnable))]
            public static void OnEnablePostfix(Ship __instance) => triggerCounter.Add(__instance, new Dictionary<Collider, int>());

            [HarmonyPostfix]
            [HarmonyPatch(nameof(Ship.OnDisable))]
            public static void OnDisablePostfix(Ship __instance) => triggerCounter.Remove(__instance);

            [HarmonyPrefix]
            [HarmonyPatch(nameof(Ship.OnTriggerEnter))]
            public static bool OnTriggerEnterPrefix(Ship __instance, Collider collider)
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
            public static bool OnTriggerExitPrefix(Ship __instance, Collider collider)
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

        public class ShipMapDataEntry
        {
            public int Revision;
            public byte[] Data;
            public int Hash;
            public float LastAccessTime;
        }

        private const string shipMapRequestRpc = "LU_RequestShipMapData";
        private const string shipMapResponseRpc = "LU_ShipMapDataResponse";
        private const string shipMapSubmitRpc = "LU_SubmitShipMapData";
        private static readonly Dictionary<ZDOID, ShipMapDataEntry> s_shipMapStore = new Dictionary<ZDOID, ShipMapDataEntry>();
        private static readonly HashSet<ZDOID> s_trackedShipZdos = new HashSet<ZDOID>();
        private static readonly List<ZDOID> s_saveInjectedShipMapZdos = new List<ZDOID>();
        private static bool s_shipMapRpcsRegistered = false;
        private static float s_nextClientShipMapPruneTime = 0f;

        private static int GetShipMapHash(byte[] data)
        {
            if (data == null)
                return 0;

            unchecked
            {
                int hash = 17;
                for (int i = 0; i < data.Length; i++)
                    hash = hash * 31 + data[i];
                return hash;
            }
        }

        private static bool IsShipZdo(ZDO zdo) => zdo != null && zdo.GetPrefab() == LongshipCustomizableParts.prefabInt;

        private static bool IsShipZdo(ZDOID id) => ZDOMan.instance != null && IsShipZdo(ZDOMan.instance.GetZDO(id));

        public static bool IsValidMapTable(MapTable mapTable) => mapTable.m_nview && mapTable.m_nview.IsValid();

        internal static void RegisterShipMapRpcs()
        {
            if (s_shipMapRpcsRegistered || ZRoutedRpc.instance == null)
                return;

            ZRoutedRpc.instance.Register<ZPackage>(shipMapRequestRpc, RPC_RequestShipMapData);
            ZRoutedRpc.instance.Register<ZPackage>(shipMapResponseRpc, RPC_ShipMapDataResponse);
            ZRoutedRpc.instance.Register<ZPackage>(shipMapSubmitRpc, RPC_SubmitShipMapData);
            s_shipMapRpcsRegistered = true;
        }

        internal static bool TryGetShipMapData(ZDOID id, out ShipMapDataEntry entry)
        {
            if (s_shipMapStore.TryGetValue(id, out entry) && entry != null && entry.Data != null)
            {
                entry.LastAccessTime = Time.time;
                return true;
            }

            entry = null;
            return false;
        }

        internal static int GetShipMapRevision(ZDOID id)
        {
            return s_shipMapStore.TryGetValue(id, out ShipMapDataEntry entry) ? entry.Revision : 0;
        }

        internal static int SyncShipMapRevision(ZDO zdo, int? preferredRevision = null)
        {
            if (!IsShipZdo(zdo))
                return 0;

            int revision = preferredRevision ?? Math.Max(zdo.GetInt(s_shipMapDataRevision), GetShipMapRevision(zdo.m_uid));
            if (revision <= 0)
                revision = 1;

            if (zdo.GetInt(s_shipMapDataRevision) != revision)
                zdo.Set(s_shipMapDataRevision, revision);

            if (s_shipMapStore.TryGetValue(zdo.m_uid, out ShipMapDataEntry entry))
                entry.Revision = Math.Max(entry.Revision, revision);

            return revision;
        }

        private static bool RemoveShipMapDataFromRuntime(ZDOID id)
        {
            if (!ZDOExtraData.s_byteArrays.TryGetValue(id, out BinarySearchDictionary<int, byte[]> byteArrays))
                return false;

            bool removed = byteArrays.Remove(ZDOVars.s_data);
            if (removed && byteArrays.Count == 0)
                ZDOExtraData.s_byteArrays.Remove(id);

            return removed;
        }

        internal static bool InjectShipMapDataIntoZdo(ZDO zdo)
        {
            if (!IsShipZdo(zdo) || !TryGetShipMapData(zdo.m_uid, out ShipMapDataEntry entry))
                return false;

            ZDOExtraData.Set(zdo.m_uid, ZDOVars.s_data, entry.Data);
            return true;
        }

        internal static bool ExtractShipMapDataFromZdo(ZDO zdo, bool removeFromRuntime = true)
        {
            if (!IsShipZdo(zdo))
                return false;

            byte[] data = zdo.GetByteArray(ZDOVars.s_data);
            if (data == null)
                return false;

            int revision = SyncShipMapRevision(zdo);
            SetShipMapData(zdo.m_uid, revision, data);

            if (removeFromRuntime)
                RemoveShipMapDataFromRuntime(zdo.m_uid);

            return true;
        }

        internal static bool SetShipMapData(ZDOID id, int revision, byte[] data)
        {
            if (data == null || revision <= 0)
                return false;

            byte[] copy = (byte[])data.Clone();
            int hash = GetShipMapHash(copy);
            if (s_shipMapStore.TryGetValue(id, out ShipMapDataEntry existing))
            {
                if (revision < existing.Revision)
                    return false;

                if (revision == existing.Revision && existing.Hash == hash && StructuralComparisons.StructuralEqualityComparer.Equals(existing.Data, copy))
                {
                    existing.LastAccessTime = Time.time;
                    return false;
                }

                existing.Revision = revision;
                existing.Data = copy;
                existing.Hash = hash;
                existing.LastAccessTime = Time.time;
                return true;
            }

            s_shipMapStore[id] = new ShipMapDataEntry()
            {
                Revision = revision,
                Data = copy,
                Hash = hash,
                LastAccessTime = Time.time
            };
            return true;
        }

        internal static void RemoveShipMapData(ZDOID id)
        {
            s_trackedShipZdos.Remove(id);
            s_shipMapStore.Remove(id);
            RemoveShipMapDataFromRuntime(id);
        }

        internal static void ClearShipMapRuntime()
        {
            s_shipMapStore.Clear();
            s_trackedShipZdos.Clear();
            s_saveInjectedShipMapZdos.Clear();
            s_shipMapRpcsRegistered = false;
            s_nextClientShipMapPruneTime = 0f;

        }

        internal static void TrackShipZdo(ZDO zdo, bool extractFromRuntime = true)
        {
            if (!IsShipZdo(zdo))
                return;

            s_trackedShipZdos.Add(zdo.m_uid);

            if (extractFromRuntime)
                ExtractShipMapDataFromZdo(zdo, removeFromRuntime: true);
        }

        internal static void RebuildTrackedShipZdos(IEnumerable<ZDO> zdos)
        {
            s_trackedShipZdos.Clear();
            foreach (ZDO zdo in zdos)
                TrackShipZdo(zdo, extractFromRuntime: true);
        }

        internal static bool RequestShipMapDataFromServer(ZDO zdo, int currentRevision)
        {
            if (zdo == null || !zdo.IsValid() || currentRevision <= 0 || ZRoutedRpc.instance == null)
                return false;

            ZPackage pkg = new ZPackage();
            pkg.Write(zdo.m_uid);
            pkg.Write(currentRevision);
            ZRoutedRpc.instance.InvokeRoutedRPC(shipMapRequestRpc, pkg);
            LogInfo($"Ship map data requested for {zdo}");
            return true;
        }

        private static void SubmitShipMapDataToServer(ZDO zdo, int revision, byte[] data)
        {
            if (zdo == null || revision <= 0 || data == null || ZRoutedRpc.instance == null || (ZNet.instance != null && ZNet.instance.IsServer()))
                return;

            ZPackage pkg = new ZPackage();
            pkg.Write(zdo.m_uid);
            pkg.Write(revision);
            pkg.Write(data);
            ZRoutedRpc.instance.InvokeRoutedRPC(shipMapSubmitRpc, pkg);
            LogInfo($"Ship map data submitted to server for {zdo}");
        }

        private static void InjectAllTrackedShipMapDataIntoRuntime()
        {
            s_saveInjectedShipMapZdos.Clear();
            foreach (ZDOID id in s_shipMapStore.Keys.ToList())
            {
                ZDO zdo = ZDOMan.instance?.GetZDO(id);
                if (zdo != null && InjectShipMapDataIntoZdo(zdo))
                    s_saveInjectedShipMapZdos.Add(id);
            }
        }

        private static void RemoveInjectedShipMapDataFromRuntime()
        {
            foreach (ZDOID id in s_saveInjectedShipMapZdos)
                RemoveShipMapDataFromRuntime(id);
            s_saveInjectedShipMapZdos.Clear();
        }

        private static void PruneClientShipMapStore()
        {
            if (ZNet.instance == null || ZNet.instance.IsServer() || ZDOMan.instance == null)
                return;

            if (Time.time < s_nextClientShipMapPruneTime)
                return;

            s_nextClientShipMapPruneTime = Time.time + 10f;
            foreach (ZDOID id in s_shipMapStore.Keys.ToList())
            {
                if (ZDOMan.instance.GetZDO(id) == null)
                    RemoveShipMapData(id);
            }
        }

        private static void RPC_RequestShipMapData(long sender, ZPackage pkg)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || pkg == null)
                return;

            ZDOID id = pkg.ReadZDOID();
            int requestedRevision = pkg.ReadInt();
            if (!TryGetShipMapData(id, out ShipMapDataEntry entry) || entry.Revision < requestedRevision)
                return;

            ZPackage response = new ZPackage();
            response.Write(id);
            response.Write(entry.Revision);
            response.Write(entry.Data);
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, shipMapResponseRpc, response);
            LogInfo($"Ship map data for {id} sent to {sender}");
        }

        private static void RPC_ShipMapDataResponse(long sender, ZPackage pkg)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer() || pkg == null)
                return;

            ZDOID id = pkg.ReadZDOID();
            int revision = pkg.ReadInt();
            byte[] data = pkg.ReadByteArray();
            if (data == null)
                return;

            if (revision <= GetShipMapRevision(id))
                return;

            SetShipMapData(id, revision, data);
            LogInfo($"Ship map data response for {id} from {sender}");
        }

        private static void RPC_SubmitShipMapData(long sender, ZPackage pkg)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || pkg == null)
                return;

            ZDOID id = pkg.ReadZDOID();
            int revision = pkg.ReadInt();
            byte[] data = pkg.ReadByteArray();
            if (data == null || revision <= 0)
                return;

            if (!SetShipMapData(id, revision, data))
                return;

            ZDO zdo = ZDOMan.instance?.GetZDO(id);
            if (zdo != null)
            {
                if (!zdo.GetBool(LongshipCustomizableParts.s_mapDataCompressed))
                    zdo.Set(LongshipCustomizableParts.s_mapDataCompressed, true);

                SyncShipMapRevision(zdo, revision);

                RemoveShipMapDataFromRuntime(id);
            }

            LogInfo($"Ship map data for {id} submitted from {sender}");
        }

        [HarmonyPatch]
        public static class ZDOMan_Load_TrackShipMapZdos
        {
            private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(ZDOMan), nameof(ZDOMan.Load));
                yield return AccessTools.Method(typeof(ZDOMan), nameof(ZDOMan.LoadChunks));
            }

            public static void Postfix(ZDOMan __instance)
            {
                RebuildTrackedShipZdos(__instance.m_objectsByID.Values);
            }
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.CreateNewZDO), new Type[] { typeof(Vector3), typeof(int) })]
        public static class ZDOMan_CreateNewZDO_TrackShipMapZdos
        {
            public static void Postfix(ZDO __result)
            {
                TrackShipZdo(__result, extractFromRuntime: true);
            }
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.CreateNewZDO), new Type[] { typeof(ZDOID), typeof(Vector3), typeof(int) })]
        public static class ZDOMan_CreateNewZDO_WithUid_TrackShipMapZdos
        {
            public static void Postfix(ZDO __result)
            {
                TrackShipZdo(__result, extractFromRuntime: true);
            }
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.HandleDestroyedZDO))]
        public static class ZDOMan_HandleDestroyedZDO_ClearShipMapStore
        {
            public static void Prefix(ZDOID uid)
            {
                RemoveShipMapData(uid);
            }
        }

        [HarmonyPatch(typeof(ZDO), nameof(ZDO.Deserialize))]
        public static class ZDO_Deserialize_TrackShipMapData
        {
            public static void Postfix(ZDO __instance)
            {
                TrackShipZdo(__instance, extractFromRuntime: true);
            }
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.PrepareSave))]
        public static class ZDOMan_PrepareSave_InjectShipMapData
        {
            public static void Prefix()
            {
                if (ZNet.instance != null && ZNet.instance.IsServer())
                    InjectAllTrackedShipMapDataIntoRuntime();
            }

            public static void Postfix()
            {
                RemoveInjectedShipMapDataFromRuntime();
            }
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.Update))]
        public static class ZDOMan_Update_PruneClientShipMapStore
        {
            public static void Postfix()
            {
                PruneClientShipMapStore();
            }
        }

        [HarmonyPatch(typeof(Ship), nameof(Ship.UpdateControlls))]
        public static class Ship_UpdateControlls_SpeedLimitWithoutMast
        {
            public static void Prefix(Ship __instance)
            {
                if (!IsControlledComponent(__instance))
                    return;

                if (__instance.m_nview && __instance.m_nview.IsValid() && (__instance.IsSailUp()) && __instance.m_nview.GetZDO().GetBool(LongshipCustomizableParts.s_mastRemoved))
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
                {
                    __instance.m_width = containerWidth.Value;
                    
                    string typeName = __instance.GetType().Name;
                    m_nview.GetZDO().Set(ZNetView.CustomFieldsStr, true);
                    m_nview.GetZDO().Set((ZNetView.CustomFieldsStr + typeName).GetStableHashCode(), true);
                    m_nview.GetZDO().Set(typeName + "." + "m_width", containerWidth.Value);
                }

                if (m_nview.GetZDO().GetBool(LongshipCustomizableParts.s_containerUpgradedLvl2) && __instance.m_height < containerHeight.Value)
                {
                    __instance.m_height = containerHeight.Value;

                    string typeName = __instance.GetType().Name;
                    m_nview.GetZDO().Set(ZNetView.CustomFieldsStr, true);
                    m_nview.GetZDO().Set((ZNetView.CustomFieldsStr + typeName).GetStableHashCode(), true);
                    m_nview.GetZDO().Set(typeName + "." + "m_height", containerHeight.Value);
                }
            }
        }

        private static bool IsBoxLadder(Ladder ladder) => ladder.m_name == "$lu_box_name";

        [HarmonyPatch(typeof(Ladder), nameof(Ladder.GetHoverText))]
        public static class Ladder_GetHoverText_BoxClimb
        {
            public static bool Prefix(Ladder __instance, ref string __result)
            {
                if (!IsBoxLadder(__instance))
                    return true;

                if (__instance.InUseDistance(Player.m_localPlayer) && !Player.m_localPlayer.IsAttached())
                    __result = Localization.instance.Localize(__instance.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $lu_box_climb");
                else
                    __result = "";
                
                return false;
            }
        }

        [HarmonyPatch(typeof(Ladder), nameof(Ladder.Interact))]
        public static class Ladder_Interact_BoxClimb
        {
            public static bool Prefix(Ladder __instance, Humanoid character) => !IsBoxLadder(__instance) || !character.IsAttached();
            public static void Postfix(Ladder __instance, Humanoid character, bool hold)
            {
                if (!IsBoxLadder(__instance))
                    return;

                if (!hold && __instance.InUseDistance(character) && !character.IsAttached())
                {
                    character.SetMoveDir(__instance.m_targetPos.forward);
                    character.UpdateWalking(Time.deltaTime);
                }
            }
        }

        private static readonly Stack<bool> s_mapTableDataCompressionFlags = new Stack<bool>();
        private static int s_mapTableWriteDepth = 0;

        private static readonly int s_shipMapDataRevision = "ShipMapDataRevision".GetStableHashCode();

        private static bool IsShipMapTable(MapTable mapTable)
        {
            return mapTable.name == LongshipCustomizableParts.mapTablePrefabName && IsControlledComponent(mapTable);
        }

        private static bool IsCompressedMapDataContext()
        {
            return s_mapTableDataCompressionFlags.Count > 0 && s_mapTableDataCompressionFlags.Peek();
        }

        [HarmonyPatch(typeof(MapTable), nameof(MapTable.OnRead), new Type[] { typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData), typeof(bool) })]
        public static class MapTable_OnRead_TrackCompressionContext
        {
            public static void Prefix(MapTable __instance, ref bool __state)
            {
                __state = IsShipMapTable(__instance);
                if (!__state)
                    return;

                if (IsValidMapTable(__instance))
                    InjectShipMapDataIntoZdo(__instance.m_nview.GetZDO());

                s_mapTableDataCompressionFlags.Push(IsValidMapTable(__instance) && __instance.m_nview.GetZDO().GetBool(LongshipCustomizableParts.s_mapDataCompressed));
            }

            public static Exception Finalizer(MapTable __instance, bool __state, Exception __exception)
            {
                if (__state)
                {
                    if (s_mapTableWriteDepth == 0 && IsValidMapTable(__instance))
                        RemoveShipMapDataFromRuntime(__instance.m_nview.GetZDO().m_uid);

                    if (s_mapTableDataCompressionFlags.Count > 0)
                        s_mapTableDataCompressionFlags.Pop();
                }

                return __exception;
            }
        }

        [HarmonyPatch(typeof(MapTable), nameof(MapTable.OnWrite))]
        public static class MapTable_OnWrite_ScaleWriteEffects
        {
            public static bool inCall = false;

            public static void Prefix(MapTable __instance, ref bool __state)
            {
                inCall = IsShipMapTable(__instance);
                __state = inCall;

                if (!__state)
                    return;

                if (IsValidMapTable(__instance))
                    InjectShipMapDataIntoZdo(__instance.m_nview.GetZDO());

                s_mapTableWriteDepth++;
                s_mapTableDataCompressionFlags.Push(IsValidMapTable(__instance) && __instance.m_nview.GetZDO().GetBool(LongshipCustomizableParts.s_mapDataCompressed));
            }

            public static Exception Finalizer(MapTable __instance, bool __state, Exception __exception)
            {
                inCall = false;

                if (__state)
                {
                    if (IsValidMapTable(__instance))
                        RemoveShipMapDataFromRuntime(__instance.m_nview.GetZDO().m_uid);

                    if (s_mapTableDataCompressionFlags.Count > 0)
                        s_mapTableDataCompressionFlags.Pop();

                    s_mapTableWriteDepth = Math.Max(0, s_mapTableWriteDepth - 1);
                }

                return __exception;
            }
        }

        [HarmonyPatch(typeof(MapTable), nameof(MapTable.RPC_MapData))]
        public static class MapTable_RPC_MapData_MarkCompressed
        {
            public static void Prefix(MapTable __instance, ref byte[] __state)
            {
                __state = null;
                if (!IsShipMapTable(__instance) || !IsValidMapTable(__instance) || !__instance.m_nview.IsOwner())
                    return;

                ZDO zdo = __instance.m_nview.GetZDO();
                InjectShipMapDataIntoZdo(zdo);
                if (TryGetShipMapData(zdo.m_uid, out ShipMapDataEntry entry) && entry.Data != null)
                    __state = (byte[])entry.Data.Clone();
            }

            public static void Postfix(MapTable __instance, byte[] __state)
            {
                if (!IsShipMapTable(__instance) || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner())
                    return;

                ZDO zdo = __instance.m_nview.GetZDO();
                byte[] currentMapData = zdo.GetByteArray(ZDOVars.s_data);
                bool changed = __state == null
                    ? currentMapData != null
                    : currentMapData == null || !StructuralComparisons.StructuralEqualityComparer.Equals(__state, currentMapData);

                int revision = Math.Max(zdo.GetInt(s_shipMapDataRevision), GetShipMapRevision(zdo.m_uid));
                if (currentMapData != null)
                {
                    revision = changed ? Math.Max(1, revision + 1) : Math.Max(1, revision);
                    SetShipMapData(zdo.m_uid, revision, currentMapData);
                    zdo.Set(LongshipCustomizableParts.s_mapDataCompressed, true);
                    SyncShipMapRevision(zdo, revision);

                    if (ZNet.instance != null && !ZNet.instance.IsServer())
                        SubmitShipMapDataToServer(zdo, revision, currentMapData);
                }

                RemoveShipMapDataFromRuntime(zdo.m_uid);
            }
        }

        [HarmonyPatch(typeof(ZDO), nameof(ZDO.Serialize))]
        public static class ZDO_Serialize_SkipUnchangedShipMapData
        {
            public struct SerializeState
            {
                public bool Removed;
                public byte[] Data;
            }

            public static void Prefix(ZDO __instance, ref SerializeState __state)
            {
                __state = default;

                if (__instance.GetPrefab() != LongshipCustomizableParts.prefabInt)
                    return;

                if (!__instance.GetBool(LongshipCustomizableParts.s_mapDataCompressed))
                    return;

                if (!ZDOExtraData.s_byteArrays.TryGetValue(__instance.m_uid, out BinarySearchDictionary<int, byte[]> byteArrays) || !byteArrays.TryGetValue(ZDOVars.s_data, out byte[] mapData) || mapData == null)
                    return;

                __state.Removed = byteArrays.Remove(ZDOVars.s_data);
                __state.Data = mapData;

                if (__state.Removed && byteArrays.Count == 0)
                    ZDOExtraData.s_byteArrays.Remove(__instance.m_uid);
            }

            public static void Finalizer(ZDO __instance, SerializeState __state)
            {
                if (__state.Removed)
                    ZDOExtraData.Set(__instance.m_uid, ZDOVars.s_data, __state.Data);
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.ReadExploredArray))]
        public static class Minimap_ReadExploredArray_CompressedShipMapData
        {
            public static bool Prefix(Minimap __instance, ZPackage pkg, ref List<bool> __result)
            {
                if (!IsCompressedMapDataContext())
                    return true;

                int exploredLength = pkg.ReadInt();
                if (exploredLength != __instance.m_explored.Length)
                {
                    ZLog.LogWarning("Map exploration array size missmatch:" + exploredLength + " VS " + __instance.m_explored.Length);
                    __result = null;
                    return false;
                }

                byte[] packedExplored = pkg.ReadByteArray();
                BitArray bits = new BitArray(packedExplored);
                List<bool> explored = new List<bool>(exploredLength);
                for (int i = 0; i < exploredLength; i++)
                    explored.Add(i < bits.Count && bits[i]);

                LogInfo($"Ship map data read: explored payload unpacked {packedExplored.Length} -> {exploredLength} bytes ({Math.Max(0, exploredLength - packedExplored.Length)} bytes added)");

                __result = explored;
                return false;
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.GetSharedMapData))]
        public static class Minimap_GetSharedMapData_CompressShipMapData
        {
            public static void Postfix(ref byte[] __result)
            {
                if (s_mapTableWriteDepth == 0)
                    return;

                ZPackage source = new ZPackage(__result);
                int version = source.ReadInt();
                int exploredLength = source.ReadInt();

                bool[] explored = new bool[exploredLength];
                for (int i = 0; i < exploredLength; i++)
                    explored[i] = source.ReadBool();

                BitArray bits = new BitArray(explored);
                byte[] packedExplored = new byte[(bits.Length - 1) / 8 + 1];
                bits.CopyTo(packedExplored, 0);

                LogInfo($"Ship map data write: explored payload compressed {exploredLength} -> {packedExplored.Length} bytes ({Math.Max(0, exploredLength - packedExplored.Length)} bytes saved)");

                ZPackage result = new ZPackage();
                result.Write(version);
                result.Write(exploredLength);
                result.Write(packedExplored);

                if (version >= 2)
                {
                    int pinCount = source.ReadInt();
                    result.Write(pinCount);

                    for (int i = 0; i < pinCount; i++)
                    {
                        long ownerId = source.ReadLong();
                        string pinName = source.ReadString();
                        Vector3 pinPos = source.ReadVector3();
                        int pinType = source.ReadInt();
                        bool pinChecked = source.ReadBool();
                        string author = version >= 3 ? source.ReadString() : string.Empty;

                        result.Write(ownerId);
                        result.Write(pinName);
                        result.Write(pinPos);
                        result.Write(pinType);
                        result.Write(pinChecked);

                        if (version >= 3)
                            result.Write(author);
                    }
                }

                __result = result.GetArray();
            }
        }

        [HarmonyPatch(typeof(EffectList), nameof(EffectList.Create))]
        public static class EffectList_Create_ScaleWriteEffects
        {
            public static void Prefix(EffectList __instance, ref float scale, ref bool __state)
            {
                if (MapTable_OnWrite_ScaleWriteEffects.inCall)
                {
                    scale = 0.1f;
                    if (__instance.HasEffects() && !__instance.m_effectPrefabs[0].m_scale)
                    {
                        __state = true;
                        __instance.m_effectPrefabs[0].m_scale = true;
                    }
                }
            }

            public static void Finalizer(EffectList __instance, bool __state)
            {
                if (__state)
                    __instance.m_effectPrefabs[0].m_scale = false;
            }
        }

        [HarmonyPatch(typeof(MapTable), nameof(MapTable.Start))]
        public static class MapTable_Start_PreventStartScript
        {
            [HarmonyPriority(Priority.First)]
            public static bool Prefix(MapTable __instance) => __instance.name != LongshipCustomizableParts.mapTablePrefabName || !IsControlledComponent(__instance);
        }

        public static void FixMeshRendererProperties(MeshRenderer renderer)
        {
            renderer.sharedMaterial = DeepCopyMaterial(renderer.sharedMaterial);
            if (renderer.GetComponent<MeshFilter>() is MeshFilter meshFilter)
                FixSharedMesh(meshFilter);
        }

        private static Texture2D CopyTexture(Texture2D source)
        {
            if (source == null)
                return null;

            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, source.mipmapCount > 1);
            
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);

            try
            {
                Graphics.Blit(source, rt);

                RenderTexture.active = rt;
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                copy.Apply(true, true);
                copy.filterMode = source.filterMode;
            }
            finally
            {
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
            }

            return copy;
        }

        private static Material DeepCopyMaterial(Material original)
        {
            Material copy = new Material(original.shader);

            copy.CopyPropertiesFromMaterial(original);

            foreach (string propertyName in original.GetTexturePropertyNames())
                CopyTextureIfExists(original, copy, propertyName);

            return copy;
        }

        private static void CopyTextureIfExists(Material original, Material copy, string propertyName)
        {
            if (!original.HasProperty(propertyName))
                return;

            Texture2D originalTexture = original.GetTexture(propertyName) as Texture2D;
            if (originalTexture == null)
                return;

            copy.SetTexture(propertyName, CopyTexture(originalTexture));
        }

        private static void FixSharedMesh(MeshFilter meshFilter)
        {
            if (meshFilter.sharedMesh == null)
                return;

            Mesh newMesh = new Mesh
            {
                vertices = meshFilter.sharedMesh.vertices,
                triangles = meshFilter.sharedMesh.triangles,
                normals = meshFilter.sharedMesh.normals,
                uv = meshFilter.sharedMesh.uv,
                uv2 = meshFilter.sharedMesh.uv2,
                tangents = meshFilter.sharedMesh.tangents,
                colors = meshFilter.sharedMesh.colors,
                bindposes = meshFilter.sharedMesh.bindposes,
                boneWeights = meshFilter.sharedMesh.boneWeights,
                subMeshCount = meshFilter.sharedMesh.subMeshCount
            };

            for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
                newMesh.SetTriangles(meshFilter.sharedMesh.GetTriangles(i), i);

            newMesh.RecalculateBounds();

            meshFilter.mesh = newMesh;
            meshFilter.sharedMesh = newMesh;
        }
    }
}
