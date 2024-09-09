using UnityEngine;

namespace LongshipUpgrades
{
    internal class LongshipPartController : MonoBehaviour, Hoverable, Interactable
    {
        public ZNetView m_nview;
        public string m_name = "Part";

        public float m_useDistance = 2f;

        public string m_messageAdd = "Add";
        public string m_messageRemove = "Remove";

        public int m_zdoPartUpgraded;
        public int m_zdoPartActive;

        public Piece.Requirement[] m_requirements;

        public string GetHoverText()
        {
            if (!InUseDistance(Player.m_localPlayer))
            {
                return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
            }

            ZDO zdo = m_nview.GetZDO();

            if (!zdo.GetBool(m_zdoPartUpgraded))
                return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_upgradebutton");

            return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + (zdo.GetBool(m_zdoPartActive) ? m_messageRemove : m_messageAdd));
        }

        public string GetHoverName()
        {
            return m_name;
        }

        public bool Interact(Humanoid human, bool hold, bool alt)
        {
            if (hold)
                return false;

            if (!InUseDistance(human))
                return false;

            ZDO zdo = m_nview.GetZDO();
            if (!zdo.GetBool(m_zdoPartUpgraded))
            {
                if (RemoveRequiredItems(human as Player, m_requirements))
                {
                    zdo.Set(m_zdoPartUpgraded, true);
                    // Play build effect
                    return true;
                }

                human.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                return false;
            }

            zdo.Set(m_zdoPartActive, !zdo.GetBool(m_zdoPartActive));
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public bool InUseDistance(Humanoid human)
        {
            return Vector3.Distance(human.transform.position, transform.position) < m_useDistance;
        }

        public static bool RemoveRequiredItems(Player player, Piece.Requirement[] requirements)
        {
            if (requirements == null)
                return true;

            return true;
        }
    }
}
