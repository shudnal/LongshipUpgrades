﻿using HarmonyLib;
using System.Text;
using UnityEngine;

namespace LongshipUpgrades
{
    public class LongshipPartController : MonoBehaviour, Hoverable, Interactable
    {
        public ZNetView m_nview;
        public string m_name = "Part";

        public bool m_checkGuardStone = true;
        public float m_useDistance = 0f;

        public string m_messageAdd = "Add";
        public string m_messageChange = "Change";
        public string m_messageRemove = "Remove";

        public int m_zdoPartUpgraded;
        public int m_zdoPartUpgradedLvl2;
        public int m_zdoPartActive;
        public int m_zdoPartVariant;

        public int m_variants;

        public Piece.Requirement[] m_requirements = new Piece.Requirement[0];
        public Piece.Requirement[] m_requirementsLvl2 = new Piece.Requirement[0];

        private static readonly StringBuilder sb = new StringBuilder(10);
        private static Recipe tempRecipe;

        public void Awake()
        {
            if (tempRecipe == null)
            {
                tempRecipe = ScriptableObject.CreateInstance<Recipe>();
                tempRecipe.name = "LongshipPartUpgradeTempRecipe";
            }
        }

        public string GetHoverText()
        {
            if (!InUseDistance(Player.m_localPlayer))
                return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");

            if (m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
                return Localization.instance.Localize(m_name + "\n$piece_noaccess");

            ZDO zdo = m_nview.GetZDO();

            if (m_zdoPartUpgradedLvl2 != 0 && !zdo.GetBool(m_zdoPartUpgradedLvl2))
            {
                sb.Clear();
                sb.Append(m_name);
                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_upgradebutton");

                if (m_requirementsLvl2.Length > 0)
                {
                    sb.Append("\n");
                    sb.Append("\n$hud_require");
                    m_requirementsLvl2.Do(req => sb.AppendFormat("\n{0}", req.m_resItem?.m_itemData.m_shared.m_name));
                }

                return Localization.instance.Localize(sb.ToString());
            }

            if (m_zdoPartUpgraded != 0 && !zdo.GetBool(m_zdoPartUpgraded))
            {
                sb.Clear();
                sb.Append(m_name);
                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_upgradebutton");

                if (m_requirements.Length > 0)
                {
                    sb.Append("\n");
                    sb.Append("\n$hud_require");
                    m_requirements.Do(req => sb.AppendFormat("\n{0}", req.m_resItem?.m_itemData.m_shared.m_name));
                }
                return Localization.instance.Localize(sb.ToString());
            }

            if (m_zdoPartVariant != 0 && m_variants > 0)
            {
                sb.Clear();
                sb.Append(m_name);
                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $hud_switchitem");
                return Localization.instance.Localize(sb.ToString());
            }

            if (m_zdoPartActive != 0)
            {
                sb.Clear();
                sb.Append(m_name);
                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] ");
                sb.Append(zdo.GetBool(m_zdoPartActive) ? m_messageRemove : m_messageAdd);
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

            if (m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position))
                return false;

            if (!InUseDistance(human))
                return false;

            ZDO zdo = m_nview.GetZDO();
            if (m_zdoPartUpgraded != 0 && !zdo.GetBool(m_zdoPartUpgraded))
            {
                if (RemoveRequiredItems(human as Player, m_requirements))
                    return SetUpgraded(zdo, m_zdoPartUpgraded);

                human.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                return false;
            }

            if (m_zdoPartUpgradedLvl2 != 0 && !zdo.GetBool(m_zdoPartUpgradedLvl2))
            {
                if (RemoveRequiredItems(human as Player, m_requirementsLvl2))
                    return SetUpgraded(zdo, m_zdoPartUpgradedLvl2);

                human.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                return false;
            }

            if (m_zdoPartVariant != 0 && m_variants > 1)
            {
                zdo.Set(m_zdoPartVariant, (zdo.GetInt(m_zdoPartVariant) + 1) % m_variants);
                return true;
            }

            if (m_zdoPartActive == 0)
                return false;

            zdo.Set(m_zdoPartActive, !zdo.GetBool(m_zdoPartActive));
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

        private static bool SetUpgraded(ZDO zdo, int hash)
        {
            zdo.Set(hash, true);
            // Play build effect

            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
            playerProfile.IncrementStat(PlayerStatType.CraftsOrUpgrades);
            playerProfile.IncrementStat(PlayerStatType.Upgrades);

            return true;
        }

        public static bool RemoveRequiredItems(Player player, Piece.Requirement[] requirements)
        {
            if (requirements.Length == 0)
                return true;

            if (player.NoCostCheat() || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))
                return true;

            tempRecipe.m_resources = requirements;
            if (!player.HaveRequirementItems(tempRecipe, false, 1))
                return false;

            player.ConsumeResources(requirements, 1);
            return true;
        }
    }
}