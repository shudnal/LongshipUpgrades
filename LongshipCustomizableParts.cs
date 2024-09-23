using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LongshipUpgrades.LongshipUpgrades;

namespace LongshipUpgrades
{
    public class LongshipCustomizableParts : MonoBehaviour
    {
        public static bool prefabInit = false;

        private Ship m_ship;
        private ZNetView m_nview;
        private ZDO m_zdo;
        private Container m_container;
        private bool m_customMast;
        private WearNTear m_wnt;

        private GameObject m_beamMast;
        private GameObject m_beamTent;
        private GameObject m_beamMesh;
        private GameObject m_beamTentCollider;
        private GameObject m_beamSailCollider;
        private GameObject m_insects;
        private GameObject m_fireWarmth;
        private GameObject m_tent;
        private GameObject m_lantern;
        private GameObject m_holdersRight;
        private GameObject m_holdersLeft;
        private GameObject m_mastUpgrade;
        private GameObject m_storageUpgrade;
        private GameObject m_healthUpgrade;
        private GameObject m_shieldsStyles;
        private GameObject m_headStyles;
        private GameObject m_turrets;
        private GameObject m_turretsUpgrade;
        private GameObject m_itemstandObject;

        private GameObject m_mast;
        private GameObject m_ropes;
        private GameObject m_sail;

        private MeshRenderer m_lampRenderer;
        private Light m_light;
        private ParticleSystem m_flare;
        private ShipTrophyStand m_trophyStand;

        private GameObject[] m_containerPartsLvl1;
        private GameObject[] m_containerPartsLvl2;
        private GameObject[] m_protectiveParts;
        private GameObject[] m_heads;

        private bool m_containerUpgradedLvl1;
        private bool m_containerUpgradedLvl2;
        private bool m_healthUpgraded;
        private bool m_ashlandsUpgraded;
        private bool m_isLampLightDisabled;
        private int m_headStyle;
        private int m_shieldsStyle;
        private int m_tentStyle;
        private int m_sailStyle;

        private GameObject m_destroyedLootPrefab;

        public const string prefabName = "VikingShip";
        public const string turretName = "piece_turret";
        public const string moderBossStone = "BossStone_DragonQueen";
        private static bool prefabFixed = false;

        private static Shader shaderStandard;
        private static Material standSharedMaterial;
        private static Material storageSharedMaterial;
        private static Material plankSharedMaterial;

        private static Color flareColor;
        private static bool isNightTime;
        private static bool isTimeToLight = true;

        public static readonly int s_mastUpgraded = "MastUpgraded".GetStableHashCode();
        public static readonly int s_mastRemoved = "MastRemoved".GetStableHashCode();
        public static readonly int s_lanternUpgraded = "LanternUpgraded".GetStableHashCode();
        public static readonly int s_lanternDisabled = "LanternDisabled".GetStableHashCode();
        public static readonly int s_tentUpgraded = "TentUpgraded".GetStableHashCode();
        public static readonly int s_tentDisabled = "TentDisabled".GetStableHashCode();
        public static readonly int s_lightsDisabled = "LightDisabled".GetStableHashCode();

        public static readonly int s_containerUpgradedLvl1 = "ContainerUpgradedLvl1".GetStableHashCode();
        public static readonly int s_containerUpgradedLvl2 = "ContainerUpgradedLvl2".GetStableHashCode();

        public static readonly int s_healthUpgraded = "HealthUpgraded".GetStableHashCode();
        public static readonly int s_ashlandsUpgraded = "AshlandsUpgraded".GetStableHashCode();

        public static readonly int s_turretsUpgraded = "TurretsUpgraded".GetStableHashCode();
        public static readonly int s_turretsDisabled = "TurretsDisabled".GetStableHashCode();

        public static readonly int s_headStyle = "HeadStyle".GetStableHashCode();
        public static readonly int s_shieldsStyle = "ShieldsStyle".GetStableHashCode();
        public static readonly int s_tentStyle = "TentStyle".GetStableHashCode();
        public static readonly int s_sailStyle = "SailStyle".GetStableHashCode();

        public bool blocksIsDirty;
        public readonly Dictionary<Renderer, MaterialPropertyBlock> s_propertyBlocks = new Dictionary<Renderer, MaterialPropertyBlock>();

        public static Texture2D s_ashlandsHull = new Texture2D(2, 2);

        public const int piece_nonsolid = 16;
        public const int vehicle = 28;

        public static List<Texture2D> customTents = new List<Texture2D>();
        public static List<Texture2D> customSails = new List<Texture2D>();
        public static List<Texture2D> customShields = new List<Texture2D>();

        public static EffectList fabricSwitchEffects = new EffectList();
        public static EffectList woodSwitchEffects = new EffectList();
        public static EffectList lampEnableEffects = new EffectList();
        public static EffectList lampDisableEffects = new EffectList();
        public static EffectList turretsEnableEffects = new EffectList();
        public static EffectList turretsDisableEffects = new EffectList();

        private static readonly Dictionary<GameObject, LongshipCustomizableParts> s_allInstances = new Dictionary<GameObject, LongshipCustomizableParts>();

        private void Awake()
        {
            if (prefabInit)
                return;

            m_ship = GetComponent<Ship>();
            m_wnt = GetComponent<WearNTear>();

            m_nview = GetComponent<ZNetView>();
            m_zdo = m_nview?.GetZDO();

            enabled = m_zdo != null;

            m_container = GetComponentsInChildren<Container>().Where(container => container.gameObject.name == "piece_chest").FirstOrDefault();
            m_destroyedLootPrefab = m_container?.m_destroyedLootPrefab;

            if (m_wnt)
                m_wnt.m_onDestroyed = (Action)Delegate.Combine(m_wnt.m_onDestroyed, new Action(OnDestroyed));

            CheckEffects();

            s_allInstances.Add(base.gameObject, this);
        }

        private void Start()
        {
            if (prefabInit)
                return;

            InitializeParts();
        }

        public void OnDestroy()
        {
            if (prefabInit)
                return;

            s_allInstances.Remove(base.gameObject);
        }

