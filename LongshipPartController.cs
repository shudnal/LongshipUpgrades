﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LongshipUpgrades
{
    public class LongshipPartController : MonoBehaviour, Hoverable, Interactable
    {
        public class UpgradeRequirements
        {
            public int m_zdoVar;
            public Piece.Requirement[] m_requirements = new Piece.Requirement[0];
            public string m_messageUpgrade = "";
            public string m_stationName;
            public int m_stationLevel;
            public int m_stationRange;

            public UpgradeRequirements(int zdoVar, string message, Piece.Requirement[] requirements, string station, int level, int range)
            {
                m_zdoVar = zdoVar;
                m_requirements = requirements;
                m_messageUpgrade = message;
                m_stationName = station;
                m_stationLevel = level;
                m_stationRange = range;
            }

            public bool FillUpgradeHover(ZDO zdo, string name)
            {
                if (m_zdoVar == 0 || zdo.GetBool(m_zdoVar))
                    return false;

                sb.Clear();

                if (!Player.m_localPlayer.NoCostCheat() && !string.IsNullOrWhiteSpace(m_stationName) && !Player.m_localPlayer.KnowStationLevel(m_stationName, m_stationLevel))
                {
                    AddUpgradeHintToHover("$lu_controller_message_unknownupgrade");
                    return true;
                }

                sb.Append(name);

                if (HaveCraftinStationInRange(out bool lvlMet))
                {
                    if (lvlMet)
                        sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_upgradebutton");

                    AddUpgradeHintToHover(m_messageUpgrade);

                    AddUpgradeRequirementsToHint(lvlMet);
                }
                else
                {
                    AddUpgradeHintToHover(m_messageUpgrade);
                    sb.Append("\n\n$msg_missingrequirement:");
                    AddRequiredStationToHover();
                }

                return true;
            }

            public bool CheckConsume(ZNetView nview, Player player, Vector3 position, out bool result)
            {
                result = false;
                if (m_zdoVar == 0 || nview.GetZDO().GetBool(m_zdoVar))
                    return false;

                if (!RemoveRequiredItems(player))
                {
                    player.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                    return true;
                }

                nview.InvokeRPC("SetBuilt", m_zdoVar, m_stationName, position.ToString());

                PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
                playerProfile.IncrementStat(PlayerStatType.CraftsOrUpgrades);
                playerProfile.IncrementStat(PlayerStatType.Upgrades);

                result = true;
                return true;
            }

            public void AddSpentUpgrades(ZDO zdo, Dictionary<int, Piece.Requirement[]> upgradeReqs)
            {
                if (m_zdoVar != 0 && zdo.GetBool(m_zdoVar) && m_requirements.Length > 0)
                    upgradeReqs[m_zdoVar] = m_requirements;
            }

            private void AddUpgradeRequirementsToHint(bool lvlMet)
            {
                if (m_requirements == null || m_requirements.Length == 0 || string.IsNullOrWhiteSpace(m_stationName))
                    return;

                sb.Append("\n");
                
                if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost))
                    sb.AppendFormat("\n<color=#{0}>$menu_nobuildcost</color>", ColorUtility.ToHtmlStringRGBA(LongshipUpgrades.hintStationColor.Value));

                sb.Append("\n$hud_require");

                if (lvlMet)
                    AddRequiredStationToHover(LongshipUpgrades.hintStationColor.Value, LongshipUpgrades.hintStationColor.Value);
                else if (Mathf.Sin(Time.time * 10f) > 0f)
                    AddRequiredStationToHover(LongshipUpgrades.hintStationColor.Value, Color.red);
                else
                    AddRequiredStationToHover(LongshipUpgrades.hintStationColor.Value);

                sb.Append(":");

                m_requirements?.Do(req => AddItemRequirementToHover(req));
            }

            private void AddRequiredStationToHover()
            {
                if (string.IsNullOrWhiteSpace(m_stationName))
                    return;

                sb.AppendFormat(" {0}", m_stationName);

                if (m_stationLevel > 1)
                    sb.AppendFormat(" $msg_level {0}", m_stationLevel);
            }

            private void AddRequiredStationToHover(Color station, Color level)
            {
                if (string.IsNullOrWhiteSpace(m_stationName))
                    return;

                sb.AppendFormat(" <color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGBA(station), m_stationName);

                if (m_stationLevel > 1)
                    sb.AppendFormat(" $msg_level <color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGBA(level), m_stationLevel);
            }

            private void AddRequiredStationToHover(Color station)
            {
                if (string.IsNullOrWhiteSpace(m_stationName))
                    return;

                sb.AppendFormat(" <color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGBA(station), m_stationName);

                if (m_stationLevel > 1)
                    sb.AppendFormat(" $msg_level {0}", m_stationLevel);
            }

            private void AddUpgradeHintToHover(string message)
            {
                if (!string.IsNullOrWhiteSpace(message))
                    sb.AppendFormat("\n<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGBA(LongshipUpgrades.hintColor.Value), message);
            }

            private bool HaveCraftinStationInRange(out bool lvlMet)
            {
                lvlMet = true;
                if (string.IsNullOrWhiteSpace(m_stationName) || Player.m_localPlayer.NoCostCheat())
                    return true;

                lvlMet = false;
                craftingStations.Clear();
                CraftingStation.FindStationsInRange(m_stationName, Player.m_localPlayer.transform.position, m_stationRange, craftingStations);

                if (craftingStations.Count == 0)
                    return false;

                lvlMet = craftingStations.Any(station => station.GetLevel() >= m_stationLevel);

                return true;
            }

            private void AddItemRequirementToHover(Piece.Requirement requirement)
            {
                if (requirement.m_resItem == null)
                    return;

                string itemName = requirement.m_resItem.m_itemData.m_shared.m_name;

                sb.AppendFormat("\n<color=#{0}>{1}</color> <color=#{2}>{3}</color>",
                    ColorUtility.ToHtmlStringRGBA(LongshipUpgrades.hintAmountColor.Value), requirement.GetAmount(1),
                    ColorUtility.ToHtmlStringRGBA(LongshipUpgrades.hintItemColor.Value), Player.m_localPlayer.NoCostCheat() || Player.m_localPlayer.IsMaterialKnown(itemName) ? itemName : "$lu_controller_message_unknownitem");
            }

            private bool RemoveRequiredItems(Player player)
            {
                if (player.NoCostCheat())
                    return true;

                if (!string.IsNullOrWhiteSpace(m_stationName) && m_stationLevel > 0 && !player.KnowStationLevel(m_stationName, m_stationLevel))
                    return false;

                if (!HaveCraftinStationInRange(out bool lvlMet) || !lvlMet)
                    return false;

                if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost))
                    return true;

                if (m_requirements.Length == 0)
                    return true;

                tempRecipe.m_resources = m_requirements;
                if (!player.HaveRequirementItems(tempRecipe, discover: false, qualityLevel: 1))
                    return false;

                player.ConsumeResources(m_requirements, qualityLevel: 1);
                return true;
            }
        }

        public ZNetView m_nview;
        public Ship m_ship;
        public Piece m_piece;
        public string m_name = "Part";

        public bool m_checkGuardStone = true;
        public float m_useDistance = 0f;

        public string m_messageEnable = "$lu_controller_message_enable";
        public string m_messageSwitch = "$lu_controller_message_switch";
        public string m_messageDisable = "$lu_controller_message_disable";

        public UpgradeRequirements[] m_upgradeRequirements = new UpgradeRequirements[0];
        public int m_zdoPartDisabled;
        public int m_zdoPartVariant;

        public int m_variants;

        public int m_switchEffects = -1;
        public int m_enableEffects = -1;
        public int m_disableEffects = -1;

        private static readonly StringBuilder sb = new StringBuilder(20);
        private static Recipe tempRecipe;
        private static readonly List<CraftingStation> craftingStations = new List<CraftingStation>();

        public void Awake()
        {
            if (tempRecipe == null)
            {
                tempRecipe = ScriptableObject.CreateInstance<Recipe>();
                tempRecipe.name = "LongshipPartUpgradeTempRecipe";
            }

            if (m_nview == null)
                m_nview = GetComponentInParent<ZNetView>();

            m_ship = GetComponentInParent<Ship>();
            m_piece = GetComponentInParent<Piece>();
        }

        public string GetHoverText()
        {
            if (!m_nview || !m_nview.IsValid())
                return "";

            if (!IsPositionToInteract())
                return "";

            if (!InUseDistance(Player.m_localPlayer))
                return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");

            if (LongshipUpgrades.onlyCreatorUpgrades.Value && m_piece != null && !m_piece.IsCreator())
                return Localization.instance.Localize("<color=#888888>$piece_noaccess</color>");

            if (m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
                return Localization.instance.Localize(m_name + "\n$piece_noaccess");

            ZDO zdo = m_nview.GetZDO();
            foreach (UpgradeRequirements upgradeRequirements in m_upgradeRequirements)
                if (upgradeRequirements.FillUpgradeHover(zdo, m_name))
                    return Localization.instance.Localize(sb.ToString());

            if (m_zdoPartVariant != 0 && m_variants > 1)
            {
                sb.Clear();
                sb.Append(m_name);
                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] ");
                sb.Append(m_messageSwitch);
                return Localization.instance.Localize(sb.ToString());
            }

            if (m_zdoPartDisabled != 0)
            {
                sb.Clear();
                sb.Append(m_name);
                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] ");
                sb.Append(zdo.GetBool(m_zdoPartDisabled) ? m_messageEnable : m_messageDisable);
                return Localization.instance.Localize(sb.ToString());
            }

            return "";
        }

        public string GetHoverName()
        {
            return m_name;
        }

        public bool Interact(Humanoid human, bool hold, bool alt)
        {
            if (hold)
                return false;

            if (LongshipUpgrades.onlyCreatorUpgrades.Value && m_piece != null && !m_piece.IsCreator())
                return false;

            if (m_checkGuardStone && !PrivateArea.CheckAccess(transform.position))
                return false;

            if (!InUseDistance(human))
                return false;

            if (!m_nview || !m_nview.IsValid())
                return false;

            if (!IsPositionToInteract())
                return false;

            Player player = human as Player;
            ZDO zdo = m_nview.GetZDO();
            foreach (UpgradeRequirements upgradeRequirements in m_upgradeRequirements)
                if (upgradeRequirements.CheckConsume(m_nview, player, transform.position, out bool result))
                    return result;

            if (m_zdoPartVariant != 0 && m_variants > 1)
            {
                m_nview.InvokeRPC("SetVariant", m_zdoPartVariant, (zdo.GetInt(m_zdoPartVariant) + 1) % m_variants, m_switchEffects, transform.position.ToString());
                return true;
            }

            if (m_zdoPartDisabled == 0)
                return false;

            bool active = !zdo.GetBool(m_zdoPartDisabled);
            m_nview.InvokeRPC("SetActive", m_zdoPartDisabled, active, active ? m_disableEffects : m_enableEffects, transform.position.ToString());
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public bool InUseDistance(Humanoid human)
        {
            return m_useDistance == 0f || Vector3.Distance(human.transform.position, transform.position) < m_useDistance;
        }

        public void AddUpgradeRequirement(int zdoVar, string message, Piece.Requirement[] requirements, string station, int level, int range)
        {
            if (zdoVar != 0)
                m_upgradeRequirements = m_upgradeRequirements.AddItem(new UpgradeRequirements(zdoVar, message, requirements, station, level, range)).ToArray();
        }

        public void AddSpentUpgrades(Dictionary<int, Piece.Requirement[]> upgradeReqs)
        {
            ZDO zdo = m_nview.GetZDO();
            foreach (UpgradeRequirements upgradeRequirements in m_upgradeRequirements)
                upgradeRequirements.AddSpentUpgrades(zdo, upgradeReqs);
        }

        private bool IsPositionToInteract()
        {
            return Ship.GetLocalShip() == m_ship
                && Vector3.Dot(transform.position - Player.m_localPlayer.GetEyePoint(), transform.position - Utils.GetMainCamera().transform.position) > 0
                && Vector3.Dot(transform.position - Player.m_localPlayer.transform.position, transform.position - Utils.GetMainCamera().transform.position) > 0;
        }
    }
}
