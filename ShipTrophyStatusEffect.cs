using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace LongshipUpgrades
{
    internal static class ShipTrophyStatusEffect
    {
        public const string statusEffectShipTrophyName = "ShipTrophy";
        public static readonly int statusEffecShipTrophyHash = statusEffectShipTrophyName.GetStableHashCode();

        public const string statusEffectName = "$se_ship_trophy_name";
        public const string statusEffectTooltip = "$se_ship_trophy_tooltip";

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public static class ObjectDB_Awake_AddStatusEffects
        {
            public static void AddCustomStatusEffects(ObjectDB odb)
            {
                if (odb.m_StatusEffects.Count > 0)
                {
                    if (!odb.m_StatusEffects.Any(se => se.name == statusEffectShipTrophyName))
                    {
                        SE_Stats statusEffect = ScriptableObject.CreateInstance<SE_Stats>();
                        statusEffect.name = statusEffectShipTrophyName;
                        statusEffect.m_nameHash = statusEffecShipTrophyHash;

                        statusEffect.m_name = statusEffectName;
                        statusEffect.m_tooltip = statusEffectTooltip;

                        statusEffect.m_ttl = LongshipUpgrades.itemStandLength.Value;
                        statusEffect.m_cooldown = 0;

                        odb.m_StatusEffects.Add(statusEffect);
                    }
                }
            }

            private static void Postfix(ObjectDB __instance)
            {
                AddCustomStatusEffects(__instance);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        public static class ObjectDB_CopyOtherDB_AddStatusEffects
        {
            private static void Postfix(ObjectDB __instance)
            {
                ObjectDB_Awake_AddStatusEffects.AddCustomStatusEffects(__instance);
            }
        }

    }
}