        private void FixedUpdate()
        {
            if (prefabInit)
                return;

            if (m_zdo == null || !m_ship)
                return;

            m_customMast = mastEnabled.Value && m_zdo.GetBool(s_mastUpgraded);
            m_mastUpgrade?.SetActive((mastEnabled.Value && !m_customMast) || mastRemovable.Value);

            m_mast?.SetActive(!m_zdo.GetBool(s_mastRemoved));
            m_ropes?.SetActive(m_mast.activeSelf);

            m_beamTent?.SetActive(m_customMast && (lanternEnabled.Value || tentEnabled.Value));
            m_beamMast?.SetActive(!m_mast.activeSelf && m_beamTent != null && m_beamTent.activeInHierarchy);
            m_beamSailCollider?.SetActive((!m_beamMast || !m_beamMast.activeSelf) && m_mast.activeSelf);

            m_tent?.SetActive(m_customMast && tentEnabled.Value && m_zdo.GetBool(s_tentUpgraded) && !m_zdo.GetBool(s_tentDisabled));
            m_lantern?.SetActive(lanternEnabled.Value && m_customMast && m_zdo.GetBool(s_lanternUpgraded) && !m_zdo.GetBool(s_lanternDisabled));

            m_holdersRight?.SetActive(m_tent && m_tent.activeInHierarchy);
            m_holdersLeft?.SetActive(m_tent && m_tent.activeInHierarchy);

            m_fireWarmth?.SetActive(tentHeat.Value && m_lantern && m_lantern.activeInHierarchy && m_tent && m_tent.activeInHierarchy);

            m_turretsUpgrade?.SetActive(turretsEnabled.Value);
            m_turrets?.SetActive(turretsEnabled.Value && m_zdo.GetBool(s_turretsUpgraded));

            m_itemstandObject?.SetActive(itemStandEnabled.Value);

            bool timeChanged = false;
            if (m_light != null && m_lantern && m_lantern.activeInHierarchy && (m_isLampLightDisabled != m_zdo.GetBool(s_lightsDisabled) || (timeChanged = isTimeToLight != IsTimeToLight()) || isNightTime != IsNightTime()))
            {         
                isNightTime = IsNightTime();
                isTimeToLight = IsTimeToLight();

                if (lanternAutoSwtich.Value && timeChanged)
                    m_zdo.Set(s_lightsDisabled, !isTimeToLight);

                m_isLampLightDisabled = m_zdo.GetBool(s_lightsDisabled);
                UpdateLights();
            }

            if (m_beamMesh)
            {
                m_beamMesh.transform.localPosition = new Vector3(0f, 0f, (m_tent && m_tent.activeInHierarchy) ? 0f : -0.9f);
                m_beamMesh.transform.localScale = new Vector3(1f, 1f, (m_tent && m_tent.activeInHierarchy) ? 1f : 0.5f);

                m_beamTentCollider.transform.localPosition = new Vector3(0f, 1.58f, (m_tent && m_tent.activeInHierarchy) ?  0.23f : -0.49f);
                m_beamTentCollider.transform.localScale = new Vector3(0.16f, 0.16f, (m_tent && m_tent.activeInHierarchy) ? 2.05f : 0.6f);
            }

            if (containerEnabled.Value && m_storageUpgrade && m_containerUpgradedLvl2 != m_zdo.GetBool(s_containerUpgradedLvl2))
            {
                m_containerUpgradedLvl2 = m_zdo.GetBool(s_containerUpgradedLvl2);
                m_containerPartsLvl2?.Do(part => part?.SetActive(m_containerUpgradedLvl2));
                
                if (m_containerUpgradedLvl2 && m_container.m_height < containerHeight.Value)
                    m_container.m_height = containerHeight.Value;

                if (m_containerUpgradedLvl2 && m_container.GetInventory().GetHeight() < containerHeight.Value)
                    m_container.GetInventory().m_height = containerHeight.Value;
            }

            if (containerEnabled.Value && m_storageUpgrade && m_containerUpgradedLvl1 != m_zdo.GetBool(s_containerUpgradedLvl1))
            {
                m_containerUpgradedLvl1 = m_zdo.GetBool(s_containerUpgradedLvl1);
                m_containerPartsLvl1?.Do(part => part?.SetActive(m_containerUpgradedLvl1));

                if (m_containerUpgradedLvl1 && m_container.m_width < containerWidth.Value)
                    m_container.m_width = containerWidth.Value;

                if (m_containerUpgradedLvl1 && m_container.GetInventory().GetWidth() < containerWidth.Value)
                    m_container.GetInventory().m_width = containerWidth.Value;
            }

            m_storageUpgrade?.SetActive(containerEnabled.Value && !m_containerUpgradedLvl2);

            if (healthEnabled.Value && m_protectiveParts != null && m_wnt && m_healthUpgraded != m_zdo.GetBool(s_healthUpgraded))
            {
                m_healthUpgraded = m_zdo.GetBool(s_healthUpgraded);
                m_protectiveParts.Do(part => part?.SetActive(m_healthUpgraded));

                if (m_healthUpgraded && m_wnt.m_health < healthUpgradeLvl1.Value)
                    m_wnt.m_health = healthUpgradeLvl1.Value;
            }

            if (healthEnabled.Value && m_wnt && m_ashlandsUpgraded != m_zdo.GetBool(s_ashlandsUpgraded))
            {
                m_ashlandsUpgraded = m_zdo.GetBool(s_ashlandsUpgraded);

                if (m_ashlandsUpgraded && healthUpgradeLvl2.Value > 0)
                {
                    if (m_wnt.m_health < healthUpgradeLvl2.Value)
                        m_wnt.m_health = healthUpgradeLvl2.Value;

                    if (ashlandsProtection.Value)
                    {
                        m_ship.m_ashlandsReady = true;

                        m_wnt.m_ashDamageResist = true;
                        m_wnt.m_damages.Apply(new List<HitData.DamageModPair> { new HitData.DamageModPair() { m_type = HitData.DamageType.Fire, m_modifier = HitData.DamageModifier.VeryResistant } });
                    }
                }

                UpdateHullPropertyBlocks();
            }

            m_healthUpgrade?.SetActive(healthEnabled.Value && !m_ashlandsUpgraded);

            if (changeShields.Value && healthEnabled.Value && m_protectiveParts != null && m_healthUpgraded && m_shieldsStyle != m_zdo.GetInt(s_shieldsStyle))
            {
                m_shieldsStyle = m_zdo.GetInt(s_shieldsStyle);
                if (maxShields.Value != 0 && m_shieldsStyle > maxShields.Value)
                {
                    m_shieldsStyle %= maxShields.Value;
                    m_zdo.Set(s_shieldsStyle, m_shieldsStyle);
                }

                UpdateShieldsPropertyBlocks();
            }

            m_shieldsStyles?.SetActive(m_healthUpgraded);

            if (changeTent.Value && m_tent != null && m_tent.activeInHierarchy && m_tentStyle != m_zdo.GetInt(s_tentStyle))
            {
                m_tentStyle = m_zdo.GetInt(s_tentStyle);

                UpdateTentPropertyBlocks();
            }

            if (changeSail.Value && m_sail != null && m_sail.activeInHierarchy && m_sailStyle != m_zdo.GetInt(s_sailStyle))
            {
                m_sailStyle = m_zdo.GetInt(s_sailStyle);

                UpdateSailPropertyBlocks();
            }

            if (changeHead.Value && m_heads != null && m_wnt && m_headStyle != m_zdo.GetInt(s_headStyle))
            {
                m_headStyle = m_zdo.GetInt(s_headStyle);

                for (int i = 0; i < m_heads.Length; i++)
                    m_heads[i].SetActive(m_headStyle == i + 1);

                foreach (GameObject head in new GameObject[] { m_wnt.m_new.transform.Find("skull_head").gameObject,
                                                               m_wnt.m_worn.transform.Find("skull_head").gameObject })
                    head.SetActive(m_headStyle == 0);
            }

            m_headStyles?.SetActive(changeHead.Value);

            SetPropertyBlocks();
        }

        private void UpdateSailPropertyBlocks()
        {
            Renderer renderer = m_sail.GetComponentInChildren<Renderer>();
            if (m_sailStyle == 0 || customSails.Count == 0)
                ResetPropertyBlock(renderer);
            else
                SetPropertyBlock(renderer, ShaderProps._MainTex, customSails[(m_sailStyle - 1) % (maxSails.Value == 0 ? customSails.Count : Math.Min(maxSails.Value, customSails.Count))]);
        }

        private void UpdateTentPropertyBlocks()
        {
            Renderer renderer = m_tent.GetComponentInChildren<Renderer>();
            if (m_tentStyle == 0 || customTents.Count == 0)
                ResetPropertyBlock(renderer);
            else
                SetPropertyBlock(renderer, ShaderProps._MainTex, customTents[(m_tentStyle - 1) % (maxTents.Value == 0 ? customTents.Count : Math.Min(maxTents.Value, customTents.Count))]);
        }

        private void UpdateHullPropertyBlocks()
        {
            if (m_ashlandsUpgraded && healthUpgradeLvl2.Value > 0)
            {
                if (ashlandsProtection.Value)
                {
                    foreach (Renderer renderer in new List<Renderer> {
                                                            m_wnt.m_new.transform.Find("hull").gameObject.GetComponent<Renderer>(),
                                                            m_wnt.m_worn.transform.Find("hull").gameObject.GetComponent<Renderer>(),
                                                            m_wnt.m_new.transform.Find("skull_head").gameObject.GetComponent<Renderer>(),
                                                            m_wnt.m_worn.transform.Find("skull_head").gameObject.GetComponent<Renderer>()})
                    {
                        SetPropertyBlock(renderer, ShaderProps._MainTex, s_ashlandsHull);
                    }

                    if (m_heads != null)
                        foreach (Renderer renderer in m_heads.Select(head => head.GetComponent<Renderer>()))
                            SetPropertyBlock(renderer, ShaderProps._MainTex, s_ashlandsHull);
                }
            }

        }

