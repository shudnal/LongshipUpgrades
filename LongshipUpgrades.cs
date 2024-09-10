using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;
using System.Collections.Generic;
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

        internal static ConfigEntry<bool> modEnabled;
        internal static ConfigEntry<bool> configLocked;
        internal static ConfigEntry<bool> loggingEnabled;

        private void Awake()
        {
            harmony.PatchAll();

            instance = this;

            ConfigInit();
            _ = configSync.AddLockingConfigEntry(configLocked);

            Game.isModded = true;
        }

        public void ConfigInit()
        {
            config("General", "NexusID", 0, "Nexus mod ID for updates", false);

            modEnabled = config("General", "Enabled", defaultValue: true, "Enable this mod.");
            configLocked = config("General", "Lock Configuration", defaultValue: true, "Configuration is locked and can be changed by server admins only.");
            loggingEnabled = config("General", "Logging enabled", defaultValue: false, "Enable logging. [Not Synced with Server]", false);
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

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, defaultValue, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T defaultValue, string description, bool synchronizedSetting = true) => config(group, name, defaultValue, new ConfigDescription(description), synchronizedSetting);

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

                if (!colliderTriggered.TryGetValue(collider, out int counter))
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

        [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
        public static class Ship_Awake_HealthUpgrades
        {
            public static void Postfix(Ship __instance)
            {
                if (!IsControlledComponent(__instance))
                    return;

                if (!__instance || !__instance.m_nview.GetZDO().GetBool(LongshipCustomizableParts.s_protectionUpgraded))
                    return;

                WearNTear component = __instance.GetComponent<WearNTear>();
                if ((bool)component && component.m_health < 1500)
                    component.m_health = 1500;
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

                ZNetView m_nview = (__instance.m_rootObjectOverride ? __instance.m_rootObjectOverride.GetComponent<ZNetView>() : __instance.GetComponent<ZNetView>());
                if (m_nview == null || !m_nview.IsValid())
                    return;

                if (m_nview.GetZDO().GetBool(LongshipCustomizableParts.s_containerUpgradedLvl1) && __instance.m_width < 7)
                    __instance.m_width = 7;

                if (m_nview.GetZDO().GetBool(LongshipCustomizableParts.s_containerUpgradedLvl2) && __instance.m_height < 4)
                    __instance.m_height = 4;
            }
        }
        
        [HarmonyPatch(typeof(Ladder))]
        public static class Ladder_ProtectionUpgrades
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.First)]
            [HarmonyPatch(nameof(Ladder.GetHoverText))]
            public static void GetHoverTextPostfix(Ladder __instance, ref string __result)
            {
                if (!IsControlledComponent(__instance))
                    return;

                ZNetView m_nview = __instance.GetComponentInParent<ZNetView>();

                if (m_nview == null)
                    return;

                ZDO zdo = m_nview.GetZDO();
                if (!zdo.GetBool(LongshipCustomizableParts.s_protectionUpgraded))
                {
                    if (!ZInput.IsNonClassicFunctionality() || !ZInput.IsGamepadActive())
                        __result += Localization.instance.Localize("\n[<color=#ffff00ff><b>$KEY_AltPlace + $KEY_Use</b></color>] $menu_expand");
                    else
                        __result += Localization.instance.Localize("\n[<color=#ffff00ff><b>$KEY_JoyAltKeys + $KEY_Use</b></color>] $menu_expand");
                }
            }

            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            [HarmonyPatch(nameof(Ladder.Interact))]
            public static bool InteractPrefix(Ladder __instance, Humanoid character, ref bool hold, ref bool alt)
            {
                if (!IsControlledComponent(__instance))
                    return true;

                if (hold || !alt)
                    return true;

                ZNetView m_nview = __instance.GetComponentInParent<ZNetView>();

                if (m_nview == null)
                    return true;

                ZDO zdo = m_nview.GetZDO();
                if (zdo.GetBool(LongshipCustomizableParts.s_protectionUpgraded))
                    return true;

                zdo.Set(LongshipCustomizableParts.s_protectionUpgraded, true);
                alt = false;
                hold = true;
                return false;
            }
        }
    }
}
