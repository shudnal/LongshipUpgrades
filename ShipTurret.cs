using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static LongshipUpgrades.LongshipUpgrades;

namespace LongshipUpgrades
{
    public class ShipTurret : MonoBehaviour, Hoverable, Interactable
    {
        public string m_name = "$piece_turret";
        public ZNetView m_nview;

        [Header("Turret")]
        public GameObject m_turretBody;

        public GameObject m_turretBodyArmed;

        public GameObject m_turretBodyUnarmed;

        public GameObject m_turretNeck;

        public GameObject m_eye;

        [Header("Look & Scan")]
        public float m_turnRate = 50f;

        public float m_horizontalAngle = 60f;

        public float m_verticalAngle = 70f;

        public float m_viewDistance = 30f;

        public float m_noTargetScanRate = 7f;

        public float m_lookAcceleration = 1.4f;

        public float m_lookDeacceleration = 0.05f;

        public float m_lookMinDegreesDelta = 0.005f;

        [Header("Attack Settings (rest in projectile)")]
        public ItemDrop m_defaultAmmo;

        public float m_attackCooldown = 2f;

        public float m_attackWarmup = 1f;

        public float m_hitNoise = 10f;

        public float m_shootWhenAimDiff = 0.99998f;

        public float m_predictionModifier = 2f;

        public float m_updateTargetIntervalNear = 1f;

        public float m_updateTargetIntervalFar = 4f;

        [Header("Ammo")]
        public int m_maxAmmo = 100;

        public string m_ammoType = "$ammo_turretbolt";

        public List<Turret.AmmoType> m_allowedAmmo = new List<Turret.AmmoType>();

        public bool m_returnAmmoOnDestroy = true;

        public float m_holdRepeatInterval = 0.2f;

        [Header("Target mode: Everything")]
        public bool m_targetEnemies = true;

        [Header("Target mode: Configured")]
        public bool m_targetTamedConfig = true;

        public List<Turret.TrophyTarget> m_configTargets = new List<Turret.TrophyTarget>();

        public int m_maxConfigTargets = 1;

        public GameObject m_lastProjectile;

        public ItemDrop.ItemData m_lastAmmo;

        public Character m_target;

        public bool m_haveTarget;

        public Quaternion m_baseBodyRotation;

        public Quaternion m_baseNeckRotation;

        public Quaternion m_lastRotation;

        public float m_aimDiffToTarget;

        public float m_updateTargetTimer;

        public float m_lastUseTime;

        public float m_scan;

        public readonly List<ItemDrop> m_targetItems = new List<ItemDrop>();

        public readonly List<Character> m_targetCharacters = new List<Character>();

        public string m_targetsText;

        public readonly StringBuilder sb = new StringBuilder(20);

        public uint m_lastUpdateTargetRevision = uint.MaxValue;

        public int m_scanDirection = 1;

        public float m_scanDistance = 1f;

        public bool m_isLeftTurret;

        public static readonly int s_lastAttackLeft = "lastAttackLeft".GetStableHashCode();
        public static readonly int s_lastAttackRight = "lastAttackRight".GetStableHashCode();
        public static readonly int s_targetsLeft = "targetsLeft".GetStableHashCode();
        public static readonly int s_targetsRight = "targetsRight".GetStableHashCode();
        
        [Header("Effects")]
        public static EffectList m_shootEffect = new EffectList();
        public static EffectList m_addAmmoEffect = new EffectList();
        public static EffectList m_reloadEffect = new EffectList();
        public static EffectList m_warmUpStartEffect = new EffectList();
        public static EffectList m_newTargetEffect = new EffectList();
        public static EffectList m_lostTargetEffect = new EffectList();
        public static EffectList m_setTargetEffect = new EffectList();

        public ShipTurret SetPositionAtShip(bool isLeft)
        {
            m_isLeftTurret = isLeft;

            if (m_isLeftTurret)
            {
                m_scanDirection = -1;
                m_scan = m_noTargetScanRate * 0.05f;

                WearNTear wnt = GetComponentInParent<WearNTear>();
                if (wnt)
                    wnt.m_onDestroyed = (Action)Delegate.Combine(wnt.m_onDestroyed, new Action(OnDestroyed));
            }

            if ((bool)m_nview && m_nview.IsValid())
            {
                m_nview.Register<ZDOID>(GetNameSetTargetRPC(), RPC_SetTarget);
                
                if (m_isLeftTurret)
                    m_nview.Register<string>("RPC_AddAmmo", RPC_AddAmmo);
            }

            return this;
        }