        private void UpdateShieldsPropertyBlocks()
        {
            if (changeShields.Value && healthEnabled.Value && m_protectiveParts != null && m_healthUpgraded)
            {
                int style = m_shieldsStyle > 3 ? (m_shieldsStyle - 1) % 3 + 1 : m_shieldsStyle;
                int texId = m_shieldsStyle > 3 ? (m_shieldsStyle - 4) / 3 : -1;
                foreach (Renderer renderer in m_protectiveParts.Select(head => head.GetComponent<Renderer>()))
                {
                    ResetPropertyBlock(renderer);
                    SetPropertyBlock(renderer, ShaderProps._Style, style);

                    if (-1 < texId && texId < customShields.Count)
                        SetPropertyBlock(renderer, LongshipPropertyBlocks._StyleTex, customShields[texId]);
                }
            }
        }

        private void UpdatePropertyBlocks()
        {
            s_propertyBlocks.Keys.Do(renderer => renderer.SetPropertyBlock(null));
            s_propertyBlocks.Clear();

            UpdateHullPropertyBlocks();

            UpdateShieldsPropertyBlocks();

            UpdateTentPropertyBlocks();

            UpdateSailPropertyBlocks();

            SetPropertyBlocks();
        }

        private void UpdateLights()
        {
            m_insects?.SetActive(isNightTime && !m_isLampLightDisabled);

            m_light.gameObject.SetActive(!m_isLampLightDisabled);
            m_light.color = lanternLightColor.Value;

            Color onlyColor = lanternLightColor.Value;
            onlyColor.a = 0f;

            if (m_flare)
            {
                m_flare.gameObject.SetActive(!m_isLampLightDisabled);

                ParticleSystem.MainModule main = m_flare.main;
                main.startColor = Color.Lerp(flareColor, onlyColor, 0.5f);
            }

            if (m_lampRenderer)
                SetPropertyBlock(m_lampRenderer, ShaderProps._EmissionColor, m_isLampLightDisabled ? Color.grey : Color.white + onlyColor);
        }

        private void SetPropertyBlock(Renderer renderer, int nameID, Texture2D tex)
        {
            GetPropertyBlock(renderer).SetTexture(nameID, tex);
        }

        private void SetPropertyBlock(Renderer renderer, int nameID, int style)
        {
            GetPropertyBlock(renderer).SetInt(nameID, style);
        }

        private void SetPropertyBlock(Renderer renderer, int nameID, Color color)
        {
            GetPropertyBlock(renderer).SetColor(nameID, color);
        }

        private void ResetPropertyBlock(Renderer renderer)
        {
            if (!s_propertyBlocks.ContainsKey(renderer))
                return;

            renderer.SetPropertyBlock(null);
            s_propertyBlocks.Remove(renderer);
            blocksIsDirty = true;
        }

        private MaterialPropertyBlock GetPropertyBlock(Renderer renderer)
        {
            blocksIsDirty = true;

            if (!s_propertyBlocks.TryGetValue(renderer, out MaterialPropertyBlock propertyBlock))
            {
                propertyBlock = new MaterialPropertyBlock();
                s_propertyBlocks[renderer] = propertyBlock;
            }

            return propertyBlock;
        }

        private void CombinePropertyBlocks(MaterialPropertyBlock propertyBlockToCombine)
        {
            foreach (MaterialPropertyBlock matBlock in s_propertyBlocks.Values)
            {
                if (propertyBlockToCombine.HasColor(ShaderProps._Color))
                {
                    matBlock.SetColor(ShaderProps._Color, propertyBlockToCombine.GetColor(ShaderProps._Color));
                    blocksIsDirty = true;
                }
                    
                if (propertyBlockToCombine.HasColor(ShaderProps._EmissionColor))
                {
                    matBlock.SetColor(ShaderProps._EmissionColor, propertyBlockToCombine.GetColor(ShaderProps._EmissionColor));
                    blocksIsDirty = true;
                }
            }

            SetPropertyBlocks();
        }

        private void SetPropertyBlocks()
        {
            if (blocksIsDirty)
                s_propertyBlocks.Do(rendererBlock => rendererBlock.Key.SetPropertyBlock(rendererBlock.Value.isEmpty ? null : rendererBlock.Value));

            blocksIsDirty = false;
        }

