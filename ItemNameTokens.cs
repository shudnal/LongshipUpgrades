using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LongshipUpgrades
{
    internal static class ItemNameTokens
    {
        private static readonly Dictionary<string, string> itemNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, GameObject> itemPrefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> craftingStationNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        private static class Player_Load_UpdateRegisters
        {
            private static void Prefix() => UpdateRegisters();
        }

        internal static void UpdateRegisters()
        {
            if (!ObjectDB.instance)
                return;

            itemNames.Clear();
            itemPrefabs.Clear();
            craftingStationNames.Clear();

            foreach (GameObject item in ObjectDB.instance.m_items)
            {
                if (!item || !item.TryGetComponent(out ItemDrop itemDrop))
                    continue;

                ItemDrop.ItemData.SharedData shared = itemDrop.m_itemData?.m_shared;
                if (shared == null || string.IsNullOrWhiteSpace(shared.m_name))
                    continue;

                RegisterItem(item.name, shared.m_name, item);
            }

            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                RegisterCraftingStation(recipe?.m_craftingStation);
                RegisterCraftingStation(recipe?.m_repairStation);
            }

            if (ZNetScene.instance)
            {
                foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
                {
                    if (!prefab)
                        continue;

                    CraftingStation station = prefab.GetComponentInChildren<CraftingStation>(includeInactive: true);
                    if (station)
                        RegisterCraftingStation(station, prefab.name);
                }
            }
        }

        private static void RegisterItem(string prefabName, string itemNameToken, GameObject prefab)
        {
            if (string.IsNullOrWhiteSpace(prefabName) || string.IsNullOrWhiteSpace(itemNameToken) || !prefab)
                return;

            itemNames[prefabName] = itemNameToken;
            itemNames[itemNameToken] = itemNameToken;
            itemPrefabs[prefabName] = prefab;
            itemPrefabs[itemNameToken] = prefab;
        }

        private static void RegisterCraftingStation(CraftingStation station, string prefabName = null)
        {
            if (!station || string.IsNullOrWhiteSpace(station.m_name))
                return;

            string stationName = station.m_name.Trim();
            craftingStationNames[stationName] = stationName;

            string resolvedPrefabName = string.IsNullOrWhiteSpace(prefabName)
                ? Utils.GetPrefabName(station.gameObject.name)
                : Utils.GetPrefabName(prefabName);

            if (!string.IsNullOrWhiteSpace(resolvedPrefabName))
                craftingStationNames[resolvedPrefabName] = stationName;
        }

        internal static string GetItemName(this string input)
        {
            string key = input?.Trim() ?? string.Empty;
            return itemNames.TryGetValue(key, out string itemName) ? itemName : key;
        }

        internal static bool TryGetItemPrefab(string input, out GameObject prefab)
        {
            string key = input?.Trim() ?? string.Empty;
            if (itemPrefabs.Count == 0)
                UpdateRegisters();

            if (itemPrefabs.TryGetValue(key, out prefab) && prefab)
                return true;

            UpdateRegisters();
            if (itemPrefabs.TryGetValue(key, out prefab) && prefab)
                return true;

            prefab = null;
            return false;
        }

        internal static string GetCraftingStationName(this string input)
        {
            string key = input?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            if (craftingStationNames.Count == 0)
                UpdateRegisters();

            if (craftingStationNames.TryGetValue(key, out string stationName))
                return stationName;

            UpdateRegisters();
            return craftingStationNames.TryGetValue(key, out stationName) ? stationName : key;
        }
    }
}
