using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LongshipUpgrades
{
    public class ShipTrophyStand : MonoBehaviour, Hoverable, Interactable
    {
        public string m_name = "$guardianstone_hook_name";

        public Transform m_attachOther;

        public Transform m_dropSpawnPoint;

        public EffectList m_effects = new EffectList();

        public EffectList m_destroyEffects = new EffectList();

        [Header("Guardian power")]
        public float m_powerActivationDelay = 2f;

        public StatusEffect m_guardianPower;

        public EffectList m_activatePowerEffects = new EffectList();

        public EffectList m_activatePowerEffectsPlayer = new EffectList();

        public string m_visualName = "";

        public int m_visualVariant;

        public GameObject m_visualItem;

        public string m_currentItemName = "";

        public ItemDrop.ItemData m_queuedItem;

        public ZNetView m_nview;

        public static ItemDrop.ItemData.ItemType supportedItemType = ItemDrop.ItemData.ItemType.Trophy;

        private static readonly List<ItemDrop.ItemData> tempItems = new List<ItemDrop.ItemData>();

        private static readonly StringBuilder sb = new StringBuilder(20);

        private static readonly Dictionary<string, SE_Stats> trophyEffects = new Dictionary<string, SE_Stats>();

        public void Awake()
        {
            m_nview = GetComponentInParent<ZNetView>();
            if (m_nview.IsValid())
            {
                m_nview.Register("DropItem", RPC_DropItem);
                m_nview.Register("RequestOwn", RPC_RequestOwn);
                m_nview.Register("DestroyAttachment", RPC_DestroyAttachment);
                m_nview.Register<string, int, int>("SetVisualItem", RPC_SetVisualItem);
                InvokeRepeating("UpdateVisual", 1f, 4f);
            }

            if (trophyEffects.Count == 0)
                FillTrophyEffects();
        }

        public string GetHoverText()
        {
            if (!Player.m_localPlayer)
                return "";

            if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
                return Localization.instance.Localize(m_name + "\n$piece_noaccess");

            sb.Clear();

            if (HaveAttachment())
            {
                sb.Append(m_currentItemName);
                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_itemstand_take");
                
                if (m_guardianPower != null)
                {
                    if (IsInvoking("DelayedPowerActivation"))
                        return "";

                    sb.Append("\n");
                    sb.AppendFormat("\n<color=orange>{0}</color>", m_guardianPower.m_name);

                    if (IsGuardianPowerActive(Player.m_localPlayer))
                        sb.Append("\n$guardianstone_hook_power_activate");
                    else if (IsGuardianPowerOnCooldown(Player.m_localPlayer))
                        sb.Append("\n$hud_powernotready");
                    else
                    {
                        string altKey = !ZInput.IsNonClassicFunctionality() || !ZInput.IsGamepadActive() ? "$KEY_AltPlace" : "$KEY_JoyAltKeys";
                        sb.AppendFormat("\n[<color=yellow><b>{0} + $KEY_Use</b></color>] $guardianstone_hook_activate", altKey);
                    }

                    sb.Append("\n\n");
                    sb.Append(m_guardianPower.GetTooltipString());
                }
            }
            else
            {
                sb.Append(m_name);
                sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>][<color=yellow><b>1-8</b></color>] $piece_itemstand_attach");
            }

            return Localization.instance.Localize(sb.ToString());
        }

        public string GetHoverName()
        {
            return m_name;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            if (!PrivateArea.CheckAccess(base.transform.position))
                return true;

            if (HaveAttachment())
            {
                if (!alt)
                {
                    m_nview.InvokeRPC("DropItem");
                    return true;
                }

                if (m_guardianPower != null)
                {
                    if (IsInvoking("DelayedPowerActivation"))
                        return false;

                    if (IsGuardianPowerActive(user))
                        return false;
                    else if (IsGuardianPowerOnCooldown(user as Player))
                        return false;

                    user.Message(MessageHud.MessageType.Center, "$guardianstone_hook_power_activate");
                    m_activatePowerEffects.Create(base.transform.position, base.transform.rotation);
                    m_activatePowerEffectsPlayer.Create(user.transform.position, Quaternion.identity, user.transform);
                    Invoke("DelayedPowerActivation", m_powerActivationDelay);
                    return true;
                }
            }
            else
            {
                user.GetInventory().GetAllItems(supportedItemType, tempItems);
                if (tempItems.Count > 0)
                {
                    UseItem(user, tempItems[0]);
                    return true;
                }

                user.Message(MessageHud.MessageType.Center, "$piece_itemstand_missingitem");
            }

            return false;
        }

        public bool IsGuardianPowerActive(Humanoid user)
        {
            return user.GetSEMan().HaveStatusEffect(m_guardianPower.m_nameHash);
        }

        public bool IsGuardianPowerOnCooldown(Player player)
        {
            return player != null && player.GetGuardianPowerName() != "" && player.m_guardianPowerCooldown > 0;
        }

        public void DelayedPowerActivation()
        {
            Player player = Player.m_localPlayer;
            if (player && m_guardianPower != null)
            {
                if (player.GetGuardianPowerName() == "")
                    player.SetGuardianPower(m_guardianPower.name);

                player.GetSEMan().AddStatusEffect(m_guardianPower);
                player.m_guardianPowerCooldown = m_guardianPower.m_cooldown;
            }
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (HaveAttachment())
                return false;

            if (!CanAttach(item))
            {
                user.Message(MessageHud.MessageType.Center, "$piece_itemstand_cantattach");
                return true;
            }

            if (!m_nview.IsOwner())
                m_nview.InvokeRPC("RequestOwn");

            m_queuedItem = item;
            CancelInvoke("UpdateAttach");
            InvokeRepeating("UpdateAttach", 0f, 0.1f);
            return true;
        }

        public void RPC_DropItem(long sender)
        {
            if (m_nview.IsOwner())
                DropItem();
        }

        public void DestroyAttachment()
        {
            m_nview.InvokeRPC("DestroyAttachment");
        }

        public void RPC_DestroyAttachment(long sender)
        {
            if (m_nview.IsOwner() && HaveAttachment())
            {
                m_nview.GetZDO().Set(ZDOVars.s_item, "");
                m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", "", 0, 0);
                m_destroyEffects.Create(m_dropSpawnPoint.position, Quaternion.identity);
            }
        }

        public void DropItem()
        {
            if (!HaveAttachment())
                return;

            string @string = m_nview.GetZDO().GetString(ZDOVars.s_item);
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(@string);
            if ((bool)itemPrefab)
            {
                Vector3 vector = Vector3.zero;
                Quaternion quaternion = Quaternion.identity;
                Transform transform = itemPrefab.transform.Find("attach");
                if ((bool)itemPrefab.transform.Find("attachobj") && (bool)transform)
                {
                    quaternion = transform.transform.localRotation;
                    vector = transform.transform.localPosition;
                }

                GameObject obj = Instantiate(itemPrefab, m_dropSpawnPoint.position + vector, m_dropSpawnPoint.rotation * quaternion);
                obj.GetComponent<ItemDrop>().LoadFromExternalZDO(m_nview.GetZDO());
                obj.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
                m_effects.Create(m_dropSpawnPoint.position, Quaternion.identity);
            }

            m_nview.GetZDO().Set(ZDOVars.s_item, "");
            m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", "", 0, 0);
        }

        public Transform GetAttach(ItemDrop.ItemData item)
        {
            return m_attachOther;
        }

        public void UpdateAttach()
        {
            if (m_nview.IsOwner())
            {
                CancelInvoke("UpdateAttach");
                Player localPlayer = Player.m_localPlayer;
                if (m_queuedItem != null && localPlayer != null && localPlayer.GetInventory().ContainsItem(m_queuedItem) && !HaveAttachment())
                {
                    ItemDrop.ItemData itemData = m_queuedItem.Clone();
                    itemData.m_stack = 1;
                    m_nview.GetZDO().Set(ZDOVars.s_item, m_queuedItem.m_dropPrefab.name);
                    ItemDrop.SaveToZDO(itemData, m_nview.GetZDO());
                    localPlayer.UnequipItem(m_queuedItem);
                    localPlayer.GetInventory().RemoveOneItem(m_queuedItem);
                    m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", itemData.m_dropPrefab.name, itemData.m_variant, itemData.m_quality);
                    Transform attach = GetAttach(m_queuedItem);
                    m_effects.Create(attach.transform.position, Quaternion.identity);
                    Game.instance.IncrementPlayerStat(PlayerStatType.ItemStandUses);
                }

                m_queuedItem = null;
            }
        }

        public void RPC_RequestOwn(long sender)
        {
            if (m_nview.IsOwner())
                m_nview.GetZDO().SetOwner(sender);
        }

        public void UpdateVisual()
        {
            if (!(m_nview == null) && m_nview.IsValid())
            {
                string @string = m_nview.GetZDO().GetString(ZDOVars.s_item);
                int @int = m_nview.GetZDO().GetInt(ZDOVars.s_variant);
                int int2 = m_nview.GetZDO().GetInt(ZDOVars.s_quality, 1);
                SetVisualItem(@string, @int, int2);
            }
        }

        public void RPC_SetVisualItem(long sender, string itemName, int variant, int quality)
        {
            SetVisualItem(itemName, variant, quality);
        }

        public void SetVisualItem(string itemName, int variant, int quality)
        {
            if (m_visualName == itemName && m_visualVariant == variant)
                return;

            m_visualName = itemName;
            m_visualVariant = variant;
            m_currentItemName = "";
            m_guardianPower = trophyEffects.TryGetValue(itemName, out SE_Stats gpower) ? gpower : null;
            if (m_visualName == "")
            {
                Destroy(m_visualItem);
                return;
            }

            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
            if (itemPrefab == null)
            {
                LongshipUpgrades.LogWarning("Missing item prefab " + itemName);
                return;
            }

            GameObject attachPrefab = GetAttachPrefab(itemPrefab);
            if (attachPrefab == null)
            {
                LongshipUpgrades.LogWarning("Failed to get attach prefab for item " + itemName);
                return;
            }

            ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
            m_currentItemName = component.m_itemData.m_shared.m_name;
            Transform attach = GetAttach(component.m_itemData);
            GameObject attachGameObject = GetAttachGameObject(attachPrefab);
            m_visualItem = Instantiate(attachGameObject, attach.position, attach.rotation, attach);
            m_visualItem.transform.localPosition = attachPrefab.transform.localPosition;
            m_visualItem.transform.localRotation = attachPrefab.transform.localRotation;

            m_visualItem.transform.localScale = Vector3.Scale(attachPrefab.transform.localScale, component.m_itemData.GetScale(quality));
            if (TryFindScaleOverride(itemName, out float scale))
                m_visualItem.transform.localScale = Vector3.one * scale;

            m_visualItem.GetComponentInChildren<IEquipmentVisual>()?.Setup(m_visualVariant);

            m_visualItem.GetComponentsInChildren<Collider>().DoIf(collider => collider.enabled && !collider.isTrigger, Destroy);
        }

        public bool CanAttach(ItemDrop.ItemData item)
        {
            return GetAttachPrefab(item.m_dropPrefab) != null && item.m_shared.m_itemType == supportedItemType;
        }

        public bool HaveAttachment()
        {
            return GetAttachedItem() != "";
        }

        public string GetAttachedItem()
        {
            return m_nview.IsValid() ? m_nview.GetZDO().GetString(ZDOVars.s_item) : "";
        }

        internal static bool TryFindScaleOverride(string itemName, out float scale)
        {
            foreach (string rescale in LongshipUpgrades.itemStandTrophyRescale.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] req = rescale.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (req.Length != 2)
                    continue;

                if (req[0] == itemName && float.TryParse(req[1], out scale))
                    return true;
            };

            scale = 0f;
            return false;
        }

        public static GameObject GetAttachPrefab(GameObject item)
        {
            return item.transform.Find("attach")?.gameObject;
        }

        public static GameObject GetAttachGameObject(GameObject prefab)
        {
            return prefab.transform.Find("attachobj")?.gameObject ?? prefab;
        }

        internal static void FillTrophyEffects()
        {
            trophyEffects.Clear();
            foreach (ItemStand itemstand in Resources.FindObjectsOfTypeAll<ItemStand>().Where(itemstand => itemstand.m_guardianPower is SE_Stats && itemstand.m_supportedItems.Count == 1 && itemstand.m_supportedItems[0] != null))
                trophyEffects[itemstand.m_supportedItems[0].name] = itemstand.m_guardianPower as SE_Stats;
        }
    }
}