        private void InitializeParts()
        {
            m_mast = transform.Find("ship/visual/Mast").gameObject;
            m_ropes = transform.Find("ship/visual/ropes").gameObject;
            m_sail = transform.Find("ship/visual/Mast/Sail/sail_full").gameObject;

            Transform customize = transform.Find("ship/visual/Customize");
            if (!customize)
                return;

            customize.gameObject.SetActive(true);

            m_holdersRight = customize.Find("ShipTentHolders").gameObject;
            m_holdersLeft = customize.Find("ShipTentHolders (1)").gameObject;

            m_holdersRight.SetActive(false);
            m_holdersLeft.SetActive(false);

            Transform storage = customize.Find("storage");
            if (storage)
            {
                List<GameObject> barrels = new List<GameObject>();
                List<GameObject> boxes = new List<GameObject>();
                List<GameObject> shields = new List<GameObject>();

                for (int i = 0; i < storage.childCount; i++)
                {
                    GameObject go = storage.GetChild(i).gameObject;
                    if (go.name.StartsWith("barrel"))
                        barrels.Add(go);
                    else if (go.name.StartsWith("Shield"))
                        shields.Add(go);
                    else
                        boxes.Add(go);

                    go.SetActive(false);
                }

                m_containerPartsLvl1 = barrels.ToArray();
                m_containerPartsLvl2 = boxes.ToArray();
                m_protectiveParts = shields.ToArray();

                UnityEngine.Random.State state = UnityEngine.Random.state;
                UnityEngine.Random.InitState((int)m_zdo.m_uid.ID);

                for (int i = 0; i < barrels.Count; i++)
                {
                    barrels[i].transform.localEulerAngles += new Vector3(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    switch (i)
                    {
                        case 0:
                            barrels[i].transform.localPosition = new Vector3(-0.69f, 0.18f, -0.88f);
                            break;
                        case 1:
                            barrels[i].transform.localPosition = new Vector3(-0.36f, 0.18f, -0.87f);
                            break;
                    }

                    BoxCollider box = barrels[i].GetComponentInChildren<BoxCollider>();
                    GameObject model = box.gameObject;
                    Destroy(box);
                    model.AddComponent<CapsuleCollider>();
                }

                for (int i = 0; i < boxes.Count; i++)
                {
                    switch (i)
                    {
                        case 0:
                        case 5:
                            Transform targetPoint = new GameObject("ladder_TargetPoint").transform;
                            targetPoint.SetParent(boxes[i].transform, worldPositionStays: false);
                            targetPoint.localPosition = new Vector3(0.4f, 0.82f, 0f);
                            targetPoint.localEulerAngles = new Vector3(0f, 270f, 0f);

                            Ladder ladder = boxes[i].AddComponent<Ladder>();
                            ladder.m_useDistance = 1.5f;
                            ladder.m_targetPos = targetPoint;
                            ladder.m_name = "$lu_box_name";
                            continue;
                        case 1:
                            boxes[i].transform.localPosition = new Vector3(0.08f, 0f, -0.45f);
                            break;
                        case 3:
                            boxes[i].transform.localPosition = new Vector3(0f, 0f, -0.84f);
                            boxes[i].transform.localScale = new Vector3(0.35f, 0.4f, 0.35f);
                            break;
                        case 4:
                            boxes[i].transform.localPosition = new Vector3(0.36f, -0.02f, -0.82f);
                            boxes[i].transform.localScale = new Vector3(0.35f, 0.4f, 0.35f);
                            break;
                        case 6:
                            boxes[i].transform.localPosition = new Vector3(0.00f, 0.26f, -0.57f);
                            break;
                        case 8:
                            boxes[i].transform.localScale = new Vector3(0.35f, 0.4f, 0.35f);
                            break;
                    }

                    boxes[i].transform.localEulerAngles += new Vector3(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                }

                UnityEngine.Random.state = state;
            }

            Transform beam = customize.Find("ShipTen2_beam");
            if (beam)
            {
                m_beamMast = Instantiate(beam.gameObject, beam.parent);
                m_beamMast.name = "ShipTen2_mast";
                m_beamMast.transform.localEulerAngles += new Vector3(90f, 0.1f, 0f);
                m_beamMast.transform.localPosition = new Vector3(0.58f, 0.35f, 0f);
                m_beamMast.SetActive(false);

                beam.localPosition += new Vector3(0.1f, 0f, 0f);
                m_beamTent = beam.gameObject;
                m_beamTent.SetActive(false);

                m_beamMesh = beam.GetComponentInChildren<Renderer>().gameObject;

                Transform mastBeamCollider = AddCollider(m_beamMast.transform, "mast_beam", typeof(BoxCollider));
                mastBeamCollider.localPosition = new Vector3(0f, 1.58f, -0.48f);
                mastBeamCollider.localScale = new Vector3(0.16f, 0.16f, 2.5f);

                Transform lanternBeamCollider = AddCollider(beam, "lantern_beam", typeof(BoxCollider));
                lanternBeamCollider.localPosition = new Vector3(0f, 1.58f, -1.41f);
                lanternBeamCollider.localScale = new Vector3(0.16f, 0.16f, 0.8f);

                Transform tentBeamCollider = AddCollider(beam, "tent_beam", typeof(BoxCollider));
                tentBeamCollider.localPosition = new Vector3(0f, 1.58f, 0.23f);
                tentBeamCollider.localScale = new Vector3(0.16f, 0.16f, 2.05f);

                Transform sailBeamCollider = AddCollider(beam, "sail_beam", typeof(BoxCollider));
                sailBeamCollider.localPosition = new Vector3(0f, 1.4f, -0.9f);
                sailBeamCollider.localScale = new Vector3(0.23f, 0.55f, 0.2f);

                m_beamTentCollider = tentBeamCollider.gameObject;

                m_beamSailCollider = sailBeamCollider.gameObject;

                if (lanternEnabled.Value)
                {
                    LongshipPartController lanternController = lanternBeamCollider.gameObject.AddComponent<LongshipPartController>();
                    lanternController.m_name = "$lu_part_lantern_name";
                    lanternController.m_zdoPartDisabled = lanternRemovable.Value ? s_lanternDisabled : 0;
                    lanternController.m_messageEnable = "$lu_part_lantern_enable";
                    lanternController.m_enableEffects = woodSwitchEffects;
                    lanternController.m_messageDisable = "$lu_part_lantern_disable";
                    lanternController.m_disableEffects = woodSwitchEffects;

                    lanternController.m_nview = m_nview;
                    lanternController.m_useDistance = 3f;
                    lanternController.AddUpgradeRequirement(s_lanternUpgraded,
                                                            "$lu_part_lantern_upgrade", 
                                                            ParseRequirements(lanternUpgradeRecipe.Value),
                                                            lanternStation.Value,
                                                            lanternStationLvl.Value,
                                                            lanternStationRange.Value);
                }

                if (tentEnabled.Value)
                {
                    LongshipPartController tentController = tentBeamCollider.gameObject.AddComponent<LongshipPartController>();
                    tentController.m_name = "$lu_part_tent_name";
                    tentController.m_zdoPartDisabled = tentRemovable.Value ? s_tentDisabled : 0;
                    tentController.m_messageEnable = "$lu_part_tent_enable";
                    tentController.m_enableEffects = fabricSwitchEffects;
                    tentController.m_messageDisable = "$lu_part_tent_disable";
                    tentController.m_disableEffects = fabricSwitchEffects;
                    tentController.m_nview = m_nview;
                    tentController.m_useDistance = 3f;
                    tentController.AddUpgradeRequirement(s_tentUpgraded,
                                                         "$lu_part_tent_upgrade",
                                                         ParseRequirements(tentUpgradeRecipe.Value),
                                                         tentStation.Value,
                                                         tentStationLvl.Value,
                                                         tentStationRange.Value);
                }

                if (changeSail.Value)
                {
                    LongshipPartController sailController = sailBeamCollider.gameObject.AddComponent<LongshipPartController>();
                    sailController.m_name = "$lu_part_sail_name";
                    sailController.m_nview = m_nview;
                    sailController.m_messageSwitch = "$lu_part_sail_switch";
                    sailController.m_zdoPartVariant = s_sailStyle;
                    sailController.m_variants = customSails.Count + 1;
                    sailController.m_switchEffects = fabricSwitchEffects;
                }
            }

            Transform tent = customize.Find("ShipTen2 (1)");
            if (tent)
            {
                m_tent = tent.gameObject;
                Transform tentColliders = new GameObject("colliders").transform;
                tentColliders.SetParent(m_tent.transform, worldPositionStays: false);

                Transform tentCollider = AddCollider(tentColliders, "collider_right", typeof(BoxCollider));
                tentCollider.localPosition = new Vector3(1.58f, 1.18f, -0.65f);
                tentCollider.localScale = new Vector3(1.9f, 0.01f, 2.6f);
                tentCollider.localEulerAngles = new Vector3(0f, 0f, -6f);

                Transform tentCollider1 = AddCollider(tentColliders, "collider_left", typeof(BoxCollider));
                tentCollider1.localPosition = new Vector3(-1.05f, 0.95f, -0.65f);
                tentCollider1.localScale = new Vector3(1f, 0.01f, 2.5f);
                tentCollider1.localEulerAngles = new Vector3(0f, 0f, 23f);

                Transform tentCollider2 = AddCollider(tentColliders, "collider_left (1)", typeof(BoxCollider));
                tentCollider2.localPosition = new Vector3(-2.1f, 0.7f, -0.55f);
                tentCollider2.localScale = new Vector3(1.15f, 0.01f, 3f);
                tentCollider2.localEulerAngles = new Vector3(0f, 0f, 6f);

                m_tent.SetActive(false);

                if (changeTent.Value)
                    for (int i = 0; i < tentColliders.childCount; i++)
                    {
                        LongshipPartController tentController = tentColliders.GetChild(i).gameObject.AddComponent<LongshipPartController>();
                        tentController.m_name = "$lu_part_tent_name";
                        tentController.m_nview = m_nview;
                        tentController.m_messageSwitch = "$lu_part_tent_switch";
                        tentController.m_zdoPartVariant = s_tentStyle;
                        tentController.m_variants = customTents.Count + 1;
                        tentController.m_switchEffects = fabricSwitchEffects;
                    }
            }

            GameObject lanternItem = ObjectDB.instance.GetItemPrefab("Lantern")?.transform.Find("attach/equiped")?.gameObject;
            if (lanternItem)
            {
                m_lantern = new GameObject("Lantern")
                {
                    layer = vehicle
                };

                Transform lanternParent = m_lantern.transform;
                lanternParent.SetParent(customize, worldPositionStays: false);
                lanternParent.localScale = Vector3.one * 0.45f;

                Transform lantern = Instantiate(lanternItem, lanternParent).transform;
                lantern.name = "Lamp";
                lantern.localPosition = new Vector3(0.23f, 1.9f, 0f);
                lantern.gameObject.layer = vehicle;

                m_light = lantern.GetComponentInChildren<Light>();
                m_light.color = lanternLightColor.Value;

                m_flare = lantern.Find("flare").GetComponent<ParticleSystem>();
                
                if (flareColor == Color.clear)
                    flareColor = m_flare.main.startColor.color;

                m_lampRenderer = lantern.Find("default").GetComponent<MeshRenderer>();

                ConfigurableJoint joint = lantern.GetComponent<ConfigurableJoint>();
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedBody = GetComponent<Rigidbody>();
                joint.connectedAnchor = new Vector3(0f, 3.01f, -0.45f);

                Transform lanternCollider = AddCollider(lanternParent, "collider", typeof(BoxCollider));
                lanternCollider.localPosition = new Vector3(0.2f, 2f, 0f);
                lanternCollider.localScale = new Vector3(0.02f, 0.02f, 0.5f);
                lanternCollider.localEulerAngles = new Vector3(90f, 0f, 0f);

                Transform lamp = customize.Find("TraderLamp");
                if (lamp)
                {
                    lamp.gameObject.SetActive(false);

                    Transform insects = lamp.Find("insects");

                    if (insects)
                    {
                        m_insects = Instantiate(insects.gameObject, lanternParent);
                        m_insects.name = "insects";
                        m_insects.SetActive(false);
                        m_insects.transform.localPosition = new Vector3(0.42f, 1.85f, 0f);
                        m_insects.layer = 8; // effect
                    }
                }

                m_fireWarmth = AddCollider(lanternParent, "FireWarmth", typeof(SphereCollider)).gameObject;
                m_fireWarmth.layer = 14; // character_trigger
                m_fireWarmth.transform.localPosition = new Vector3(-1f, 0f, 0f);

                SphereCollider fireWarmthCollider = m_fireWarmth.GetComponent<SphereCollider>();
                fireWarmthCollider.radius = 3;
                fireWarmthCollider.isTrigger = true;

                EffectArea fireWarmth = m_fireWarmth.gameObject.AddComponent<EffectArea>();
                fireWarmth.m_type = EffectArea.Type.Fire | EffectArea.Type.Heat;

                if (lanternSwitchable.Value)
                {
                    LongshipPartController lampController = lanternParent.gameObject.AddComponent<LongshipPartController>();
                    lampController.m_name = "$lu_part_lamp_name";
                    lampController.m_zdoPartDisabled = s_lightsDisabled;
                    lampController.m_messageEnable = "$lu_part_lamp_enable";
                    lampController.m_messageDisable = "$lu_part_lamp_disable";
                    lampController.m_enableEffects = lampEnableEffects;
                    lampController.m_disableEffects = lampDisableEffects;
                    lampController.m_nview = m_nview;
                    lampController.m_useDistance = 2f;
                }

                lantern.gameObject.SetActive(true);
                m_lantern.SetActive(false);
            }

            GameObject interactables = new GameObject("interactive");

            Transform interactableParent = interactables.transform;
            interactableParent.SetParent(customize, worldPositionStays: false);

            // Mast controller
            if (mastEnabled.Value || mastRemovable.Value)
            {
                Transform mastControllerCollider = AddCollider(interactableParent, "mast_controller", typeof(BoxCollider));
                mastControllerCollider.localPosition = new Vector3(-0.05f, 0.08f, 0f);
                mastControllerCollider.localScale = new Vector3(0.6f, 0.17f, 0.26f);

                m_mastUpgrade = mastControllerCollider.gameObject;
                m_mastUpgrade.layer = piece_nonsolid;
                m_mastUpgrade.SetActive(true);

                LongshipPartController mastController = mastControllerCollider.gameObject.AddComponent<LongshipPartController>();
                mastController.m_name = "$lu_part_mast_name";
                mastController.m_zdoPartDisabled = mastRemovable.Value ? s_mastRemoved : 0;
                mastController.m_messageEnable = "$lu_part_mast_enable";
                mastController.m_messageDisable = "$lu_part_mast_disable";
                mastController.m_enableEffects = woodSwitchEffects;
                mastController.m_disableEffects = woodSwitchEffects;
                mastController.m_nview = m_nview;
                mastController.m_useDistance = 2.5f;
                mastController.AddUpgradeRequirement(mastEnabled.Value ? s_mastUpgraded : 0,
                                                     "$lu_part_mast_upgrade",
                                                     ParseRequirements(mastUpgradeRecipe.Value),
                                                     mastStation.Value,
                                                     mastStationLvl.Value,
                                                     mastStationRange.Value);
            }

            // Storage controller
            if (m_container)
            {
                Transform storageUpgradeCollider = AddCollider(interactableParent, "storage_controller", typeof(BoxCollider));
                storageUpgradeCollider.localPosition = new Vector3(-1.9f, -0.02f, 0f);
                storageUpgradeCollider.localScale = new Vector3(0.45f, 0.45f, 0.18f);
                storageUpgradeCollider.localEulerAngles = new Vector3(0f, 270f, 0f);

                BoxCollider colliderContainer = m_container.GetComponent<BoxCollider>();
                BoxCollider colliderUpgrade = storageUpgradeCollider.GetComponent<BoxCollider>();

                colliderUpgrade.center = colliderContainer.center;
                colliderUpgrade.size = colliderContainer.size;

                LongshipPartController storageController = storageUpgradeCollider.gameObject.AddComponent<LongshipPartController>();
                storageController.m_name = m_container.m_name.StartsWith("$") ? m_container.m_name : "$msg_cart_storage";
                storageController.m_nview = m_nview;

                storageController.AddUpgradeRequirement(s_containerUpgradedLvl1,
                                                        "$lu_part_container_upgrade1",
                                                        ParseRequirements(containerLvl1UpgradeRecipe.Value),
                                                        containerLvl1Station.Value,
                                                        containerLvl1StationLvl.Value,
                                                        containerLvl1StationRange.Value);

                storageController.AddUpgradeRequirement(s_containerUpgradedLvl2,
                                                        "$lu_part_container_upgrade2",
                                                        ParseRequirements(containerLvl2UpgradeRecipe.Value),
                                                        containerLvl2Station.Value,
                                                        containerLvl2StationLvl.Value,
                                                        containerLvl2StationRange.Value);

                m_storageUpgrade = storageUpgradeCollider.gameObject;
                m_storageUpgrade.layer = piece_nonsolid;
                m_storageUpgrade.SetActive(false);
            }

            // Health controller
            if (m_protectiveParts != null)
            {
                m_healthUpgrade = new GameObject("health");

                Transform healthParent = m_healthUpgrade.transform;
                healthParent.SetParent(interactableParent, worldPositionStays: false);

                Transform leftSide = transform.Find("ship/colliders/left_side");
                if (leftSide)
                    for (int i = 1; i < Mathf.Min(leftSide.childCount, 5); i++)
                        AddHealthController(leftSide, i, "health_controller_left", 0.01f);

                Transform rightSide = transform.Find("ship/colliders/left_side (1)");
                if (rightSide)
                    for (int i = 1; i < Mathf.Min(rightSide.childCount, 5); i++)
                        AddHealthController(rightSide, i, "health_controller_right", -0.01f);

                void AddHealthController(Transform parent, int i, string name, float offset)
                {
                    GameObject healthUpgradeCollider = Instantiate(parent.GetChild(i).gameObject, healthParent, worldPositionStays:true);
                    healthUpgradeCollider.name = i == 0 ? name : $"{name} ({i})";

                    healthUpgradeCollider.transform.localPosition += new Vector3(0f, 0f, offset);

                    LongshipPartController healthController = healthUpgradeCollider.gameObject.AddComponent<LongshipPartController>();
                    healthController.m_nview = m_nview;
                    healthController.m_name = "$lu_part_hull_name";
                    healthController.AddUpgradeRequirement(s_healthUpgraded,
                                                           "$lu_part_hull_upgrade1",
                                                           ParseRequirements(healthUpgradeRecipe.Value),
                                                           healthLvl1Station.Value,
                                                           healthLvl1StationLvl.Value,
                                                           healthLvl1StationRange.Value);


                    if (healthUpgradeLvl2.Value != 0)
                    {
                        healthController.AddUpgradeRequirement(s_ashlandsUpgraded,
                                                               Localization.instance.Localize("$lu_part_hull_upgrade2", ashlandsProtection.Value ? "\n$lu_part_hull_upgrade2_ashlands" : ""),
                                                               ParseRequirements(ashlandsUpgradeRecipe.Value),
                                                               healthLvl2Station.Value,
                                                               healthLvl2StationLvl.Value,
                                                               healthLvl2StationRange.Value);
                    }

                    healthUpgradeCollider.gameObject.SetActive(true);
                    healthUpgradeCollider.gameObject.layer = piece_nonsolid;
                }
            }

            // Shields controller
            if (m_protectiveParts != null)
            {
                m_shieldsStyles = new GameObject("shields");

                Transform shieldsParent = m_shieldsStyles.transform;
                shieldsParent.SetParent(interactableParent, worldPositionStays: false);

                Transform storageUpgradeCollider = AddCollider(shieldsParent, "shield_controller_right", typeof(BoxCollider));
                storageUpgradeCollider.localPosition = new Vector3(-1.16f, 0.2f, 1.11f);
                storageUpgradeCollider.localScale = new Vector3(0.95f, 0.45f, 0.05f);
                storageUpgradeCollider.localEulerAngles = new Vector3(3f, 355f, 0f);
                
                Transform storageUpgradeCollider1 = AddCollider(shieldsParent, "shield_controller_right (1)", typeof(BoxCollider));
                storageUpgradeCollider1.localPosition = new Vector3(-0.23f, 0.2f, 1.16f);
                storageUpgradeCollider1.localScale = new Vector3(0.91f, 0.43f, 0.05f);
                storageUpgradeCollider1.localEulerAngles = new Vector3(3f, 0f, 0f);

                Transform storageUpgradeCollider2 = AddCollider(shieldsParent, "shield_controller_right (2)", typeof(BoxCollider));
                storageUpgradeCollider2.localPosition = new Vector3(1.35f, 0.16f, 1.05f);
                storageUpgradeCollider2.localScale = new Vector3(1.5f, 0.45f, 0.05f);
                storageUpgradeCollider2.localEulerAngles = new Vector3(3f, 8f, 0f);

                Transform storageUpgradeCollider3 = AddCollider(shieldsParent, "shield_controller_left", typeof(BoxCollider));
                storageUpgradeCollider3.localPosition = new Vector3(-1.18f, 0.27f, -1.11f);
                storageUpgradeCollider3.localScale = new Vector3(0.95f, 0.45f, 0.05f);
                storageUpgradeCollider3.localEulerAngles = new Vector3(0, 4f, 0f);

                Transform storageUpgradeCollider4 = AddCollider(shieldsParent, "shield_controller_left (1)", typeof(BoxCollider));
                storageUpgradeCollider4.localPosition = new Vector3(-0.25f, 0.27f, -1.16f);
                storageUpgradeCollider4.localScale = new Vector3(0.89f, 0.45f, 0.05f);
                storageUpgradeCollider4.localEulerAngles = new Vector3(0, 2f, 0f);

                Transform storageUpgradeCollider5 = AddCollider(shieldsParent, "shield_controller_left (2)", typeof(BoxCollider));
                storageUpgradeCollider5.localPosition = new Vector3(1.33f, 0.23f, -1.1f);
                storageUpgradeCollider5.localScale = new Vector3(1.37f, 0.45f, 0.05f);
                storageUpgradeCollider5.localEulerAngles = new Vector3(0, 352f, 0f);

                if (changeShields.Value)
                    for (int i = 0; i < shieldsParent.childCount; i++)
                    {
                        LongshipPartController shieldController = shieldsParent.GetChild(i).gameObject.AddComponent<LongshipPartController>();
                        shieldController.m_name = "$lu_part_shields_name";
                        shieldController.m_nview = m_nview;
                        shieldController.m_messageSwitch = "$lu_part_shields_switch";
                        shieldController.m_zdoPartVariant = s_shieldsStyle;
                        shieldController.m_variants = 4 + 3 * customShields.Count;
                        shieldController.m_switchEffects = woodSwitchEffects;
                    }
            }

            // Heads
            Transform unused = customize.parent.Find("unused");
            if (unused)
            {
                GameObject heads = new GameObject("heads");

                Transform headsParent = heads.transform;
                headsParent.SetParent(customize, worldPositionStays: false);

                List<GameObject> headsObjects = new List<GameObject>();

                Transform carnyx_head = unused.Find("carnyx_head");
                if (carnyx_head)
                {
                    GameObject head = Instantiate(carnyx_head.gameObject, headsParent, worldPositionStays: true);
                    head.name = carnyx_head.name;
                    head.SetActive(false);
                    
                    headsObjects.Add(head);
                }

                Transform dragon_head = unused.Find("dragon_head");
                if (dragon_head)
                {
                    GameObject head = Instantiate(dragon_head.gameObject, headsParent, worldPositionStays: true);
                    head.name = dragon_head.name;
                    head.SetActive(false);

                    headsObjects.Add(head);
                }

                Transform oseberg_head = unused.Find("oseberg_head");
                if (oseberg_head)
                {
                    GameObject head = Instantiate(oseberg_head.gameObject, headsParent, worldPositionStays: true);
                    head.name = oseberg_head.name;
                    head.SetActive(false);

                    head.transform.localPosition = Vector3.zero;

                    headsObjects.Add(head);
                }

                m_heads = headsObjects.ToArray();
            }

            // Heads controller
            if (m_heads != null)
            {
                Transform headsControllerCollider = AddCollider(interactableParent, "heads_controller", typeof(BoxCollider));
                headsControllerCollider.localPosition = new Vector3(-3.90f, 0.53f, 0f);
                headsControllerCollider.localScale = new Vector3(0.10f, 0.45f, 0.40f);
                headsControllerCollider.localEulerAngles = new Vector3(0f, 0f, 63f);

                m_headStyles = headsControllerCollider.gameObject;
                m_headStyles.layer = piece_nonsolid;
                m_headStyles.SetActive(true);

                LongshipPartController headsController = headsControllerCollider.gameObject.AddComponent<LongshipPartController>();
                headsController.m_name = "$lu_part_head_name";
                headsController.m_nview = m_nview;
                headsController.m_messageSwitch = "$lu_part_head_switch";
                headsController.m_zdoPartVariant = s_headStyle;
                headsController.m_variants = 4;
                headsController.m_switchEffects = woodSwitchEffects;
            }

            GameObject pieceTurret = Resources.FindObjectsOfTypeAll<Turret>().FirstOrDefault(ws => ws.name == turretName)?.gameObject;
            if (pieceTurret != null)
            {
                m_turrets = new GameObject("turrets")
                {
                    layer = vehicle
                };

                Transform turretsParent = m_turrets.transform;
                turretsParent.SetParent(customize, worldPositionStays: false);

                Transform turretsControllerCollider = AddCollider(interactableParent, "turrets_controller", typeof(BoxCollider));
                turretsControllerCollider.localPosition = new Vector3(-3.35f, 0.1f, 0f);
                turretsControllerCollider.localScale = new Vector3(0.04f, 0.35f, 0.45f);

                m_turretsUpgrade = turretsControllerCollider.gameObject;
                m_turretsUpgrade.layer = piece_nonsolid;
                m_turretsUpgrade.SetActive(true);

                LongshipPartController turretsController = turretsControllerCollider.gameObject.AddComponent<LongshipPartController>();
                turretsController.m_name = "$lu_part_turrets_name";
                turretsController.m_nview = m_nview;
                turretsController.m_zdoPartDisabled = s_turretsDisabled;
                turretsController.m_messageEnable = "$lu_part_turrets_enable";
                turretsController.m_enableEffects = turretsEnableEffects;
                turretsController.m_messageDisable = "$lu_part_turrets_disable";
                turretsController.m_disableEffects = turretsDisableEffects;
                turretsController.AddUpgradeRequirement(s_turretsUpgraded,
                                                        "$lu_part_turrets_upgrade",
                                                        ParseRequirements(turretsUpgradeRecipe.Value),
                                                        turretsStation.Value,
                                                        turretsStationLvl.Value,
                                                        turretsStationRange.Value);

                GameObject turret_right = Instantiate(pieceTurret.transform.Find("New").gameObject, turretsParent, worldPositionStays: false);
                turret_right.name = "turret_right";
                turret_right.layer = piece_nonsolid;
                turret_right.SetActive(true);
                turret_right.transform.Find("Base").gameObject.SetActive(false);

                turret_right.transform.localScale = Vector3.one * 0.25f;
                turret_right.transform.localPosition = new Vector3(-3.3f, -0.01f, 0.53f);
                turret_right.transform.localEulerAngles = new Vector3(0f, 350f, 0f);

                BoxCollider rightCollider = turret_right.AddComponent<BoxCollider>();
                rightCollider.center = new Vector3(0f, 0.7f, 0.1f);
                rightCollider.size = new Vector3(1f, 2f, 1f);

                GameObject turret_left = Instantiate(turret_right, turretsParent, worldPositionStays: true);
                turret_left.name = "turret_left";

                turret_left.transform.localPosition = new Vector3(-3.3f, -0.01f, -0.53f);
                turret_left.transform.localEulerAngles = new Vector3(0f, 190f, 0f);

                Turret original = pieceTurret.GetComponent<Turret>();
                ShipTurret.m_shootEffect = original.m_shootEffect;
                ShipTurret.m_addAmmoEffect = original.m_addAmmoEffect;
                ShipTurret.m_reloadEffect = original.m_reloadEffect;
                ShipTurret.m_warmUpStartEffect = original.m_warmUpStartEffect;
                ShipTurret.m_newTargetEffect = original.m_newTargetEffect;
                ShipTurret.m_lostTargetEffect = original.m_lostTargetEffect;
                ShipTurret.m_setTargetEffect = original.m_setTargetEffect;

                turret_right.AddComponent<ShipTurret>().SetPositionAtShip(isLeft: false).FillAllowedAmmo(original.m_allowedAmmo).m_destroyedLootPrefab = m_destroyedLootPrefab;
                turret_left.AddComponent<ShipTurret>().SetPositionAtShip(isLeft: true).FillAllowedAmmo(original.m_allowedAmmo).m_destroyedLootPrefab = m_destroyedLootPrefab;
            }

            GameObject standBossDragon = Resources.FindObjectsOfTypeAll<ItemStand>().FirstOrDefault(ws => ws.transform.root.gameObject.name == moderBossStone)?.gameObject;
            if (standBossDragon != null)
            {
                m_itemstandObject = new GameObject("ItemStand_Bow")
                {
                    layer = vehicle
                };

                Transform standParent = m_itemstandObject.transform;
                standParent.SetParent(customize, worldPositionStays: false);
                m_itemstandObject.SetActive(false);

                GameObject itemstand = Instantiate(standBossDragon, standParent, worldPositionStays: false);
                itemstand.name = "itemstand";
                itemstand.layer = piece_nonsolid;
                Destroy(itemstand.transform.Find("model/wood_pole (2)").gameObject);

                Transform model = itemstand.transform.Find("model");
                model.gameObject.layer = piece_nonsolid;
                model.gameObject.SetActive(true);

                Transform plate = model.Find("plate");
                plate.localScale = Vector3.one * 0.3f;
                plate.gameObject.layer = piece_nonsolid;
                Destroy(plate.GetComponent<MeshCollider>());

                MeshRenderer plateRenderer = plate.GetComponent<MeshRenderer>();
                if (standSharedMaterial == null)
                {
                    standSharedMaterial = new Material(plateRenderer.sharedMaterial)
                    {
                        shader = shaderStandard
                    };
                }
                plateRenderer.sharedMaterial = standSharedMaterial;

                Transform attach = itemstand.transform.Find("attach_trophie");
                attach.localPosition = Vector3.zero;
                attach.localScale = Vector3.one * 0.75f;
                attach.gameObject.layer = piece_nonsolid;

                Transform dropspawn = itemstand.transform.Find("dropspawn");
                dropspawn.localPosition = new Vector3(0.01f, 0.5f, -0.69f);
                dropspawn.gameObject.layer = piece_nonsolid;

                itemstand.transform.localScale = Vector3.one * 0.45f;
                itemstand.transform.localPosition = new Vector3(-4.60f, 0.70f, 0f);
                itemstand.transform.localEulerAngles = new Vector3(0f, 270f, 0f);

                itemstand.GetComponent<BoxCollider>().size = new Vector3(0.75f, 1f, 0.5f);

                ItemStand component = itemstand.GetComponent<ItemStand>();

                m_trophyStand = itemstand.AddComponent<ShipTrophyStand>();
                m_trophyStand.m_activatePowerEffects = component.m_activatePowerEffects;
                m_trophyStand.m_activatePowerEffectsPlayer = component.m_activatePowerEffectsPlayer;
                m_trophyStand.m_attachOther = component.m_attachOther;
                m_trophyStand.m_dropSpawnPoint = component.m_dropSpawnPoint;
                m_trophyStand.m_effects = component.m_effects;
                m_trophyStand.m_destroyEffects = component.m_destroyEffects;

                Destroy(component);

                if (itemStandDisableSpeaking.Value)
                    m_itemstandObject.GetComponentInChildren<RandomSpeak>()?.gameObject.SetActive(false);

                // Stand Awake call
                m_itemstandObject.SetActive(true);
            }

            // TODO
            // recipe balance
            // check multiplayer
        }

        public void OnDestroyed()
        {
            if (m_nview.IsOwner())
            {
                DropItemStand();

                DropSpentUpgrades();
            }
        }

        private void DropItemStand()
        {
            if (!m_trophyStand)
                return;

            if (!m_trophyStand.HaveAttachment())
                return;

            string @string = m_trophyStand.m_nview.GetZDO().GetString(ZDOVars.s_item);
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

                GameObject item = Instantiate(itemPrefab, m_trophyStand.m_dropSpawnPoint.position + vector, m_trophyStand.m_dropSpawnPoint.rotation * quaternion);
                ItemDrop itemDrop = item.GetComponent<ItemDrop>();
                itemDrop.LoadFromExternalZDO(m_trophyStand.m_nview.GetZDO());
                item.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;

                if (m_destroyedLootPrefab)
                {
                    Inventory inventory = SpawnContainer(m_destroyedLootPrefab);
                    if (inventory.AddItem(itemDrop.m_itemData))
                        ZNetScene.instance.Destroy(item);
                }
            }
        }

        public void DropSpentUpgrades()
        {
            if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost))
                return;

