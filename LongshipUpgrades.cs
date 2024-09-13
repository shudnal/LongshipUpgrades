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

        internal static ConfigEntry<Color> hintColor;
        internal static ConfigEntry<Color> hintAmountColor;
        internal static ConfigEntry<Color> hintItemColor;

        internal static ConfigEntry<bool> containerEnabled;
        internal static ConfigEntry<int> containerHeight;
        internal static ConfigEntry<int> containerWidth;

        internal static ConfigEntry<bool> healthEnabled;
        internal static ConfigEntry<int> healthUpgradeLvl1;
        internal static ConfigEntry<int> healthUpgradeLvl2;
        internal static ConfigEntry<bool> ashlandsProtection;

        internal static ConfigEntry<bool> lanternEnabled;
        internal static ConfigEntry<bool> lanternRemovable;
        internal static ConfigEntry<bool> lanternAutoSwtich;
        internal static ConfigEntry<bool> lanternSwitchable;
        internal static ConfigEntry<Color> lanternLightColor;

        internal static ConfigEntry<bool> turretsEnabled;

        internal static ConfigEntry<bool> mastEnabled;
        internal static ConfigEntry<bool> mastRemovable;

        internal static ConfigEntry<bool> tentEnabled;
        internal static ConfigEntry<bool> tentHeat;
        internal static ConfigEntry<bool> tentRemovable;

        internal static ConfigEntry<bool> changeHead;
        internal static ConfigEntry<bool> changeShields;
        internal static ConfigEntry<bool> changeTent;

        internal static ConfigEntry<string> mastUpgradeRecipe;
        internal static ConfigEntry<string> lanternUpgradeRecipe;
        internal static ConfigEntry<string> tentUpgradeRecipe;
        internal static ConfigEntry<string> containerLvl1UpgradeRecipe;
        internal static ConfigEntry<string> containerLvl2UpgradeRecipe;
        internal static ConfigEntry<string> healthUpgradeRecipe;
        internal static ConfigEntry<string> ashlandsUpgradeRecipe;
        internal static ConfigEntry<string> turretsUpgradeRecipe;

        public static string configDirectory;

        private void Awake()
        {
            harmony.PatchAll();

            instance = this;

            ConfigInit();
            _ = configSync.AddLockingConfigEntry(configLocked);

            configDirectory = Path.Combine(Paths.ConfigPath, pluginID);

            Game.isModded = true;

            LoadIcons();
        }

        public void ConfigInit()
        {
            config("General", "NexusID", 2885, "Nexus mod ID for updates", false);

            configLocked = config("General", "Lock Configuration", defaultValue: true, "Configuration is locked and can be changed by server admins only.");
            loggingEnabled = config("General", "Logging enabled", defaultValue: false, "Enable logging. [Not Synced with Server]", false);
            
            hintColor = config("Hint", "Hint color", defaultValue: new Color(0.75f, 0.75f, 0.75f, 0.8f), "Color of hint in upgrade tooltip. [Not Synced with Server]", false);
            hintAmountColor = config("Hint", "Entry amount color", defaultValue: new Color(1f, 1f, 0f, 0.6f), "Color for amount. [Not Synced with Server]", false);
            hintItemColor = config("Hint", "Entry item name color", defaultValue: new Color(0.75f, 0.75f, 0.75f, 0.6f), "Color for item name. [Not Synced with Server]", false);

            containerEnabled = config("Container", "Enable upgrades", defaultValue: true, "Container upgrades. Pls be aware items in upgraded slots will be unavailable after mod disabling. But it will drop on ship destruction.");
            containerHeight = config("Container", "Upgraded height", defaultValue: 4, "Height of ship container after upgrade.");
            containerWidth = config("Container", "Upgraded width", defaultValue: 7, "Width of ship container after upgrade.");

            healthEnabled = config("Health", "Enable upgrades", defaultValue: true, "Health upgrades.");
            healthUpgradeLvl1 = config("Health", "Lvl 1 Upgrade", defaultValue: 1500, "Health of ship container after first upgrade.");
            healthUpgradeLvl2 = config("Health", "Lvl 2 Upgrade", defaultValue: 2000, "Health of ship container after second upgrade.");
            ashlandsProtection = config("Health", "Lvl 2 Ashlands protection", defaultValue: true, "Should be ship protected from ashlands ocean after second upgrade.");

            lanternEnabled = config("Lantern", "Enable upgrades", defaultValue: true, "Lantern upgrades requires mast to be upgraded.");
            lanternRemovable = config("Lantern", "Make removable", defaultValue: true, "Make lantern removable. World restart or ship rebuild required to apply changes.");
            lanternAutoSwtich = config("Lantern", "Light auto enabled and disabled", defaultValue: true, "Light will be automatically enabled in night time or dark environments and automatically disabled in day light");
            lanternSwitchable = config("Lantern", "Light switch enabled", defaultValue: true, "Enable manual light switch. World restart or ship rebuild required to apply changes.");
            lanternLightColor = config("Lantern", "Light color", defaultValue: new Color(0.96f, 0.78f, 0.68f, 1f), "Color of lantern light. Switch light to apply changes.");

            mastEnabled = config("Mast", "Enable upgrades", defaultValue: true, "Mast upgrade makes lantern and tent upgrades possible.");
            mastRemovable = config("Mast", "Make removable", defaultValue: true, "Enable mast removal. World restart or ship rebuild required to apply changes.");

            tentEnabled = config("Tent", "Enable upgrades", defaultValue: true, "Tent upgrades requires mast to be upgraded.");
            tentHeat = config("Tent", "Heat enabled", defaultValue: true, "Enable heat zone under the tent to get place to rest. Enabled lantern required.");
            tentRemovable = config("Tent", "Make removable", defaultValue: true, "Enable tent removal. World restart or ship rebuild required to apply changes.");

            turretsEnabled = config("Turrets", "Enable upgrades", defaultValue: true, "Enable turrets upgrades.");

            changeHead = config("Style", "Change heads", defaultValue: true, "Change ship's head style.");
            changeShields = config("Style", "Change shields color", defaultValue: true, "Change shields colors. World restart or ship rebuild required to apply changes.");
            changeTent = config("Style", "Change tent color", defaultValue: true, "Change tent colors. World restart or ship rebuild required to apply changes.");

            mastUpgradeRecipe = config("Recipes", "Mast", defaultValue: "", "Mast upgrade recipe. World restart or ship rebuild required to apply changes.");
            lanternUpgradeRecipe = config("Recipes", "Lantern", defaultValue: "", "Lantern upgrade recipe. World restart or ship rebuild required to apply changes.");
            tentUpgradeRecipe = config("Recipes", "Tent", defaultValue: "", "Tent upgrade recipe. World restart or ship rebuild required to apply changes.");
            containerLvl1UpgradeRecipe = config("Recipes", "Container - Lvl 1", defaultValue: "", "Container lvl 1 upgrade recipe. World restart or ship rebuild required to apply changes.");
            containerLvl2UpgradeRecipe = config("Recipes", "Container - Lvl 2", defaultValue: "", "Container lvl 2 upgrade recipe. World restart or ship rebuild required to apply changes.");
            healthUpgradeRecipe = config("Recipes", "Hull - Health", defaultValue: "", "Hull lvl 1 upgrade recipe. World restart or ship rebuild required to apply changes.");
            ashlandsUpgradeRecipe = config("Recipes", "Hull - Ashlands", defaultValue: "", "Hull lvl 2 upgrade recipe. World restart or ship rebuild required to apply changes.");
            turretsUpgradeRecipe = config("Recipes", "Turrets", defaultValue: "", "Turrets upgrade recipe. World restart or ship rebuild required to apply changes.");
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

        private void LoadIcons()
        {
            LoadTexture("ashlands_hull.png", ref LongshipCustomizableParts.s_ashlandsHull);
            LoadTexture("tent_blue.png", ref LongshipCustomizableParts.s_tentBlue);
            LoadTexture("tent_black.png", ref LongshipCustomizableParts.s_tentBlack);
        }

        internal static void LoadIcon(string filename, ref Sprite icon)
        {
            Texture2D tex = new Texture2D(2, 2);
            if (LoadTexture(filename, ref tex))
                icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }

        internal static bool LoadTexture(string filename, ref Texture2D tex)
        {
            string fileInConfigFolder = Path.Combine(configDirectory, filename);
            if (File.Exists(fileInConfigFolder))
            {
                LogInfo($"Loaded image: {fileInConfigFolder}");
                return tex.LoadImage(File.ReadAllBytes(fileInConfigFolder));
            }

            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            string name = executingAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));

            Stream resourceStream = executingAssembly.GetManifestResourceStream(name);

            byte[] data = new byte[resourceStream.Length];
            resourceStream.Read(data, 0, data.Length);

            tex.name = Path.GetFileNameWithoutExtension(filename);

            return tex.LoadImage(data, true);
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
                if (!IsControlledComponent(__instance))
                    return;

                if (!containerEnabled.Value)
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
    }
}