        public void FillAllowedAmmo(List<Turret.AmmoType> turretList)
        {
            foreach (Turret.AmmoType allowedAmmo in turretList)
            {
                m_allowedAmmo.Add(new Turret.AmmoType()
                {
                    m_ammo = allowedAmmo.m_ammo,
                    m_visual = m_turretBody.transform.Find(allowedAmmo.m_visual.name).gameObject
                });
            }

            if (m_nview && m_nview.IsValid())
                UpdateVisualBolt();
        }

        public void Awake()
        {
            m_nview = GetComponentInParent<ZNetView>();

            m_turretBody = transform.Find("BodyRotation").gameObject;
            m_turretBodyArmed = transform.Find("BodyRotation/Body").gameObject;
            m_turretBodyUnarmed = transform.Find("BodyRotation/Body_Unarmed").gameObject;
            m_turretNeck = transform.Find("NeckRotation").gameObject;
            m_eye = transform.Find("BodyRotation/Eye").gameObject;

            m_updateTargetTimer = UnityEngine.Random.Range(0f, m_updateTargetIntervalNear);
            m_baseBodyRotation = m_turretBody.transform.localRotation;
            m_baseNeckRotation = m_turretNeck.transform.localRotation;

            ReadTargets();
        }

        public void FixedUpdate()
        {
            float fixedDeltaTime = Time.fixedDeltaTime;
            UpdateReloadState();
            if (!m_nview.IsValid())
                return;

            UpdateTurretRotation(fixedDeltaTime);
            UpdateVisualBolt();
            if (!m_nview.IsOwner())
            {
                if (m_nview.IsValid() && m_lastUpdateTargetRevision != m_nview.GetZDO().DataRevision)
                {
                    m_lastUpdateTargetRevision = m_nview.GetZDO().DataRevision;
                    ReadTargets();
                }
            }
            else
            {
                UpdateTarget(fixedDeltaTime);
                UpdateAttack(fixedDeltaTime);
            }
        }

        public void UpdateTurretRotation(float dt)
        {
            if (IsCoolingDown())
                return;

            bool isReadyToShoot = false;
            Vector3 forward;

            if (HasAmmoAndEnabled())
            {
                isReadyToShoot = (bool)m_target;
                if (isReadyToShoot)
                {
                    if (m_lastAmmo == null)
                        m_lastAmmo = GetAmmoItem();

                    if (m_lastAmmo == null)
                    {
                        LogWarning("Turret had invalid ammo, resetting ammo");
                        m_nview.GetZDO().Set(ZDOVars.s_ammo, 0);
                        return;
                    }

                    float timeToHit = Vector2.Distance(m_target.transform.position, m_eye.transform.position) / m_lastAmmo.m_shared.m_attack.m_projectileVel;
                    Vector3 targetPoint = m_target.GetVelocity() * timeToHit * m_predictionModifier;
                    forward = m_target.transform.position + targetPoint - m_turretBody.transform.position;

                    CapsuleCollider componentInChildren = m_target.GetComponentInChildren<CapsuleCollider>();
                    forward.y += (componentInChildren ? componentInChildren.height / 2f : 1f);
                }
                else
                {
                    m_scan += dt;
                    if (m_scan > m_noTargetScanRate * 2f)
                    {
                        m_scan = 0f;
                        m_scanDistance = UnityEngine.Random.Range(0.5f, 1f);
                    }

                    forward = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y + (float)((m_scan - m_noTargetScanRate > 0f) ? m_scanDirection : (-m_scanDirection)) * m_horizontalAngle * m_scanDistance, 0f) * Vector3.forward;
                }
            }
            else
            {
                forward = base.transform.forward + new Vector3(0f, -0.3f, 0f);
            }