            Dictionary<int, Piece.Requirement[]> upgradeReqs = new Dictionary<int, Piece.Requirement[]>();
            foreach (LongshipPartController partController in GetComponentsInChildren<LongshipPartController>(includeInactive: true))
                partController.AddSpentUpgrades(upgradeReqs);

            if (upgradeReqs.Count == 0)
                return;

            upgradeReqs.Values.Do(itemsToDrop => DropRequirements(itemsToDrop.ToList()));
        }

        public void DropRequirements(List<Piece.Requirement> itemsToDrop)
        {
            if (m_destroyedLootPrefab)
            {
                Inventory inventory = SpawnContainer(m_destroyedLootPrefab);
                while (itemsToDrop.Count > 0)
                {
                    Piece.Requirement item = itemsToDrop[0];
                    if (item.m_amount <= 0)
                        itemsToDrop.RemoveAt(0);
                    else if (inventory.AddItem(ObjectDB.instance.GetItemPrefab(item.m_resItem.name), 1))
                        item.m_amount--;
                    else if (!inventory.HaveEmptySlot())
                        inventory = SpawnContainer(m_destroyedLootPrefab);
                    else
                        itemsToDrop.RemoveAt(0);
                }
            }
            else
            {
                while (itemsToDrop.Count > 0)
                {
                    Piece.Requirement item = itemsToDrop[0];
                    while (item.m_amount > 0)
                    {
                        Vector3 position = base.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
                        Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
                        ItemDrop itemDrop = Instantiate(ObjectDB.instance.GetItemPrefab(item.m_resItem.name), position, rotation).GetComponent<ItemDrop>();
                        if (itemDrop == null)
                            break;

                        itemDrop.SetStack(item.m_amount);
                        item.m_amount -= itemDrop.m_itemData.m_stack;
                    }
                    itemsToDrop.RemoveAt(0);
                }
            }
        }

