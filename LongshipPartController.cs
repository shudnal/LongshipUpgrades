using HarmonyLib;
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

        public string m_messageEnable = "Add";
        public string m_messageSwitch = "Switch";
        public string m_messageDisable = "Remove";
        public string m_messageUpgrade = "";
        public string m_messageUpgradeLvl2 = "";

        public int m_zdoPartUpgraded;
        public int m_zdoPartUpgradedLvl2;
        public int m_zdoPartDisabled;
        public int m_zdoPartVariant;

        public int m_variants;

        public Piece.Requirement[] m_requirements = new Piece.Requirement[0];
        public Piece.Requirement[] m_requirementsLvl2 = new Piece.Requirement[0];

        private static readonly StringBuilder sb = new StringBuilder(12);
        private static Recipe tempRecipe;

        public void Awake()
        {
            if (tempRecipe == null)
            {
                tempRecipe = ScriptableObject.CreateInstance<Recipe>();
                tempRecipe.name = "LongshipPartUpgradeTempRecipe";
            }
        }

        public void Start()
        {
            if (m_nview == null)
                m_nview = GetComponent<ZNetView>();
        }

        public string GetHoverText()
        {
            if (!InUseDistance(Player.m_localPlayer))
                return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");

            if (m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
                return Localization.instance.Localize(m_name + "\n$piece_noaccess");

            if (!m_nview || !m_nview.IsValid())
                return "";

            ZDO zdo = m_nview.GetZDO();

            if (m_zdoPartUpgraded != 0 && !zdo.GetBool(m_zdoPartUpgraded))
            {
                sb.Clear();
                sb.Append(m_name);
                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_upgradebutton");

                if (!string.IsNullOrWhiteSpace(m_messageUpgrade))
                    sb.AppendFormat("\n<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGBA(LongshipUpgrades.hintColor.Value), m_messageUpgrade);

                if (m_requirements.Length > 0)
                {
                    sb.Append("\n");
                    sb.Append("\n$hud_require");
                    m_requirements.Do(req => sb.AppendFormat("\n{0}", req.m_resItem?.m_itemData.m_shared.m_name));
                }
                return Localization.instance.Localize(sb.ToString());
            }

            if (m_zdoPartUpgradedLvl2 != 0 && !zdo.GetBool(m_zdoPartUpgradedLvl2))
            {
                sb.Clear();
                sb.Append(m_name);
                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_upgradebutton");

                if (!string.IsNullOrWhiteSpace(m_messageUpgradeLvl2))
                    sb.AppendFormat("\n<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGBA(LongshipUpgrades.hintColor.Value), m_messageUpgradeLvl2);

                if (m_requirementsLvl2.Length > 0)
                {
                    sb.Append("\n");
                    sb.Append("\n$hud_require");
                    m_requirementsLvl2.Do(req => sb.AppendFormat("\n{0}", req.m_resItem?.m_itemData.m_shared.m_name));
                }

                return Localization.instance.Localize(sb.ToString());
            }

            if (m_zdoPartVariant != 0 && m_variants > 0)
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

            if (m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position))
                return false;

            if (!InUseDistance(human))
                return false;

            if (!m_nview || !m_nview.IsValid())
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

            if (m_zdoPartDisabled == 0)
                return false;

            zdo.Set(m_zdoPartDisabled, !zdo.GetBool(m_zdoPartDisabled));
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