            forward.Normalize();
            Quaternion quaternion = Quaternion.LookRotation(forward, Vector3.up);
            Vector3 eulerAngles = quaternion.eulerAngles;
            float y2 = base.transform.rotation.eulerAngles.y;
            eulerAngles.y -= y2;
            if (m_horizontalAngle >= 0f)
            {
                float num3 = eulerAngles.y;
                if (num3 > 180f)
                    num3 -= 360f;
                else if (num3 < -180f)
                    num3 += 360f;

                if (num3 > m_horizontalAngle)
                {
                    eulerAngles = new Vector3(eulerAngles.x, m_horizontalAngle + y2, eulerAngles.z);
                    quaternion.eulerAngles = eulerAngles;
                }
                else if (num3 < 0f - m_horizontalAngle)
                {
                    eulerAngles = new Vector3(eulerAngles.x, 0f - m_horizontalAngle + y2, eulerAngles.z);
                    quaternion.eulerAngles = eulerAngles;
                }
            }

            Quaternion quaternion2 = Utils.RotateTorwardsSmooth(m_turretBody.transform.rotation, quaternion, m_lastRotation, m_turnRate * dt, m_lookAcceleration, m_lookDeacceleration, m_lookMinDegreesDelta);
            m_lastRotation = m_turretBody.transform.rotation;
            m_turretBody.transform.rotation = m_baseBodyRotation * quaternion2;
            m_turretNeck.transform.rotation = m_baseNeckRotation * Quaternion.Euler(0f, m_turretBody.transform.rotation.eulerAngles.y, m_turretBody.transform.rotation.eulerAngles.z);
            m_aimDiffToTarget = isReadyToShoot ? Math.Abs(Quaternion.Dot(quaternion2, quaternion)) : (-1f);
        }

        public void UpdateTarget(float dt)
        {
            if (!m_nview.IsValid())
            {
                return;
            }

            if (!HasAmmoAndEnabled())
            {
                if (m_haveTarget)
                {
                    m_nview.InvokeRPC(ZNetView.Everybody, GetNameSetTargetRPC(), ZDOID.None);
                }

                return;
            }

            m_updateTargetTimer -= dt;
            if (m_updateTargetTimer <= 0f)
            {
                m_updateTargetTimer = (Character.IsCharacterInRange(base.transform.position, 40f) ? m_updateTargetIntervalNear : m_updateTargetIntervalFar);
                Character character = BaseAI.FindClosestCreature(base.transform, m_eye.transform.position, 
                                                                 hearRange: 0f, viewRange: m_viewDistance, viewAngle: m_horizontalAngle, 
                                                                 alerted: false, mistVision: false, passiveAggresive: true, 
                                                                 includePlayers: false, includeTamed: (m_targetItems.Count > 0) && m_targetTamedConfig, includeEnemies: m_targetEnemies, 
                                                                 m_targetCharacters);
                if (character != m_target)
                {
                    if ((bool)character)
                    {
                        m_newTargetEffect.Create(base.transform.position, base.transform.rotation);
                    }
                    else
                    {
                        m_lostTargetEffect.Create(base.transform.position, base.transform.rotation);
                    }

                    m_nview.InvokeRPC(ZNetView.Everybody, GetNameSetTargetRPC(), character ? character.GetZDOID() : ZDOID.None);
                }
            }

            if (m_haveTarget && (!m_target || m_target.IsDead()))
            {
                LogInfo("Target is gone");
                m_nview.InvokeRPC(ZNetView.Everybody, GetNameSetTargetRPC(), ZDOID.None);
                m_lostTargetEffect.Create(base.transform.position, base.transform.rotation);
            }
        }

        public void UpdateAttack(float dt)
        {
            if ((bool)m_target && !(m_aimDiffToTarget < m_shootWhenAimDiff) && HasAmmoAndEnabled() && !IsCoolingDown())
            {
                ShootProjectile();
            }
        }