        public Inventory SpawnContainer(GameObject lootContainerPrefab)
        {
            Vector3 position = base.transform.position + UnityEngine.Random.insideUnitSphere * 1f;
            return Instantiate(lootContainerPrefab, position, UnityEngine.Random.rotation).GetComponent<Container>().GetInventory();
        }

        private static bool IsNightTime()
        {
            return EnvMan.IsNight();
        }
        
        private static bool IsTimeToLight()
        {
            if (!EnvMan.IsDaylight() || !EnvMan.instance)
                return true;
            
            float dayFraction = EnvMan.instance.GetDayFraction();

            if (!(dayFraction <= 0.28f))
                return dayFraction >= 0.71f;

            return true;
        }

        internal static void OnGlobalStart()
        {
            isNightTime = false;
            isTimeToLight = true;

            FixPrefab();
        }

        internal static void OnGlobalDestroy()
        {
            Destroy(standSharedMaterial);
            Destroy(storageSharedMaterial);
            Destroy(plankSharedMaterial);

            standSharedMaterial = null;
            storageSharedMaterial = null;
            plankSharedMaterial = null;
        }

        private static void FixPrefab()
        {
            if (prefabFixed)
                return;

            prefabFixed = true;

            GameObject prefab = Resources.FindObjectsOfTypeAll<Ship>().FirstOrDefault(ws => ws.name == prefabName)?.gameObject;
            if (prefab == null)
                return;

            // Flickering fix
            shaderStandard = prefab.transform.Find("ship/visual/Customize/TraderLamp/fi_vil_light_lamp01_03").GetComponent<MeshRenderer>().sharedMaterial.shader;

            Transform storage = prefab.transform.Find("ship/visual/Customize/storage");
            for (int i = 0; i < storage.childCount; i++)
            {
                Transform child = storage.GetChild(i);
                if (!child.name.StartsWith("default"))
                    continue;

                MeshRenderer renderer = child.GetComponent<MeshRenderer>();

                if (storageSharedMaterial == null)
                    storageSharedMaterial = new Material(renderer.sharedMaterial)
                    {
                        shader = shaderStandard
                    };

                renderer.sharedMaterial = storageSharedMaterial;
            }
            
            MeshRenderer plankRenderer = prefab.transform.Find("ship/visual/hull_worn/plank").GetComponent<MeshRenderer>();
            if (plankSharedMaterial == null)
                plankSharedMaterial = new Material(plankRenderer.sharedMaterial)
                {
                    shader = shaderStandard
                };
            plankRenderer.sharedMaterial = plankSharedMaterial;
            prefab.transform.Find("ship/visual/hull_worn/plank (1)").GetComponent<MeshRenderer>().sharedMaterial = plankSharedMaterial;

            Transform beam = prefab.transform.Find("ship/visual/Customize/ShipTen2_beam");
            beam.localPosition += new Vector3(0f, 0.1f, 0f);
            beam.gameObject.layer = vehicle;

            Transform tent = prefab.transform.Find("ship/visual/Customize/ShipTen2 (1)");
            tent.localPosition += new Vector3(0f, 0.08f, 0f);

            Transform colliderOnboard = prefab.transform.Find("OnboardTrigger");
            colliderOnboard.localScale += new Vector3(0.1f, 2f, 0f);
            colliderOnboard.localPosition += new Vector3(-0.05f, 1f, 0f);

            Material material = tent.GetComponentInChildren<MeshRenderer>().sharedMaterial;

            material.SetFloat("_RippleSpeed", 75);
            material.SetFloat("_RippleDistance", 1.25f);

            prefab.transform.Find("ship/visual/Mast").localPosition += new Vector3(0f, 0.21f, 0f);
            prefab.transform.Find("ship/visual/Customize/ShipTentHolders").localPosition += new Vector3(0f, 0.01f, 0f);

            Transform holders = prefab.transform.Find("ship/visual/Customize/ShipTentHolders (1)");
            holders.localPosition += new Vector3(0.10f, -0.18f, 0.11f);
            holders.localEulerAngles += new Vector3(0.00f, 5.00f, 6.60f);

            Transform lamp = prefab.transform.Find("ship/visual/Customize/TraderLamp");
            lamp.gameObject.SetActive(false);

            prefabInit = true;
            prefab.AddComponent<LongshipCustomizableParts>();
            prefabInit = false;
        }

        private static Transform AddCollider(Transform transform, string name, System.Type type)
        {
            Transform collider = new GameObject(name, type).transform;
            collider.SetParent(transform, worldPositionStays: false);
            
            collider.gameObject.layer = vehicle;

            return collider;
        }

        internal static void AddCustomTent(string filename)
        {
            AddCustomTexture(customTents, filename);
        }

        internal static void AddCustomSail(string filename)
        {
            AddCustomTexture(customSails, filename);
        }

        internal static void AddCustomShields(string filename)
        {
            AddCustomTexture(customShields, filename);
        }

        private static void AddCustomTexture(List<Texture2D> list, string filename)
        {
            Texture2D tex = new Texture2D(2, 2);
            if (LoadTextureFromConfigDirectory(filename, ref tex))
                list.Add(tex);
        }

        private static void CheckEffects()
        {
            CheckEffect(fabricSwitchEffects, "vfx_Place_HildirFabricRoll");
            CheckEffect(woodSwitchEffects, "sfx_gui_repairitem_workbench");
            CheckEffect(lampEnableEffects, "sfx_FireAddFuel");
            CheckEffect(lampDisableEffects, "sfx_fishingrod_linebreak");
            CheckEffect(turretsEnableEffects, "fx_guardstone_permitted_add");
            CheckEffect(turretsDisableEffects, "fx_guardstone_permitted_removed");

            static void CheckEffect(EffectList effect, string prefabName)
            {
                if (effect.m_effectPrefabs != null && effect.m_effectPrefabs.Length > 0)
                    return;

                GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
                effect.m_effectPrefabs = new EffectList.EffectData[1] {
                    new EffectList.EffectData
                    {
                        m_prefab = prefab,
                        m_enabled = prefab != null,
                    }
                };
            }
        }

        internal static void UpdatePropertyBlocks(GameObject go)
        {
            if (s_allInstances.TryGetValue(go, out LongshipCustomizableParts instance))
                instance.UpdatePropertyBlocks();
        }

        internal static void CombinePropertyBlocks(GameObject go, MaterialPropertyBlock propertyBlockToCombine)
        {
            if (s_allInstances.TryGetValue(go, out LongshipCustomizableParts instance))
                instance.CombinePropertyBlocks(propertyBlockToCombine);
        }

        internal static bool HasComponent(GameObject go)
        {
            return s_allInstances.ContainsKey(go);
        }
    }
}