        public void ShootProjectile()
        {
            Transform transform = m_eye.transform;
            m_shootEffect.Create(transform.position, transform.rotation);
            m_nview.GetZDO().Set(GetLastAttackZDO(), (float)ZNet.instance.GetTimeSeconds());
            m_lastAmmo = GetAmmoItem();
            int @int = GetAmmo();
            int num = Mathf.Min(1, (m_maxAmmo == 0) ? m_lastAmmo.m_shared.m_attack.m_projectiles : Mathf.Min(@int, m_lastAmmo.m_shared.m_attack.m_projectiles));
            if (m_maxAmmo > 0)
            {
                m_nview.GetZDO().Set(ZDOVars.s_ammo, @int - num);
            }

            LogInfo($"Turret '{base.name}' is shooting {num} projectiles, ammo: {@int}/{m_maxAmmo}");
            for (int i = 0; i < num; i++)
            {
                Vector3 forward = transform.forward;
                Vector3 axis = Vector3.Cross(forward, Vector3.up);
                float projectileAccuracy = m_lastAmmo.m_shared.m_attack.m_projectileAccuracy;
                Quaternion quaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - projectileAccuracy, projectileAccuracy), Vector3.up);
                forward = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - projectileAccuracy, projectileAccuracy), axis) * forward;
                forward = quaternion * forward;
                m_lastProjectile = UnityEngine.Object.Instantiate(m_lastAmmo.m_shared.m_attack.m_attackProjectile, transform.position, transform.rotation);
                HitData hitData = new HitData();
                hitData.m_toolTier = (short)m_lastAmmo.m_shared.m_toolTier;
                hitData.m_pushForce = m_lastAmmo.m_shared.m_attackForce;
                hitData.m_backstabBonus = m_lastAmmo.m_shared.m_backstabBonus;
                hitData.m_staggerMultiplier = m_lastAmmo.m_shared.m_attack.m_staggerMultiplier;
                hitData.m_damage.Add(m_lastAmmo.GetDamage());
                hitData.m_statusEffectHash = (m_lastAmmo.m_shared.m_attackStatusEffect ? m_lastAmmo.m_shared.m_attackStatusEffect.NameHash() : 0);
                hitData.m_blockable = m_lastAmmo.m_shared.m_blockable;
                hitData.m_dodgeable = m_lastAmmo.m_shared.m_dodgeable;
                hitData.m_skill = m_lastAmmo.m_shared.m_skillType;
                hitData.m_itemWorldLevel = (byte)Game.m_worldLevel;
                hitData.m_hitType = HitData.HitType.Turret;
                if (m_lastAmmo.m_shared.m_attackStatusEffect != null)
                {
                    hitData.m_statusEffectHash = m_lastAmmo.m_shared.m_attackStatusEffect.NameHash();
                }

                m_lastProjectile.GetComponent<IProjectile>()?.Setup(null, forward * m_lastAmmo.m_shared.m_attack.m_projectileVel, m_hitNoise, hitData, null, m_lastAmmo);
            }
        }

        public bool IsCoolingDown()
        {
            if (!m_nview.IsValid())
            {
                return false;
            }

            return (double)(m_nview.GetZDO().GetFloat(GetLastAttackZDO()) + m_attackCooldown) > ZNet.instance.GetTimeSeconds();
        }

        public bool HasAmmoAndEnabled()
        {
            return !IsDeactivated() && HasAmmo();
        }

        public bool IsDeactivated()
        {
            return m_nview.GetZDO().GetBool(LongshipCustomizableParts.s_turretsDisabled);
        }

        public bool HasAmmo()
        {
            if (m_maxAmmo != 0)
            {
                return GetAmmo() > 0;
            }

            return true;
        }

        public int GetAmmo()
        {
            return m_nview.GetZDO().GetInt(ZDOVars.s_ammo);
        }

        public string GetAmmoType()
        {
            if (!m_defaultAmmo)
            {
                return m_nview.GetZDO().GetString(ZDOVars.s_ammoType);
            }

            return m_defaultAmmo.name;
        }

        public void UpdateReloadState()
        {
            bool flag = IsCoolingDown();
            if (!m_turretBodyArmed.activeInHierarchy && !flag)
            {
                m_reloadEffect.Create(base.transform.position, base.transform.rotation);
            }

            m_turretBodyArmed.SetActive(!flag);
            m_turretBodyUnarmed.SetActive(flag);
        }

        public ItemDrop.ItemData GetAmmoItem()
        {
            string ammoType = GetAmmoType();
            GameObject prefab = ZNetScene.instance.GetPrefab(ammoType);
            if (!prefab)
            {
                LogWarning("Turret '" + base.name + "' is trying to fire but has no ammo or default ammo!");
                return null;
            }

            return prefab.GetComponent<ItemDrop>().m_itemData;
        }

        public string GetHoverText()
        {
            if (!m_nview.IsValid())
                return "";

            if (!m_targetEnemies)
                return Localization.instance.Localize(m_name);

            if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
                return Localization.instance.Localize(m_name + "\n$piece_noaccess");

            sb.Clear();
            sb.Append(m_name);
            if (IsDeactivated())
                sb.Append(" disabled");

            if (HasAmmo())
                sb.AppendFormat(" ({0} / {1})", GetAmmo(), m_maxAmmo);
            else
                sb.Append(" ($piece_turret_noammo)");

            if (m_targetCharacters.Count == 0)
            {
                sb.Append("\n$piece_turret_target $piece_turret_target_everything");
            }
            else
            {
                sb.Append("\n$piece_turret_target ");
                sb.Append(m_targetsText);
            }

            sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_turret_addammo\n[<color=yellow><b>1-8</b></color>] $piece_turret_target_set");
            return Localization.instance.Localize(sb.ToString());
        }

        public string GetHoverName()
        {
            return m_name;
        }

        public bool Interact(Humanoid character, bool hold, bool alt)
        {
            if (hold)
            {
                if (m_holdRepeatInterval <= 0f)
                    return false;

                if (Time.time - m_lastUseTime < m_holdRepeatInterval)
                    return false;
            }

            m_lastUseTime = Time.time;
            return UseItem(character, null);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (item == null)
            {
                item = FindAmmoItem(user.GetInventory(), onlyCurrentlyLoadableType: true);
                if (item == null)
                {
                    if (GetAmmo() > 0 && FindAmmoItem(user.GetInventory(), onlyCurrentlyLoadableType: false) != null)
                    {
                        ItemDrop component = ZNetScene.instance.GetPrefab(GetAmmoType()).GetComponent<ItemDrop>();
                        user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_turretotherammo ") + Localization.instance.Localize(component.m_itemData.m_shared.m_name));
                        return false;
                    }

                    user.Message(MessageHud.MessageType.Center, "$msg_noturretammo");
                    return false;
                }
            }

            foreach (Turret.TrophyTarget configTarget in m_configTargets)
            {
                if (!(item.m_shared.m_name == configTarget.m_item.m_itemData.m_shared.m_name))
                {
                    continue;
                }

                if (m_targetItems.Contains(configTarget.m_item))
                {
                    m_targetItems.Remove(configTarget.m_item);
                }
                else
                {
                    if (m_targetItems.Count >= m_maxConfigTargets)
                    {
                        m_targetItems.RemoveAt(0);
                    }

                    m_targetItems.Add(configTarget.m_item);
                }

                SetTargets();
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$piece_turret_target_set_msg " + ((m_targetCharacters.Count == 0) ? "$piece_turret_target_everything" : m_targetsText)));
                m_setTargetEffect.Create(base.transform.position, base.transform.rotation);
                Game.instance.IncrementPlayerStat(PlayerStatType.TurretTrophySet);
                return true;
            }

            if (!IsItemAllowed(item.m_dropPrefab.name))
            {
                user.Message(MessageHud.MessageType.Center, "$msg_wontwork");
                return false;
            }

            if (GetAmmo() > 0 && GetAmmoType() != item.m_dropPrefab.name)
            {
                ItemDrop component2 = ZNetScene.instance.GetPrefab(GetAmmoType()).GetComponent<ItemDrop>();
                user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_turretotherammo ") + Localization.instance.Localize(component2.m_itemData.m_shared.m_name));
                return false;
            }

            LogInfo("trying to add ammo " + item.m_shared.m_name);
            if (GetAmmo() >= m_maxAmmo)
            {
                user.Message(MessageHud.MessageType.Center, "$msg_itsfull");
                return false;
            }

            user.Message(MessageHud.MessageType.Center, "$msg_added " + item.m_shared.m_name);

            if (!(user as Player).NoCostCheat() && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))
                user.GetInventory().RemoveItem(item, 1);

            Game.instance.IncrementPlayerStat(PlayerStatType.TurretAmmoAdded);
            m_nview.InvokeRPC("RPC_AddAmmo", item.m_dropPrefab.name);
            return true;
        }

        public void RPC_AddAmmo(long sender, string name)
        {
            if (m_nview.IsOwner())
            {
                if (!IsItemAllowed(name))
                {
                    LogInfo("Item not allowed " + name);
                    return;
                }

                m_nview.GetZDO().Set(ZDOVars.s_ammo, GetAmmo() + 1);
                m_nview.GetZDO().Set(ZDOVars.s_ammoType, name);

                foreach (ShipTurret turret in transform.parent.GetComponentsInChildren<ShipTurret>())
                    m_addAmmoEffect.Create(turret.m_turretBody.transform.position, turret.m_turretBody.transform.rotation);

                UpdateVisualBolt();
                LogInfo("Added ammo " + name);
            }
        }

        public void RPC_SetTarget(long sender, ZDOID character)
        {
            GameObject gameObject = ZNetScene.instance.FindInstance(character);
            if ((bool)gameObject)
            {
                Character component = gameObject.GetComponent<Character>();
                if (component)
                {
                    m_target = component;
                    m_haveTarget = true;
                    return;
                }
            }

            m_target = null;
            m_haveTarget = false;
            m_scan = 0f;
        }

        public void UpdateVisualBolt()
        {
            string ammoType = GetAmmoType();
            bool flag = HasAmmo() && !IsCoolingDown();

            foreach (Turret.AmmoType item in m_allowedAmmo)
            {
                bool flag2 = item.m_ammo.name == ammoType;
                item.m_visual.SetActive(flag2 && flag);
            }
        }

        public bool IsItemAllowed(string itemName)
        {
            foreach (Turret.AmmoType item in m_allowedAmmo)
                if (item.m_ammo.name == itemName)
                    return true;

            return false;
        }

        public ItemDrop.ItemData FindAmmoItem(Inventory inventory, bool onlyCurrentlyLoadableType)
        {
            if (onlyCurrentlyLoadableType && HasAmmo())
                return inventory.GetAmmoItem(m_ammoType, GetAmmoType());

            return inventory.GetAmmoItem(m_ammoType);
        }

        public void OnDestroyed()
        {
            if (m_nview.IsOwner() && m_returnAmmoOnDestroy)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(GetAmmoType());
                for (int i = 0; i < GetAmmo(); i++)
                {
                    Vector3 position = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
                    Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
                    Instantiate(prefab, position, rotation);
                }
            }
        }

        public void SetTargets()
        {
            if (!m_nview.IsOwner())
                m_nview.ClaimOwnership();

            m_nview.GetZDO().Set(GetTargetsZDO(), m_targetItems.Count);
            for (int i = 0; i < m_targetItems.Count; i++)
                m_nview.GetZDO().Set(GetTargetZDO(i), m_targetItems[i].m_itemData.m_shared.m_name);

            ReadTargets();
        }

        public string GetNameSetTargetRPC()
        {
            return m_isLeftTurret ? "RPC_SetTargetLeft" : "RPC_SetTargetRight";
        }

        public int GetTargetsZDO()
        {
            return m_isLeftTurret ? s_targetsLeft : s_targetsRight;
        }

        public int GetLastAttackZDO()
        {
            return m_isLeftTurret ? s_lastAttackLeft : s_lastAttackRight;
        }

        public string GetTargetZDO(int i)
        {
            return (m_isLeftTurret ? "targetleft" : "targetright") + i;
        }

        public void ReadTargets()
        {
            if (!m_nview || !m_nview.IsValid())
                return;

            m_targetItems.Clear();
            m_targetCharacters.Clear();
            m_targetsText = "";
            int @int = m_nview.GetZDO().GetInt(GetTargetsZDO());
            for (int i = 0; i < @int; i++)
            {
                string @string = m_nview.GetZDO().GetString(GetTargetZDO(i));
                foreach (Turret.TrophyTarget configTarget in m_configTargets)
                {
                    if (!(configTarget.m_item.m_itemData.m_shared.m_name == @string))
                    {
                        continue;
                    }

                    m_targetItems.Add(configTarget.m_item);
                    m_targetCharacters.AddRange(configTarget.m_targets);
                    if (m_targetsText.Length > 0)
                    {
                        m_targetsText += ", ";
                    }

                    if (!string.IsNullOrEmpty(configTarget.m_nameOverride))
                    {
                        m_targetsText += configTarget.m_nameOverride;
                        break;
                    }

                    for (int j = 0; j < configTarget.m_targets.Count; j++)
                    {
                        m_targetsText += configTarget.m_targets[j].m_name;
                        if (j + 1 < configTarget.m_targets.Count)
                        {
                            m_targetsText += ", ";
                        }
                    }

                    break;
                }
            }
        }
    }
}