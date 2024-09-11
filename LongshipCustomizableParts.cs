using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        private GameObject m_mast;
        private GameObject m_ropes;

        private MeshRenderer m_lampRenderer;
        private GameObject[] m_lightParts;
        private GameObject[] m_containerPartsLvl1;
        private GameObject[] m_containerPartsLvl2;
        private GameObject[] m_protectiveParts;
        private GameObject[] m_heads;

        private bool m_containerUpgradedLvl1;
        private bool m_containerUpgradedLvl2;
        private bool m_healthUpgraded;
        private bool m_ashlandsUpgraded;
        private bool m_isLampLightOn;
        private int m_headStyle;
        private int m_shieldsStyle;
        private int m_tentStyle;

        public const string prefabName = "VikingShip";
        private static bool prefabFixed = false;

        private static Material lampSharedMaterial;
        private static Color lampColor;
        private static bool isNightTime;
        private static bool isTimeToLight = true;

        public static readonly int s_mastUpgraded = "MastUpgraded".GetStableHashCode();
        public static readonly int s_mastRemoved = "MastRemoved".GetStableHashCode();
        public static readonly int s_lanternUpgraded = "LanternUpgraded".GetStableHashCode();
        public static readonly int s_lanternAdded = "LanternAdded".GetStableHashCode();
        public static readonly int s_tentUpgraded = "TentUpgraded".GetStableHashCode();
        public static readonly int s_tentAdded = "TentAdded".GetStableHashCode();
        public static readonly int s_lightsOn = "LampLight".GetStableHashCode();

        public static readonly int s_containerUpgradedLvl1 = "ContainerUpgradedLvl1".GetStableHashCode();
        public static readonly int s_containerUpgradedLvl2 = "ContainerUpgradedLvl2".GetStableHashCode();
        
        public static readonly int s_healthUpgraded = "HealthUpgraded".GetStableHashCode();
        public static readonly int s_ashlandsUpgraded = "AshlandsUpgraded".GetStableHashCode();

        public static readonly int s_headStyle = "HeadStyle".GetStableHashCode();
        public static readonly int s_shieldsStyle = "ShieldsStyle".GetStableHashCode();
        public static readonly int s_tentStyle = "TentStyle".GetStableHashCode();

        public static readonly MaterialPropertyBlock s_materialBlock = new MaterialPropertyBlock();
        public static Texture2D s_ashlandsHull = new Texture2D(2, 2);
        public static Texture2D s_tentBlue = new Texture2D(2, 2);
        public static Texture2D s_tentBlack = new Texture2D(2, 2);

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
        }

        private void Start()
        {
            if (prefabInit)
                return;

            InitializeParts();
        }

        private void FixedUpdate()
        {
            if (prefabInit)
                return;

            if (m_zdo == null || !m_ship)
                return;

            m_customMast = m_zdo.GetBool(s_mastUpgraded);

            m_mast?.SetActive(!m_customMast || !m_zdo.GetBool(s_mastRemoved));
            m_ropes?.SetActive(m_mast.activeSelf);
            m_beamMast?.SetActive(!m_mast.activeSelf);

            m_beamTent?.SetActive(m_customMast);

            m_tent?.SetActive(m_customMast && m_zdo.GetBool(s_tentAdded));
            m_lantern?.SetActive(m_customMast && m_zdo.GetBool(s_lanternAdded));

            m_holdersRight?.SetActive(m_tent && m_tent.activeInHierarchy);
            m_holdersLeft?.SetActive(m_tent && m_tent.activeInHierarchy);

            m_fireWarmth?.SetActive(m_lantern && m_lantern.activeInHierarchy && m_tent && m_tent.activeInHierarchy);

            bool timeChanged = false;
            if (m_lantern && m_lightParts != null && m_lantern.activeInHierarchy && (m_isLampLightOn != m_zdo.GetBool(s_lightsOn) || (timeChanged = isTimeToLight != IsTimeToLight()) || isNightTime != IsNightTime()))
            {
                isNightTime = IsNightTime();
                isTimeToLight = IsTimeToLight();

                if (timeChanged)
                    m_zdo.Set(s_lightsOn, isTimeToLight);

                m_isLampLightOn = m_zdo.GetBool(s_lightsOn);
                UpdateLights();
            }

            if (m_storageUpgrade && m_containerUpgradedLvl2 != m_zdo.GetBool(s_containerUpgradedLvl2))
            {
                m_containerUpgradedLvl2 = m_zdo.GetBool(s_containerUpgradedLvl2);
                m_containerPartsLvl2?.Do(part => part?.SetActive(m_containerUpgradedLvl2));
                
                if (m_containerUpgradedLvl2 && m_container.m_height < 4)
                    m_container.m_height = 4;

                if (m_containerUpgradedLvl2 && m_container.GetInventory().GetHeight() < 4)
                    m_container.GetInventory().m_height = 4;
            }

            if (m_storageUpgrade && m_containerUpgradedLvl1 != m_zdo.GetBool(s_containerUpgradedLvl1))
            {
                m_containerUpgradedLvl1 = m_zdo.GetBool(s_containerUpgradedLvl1);
                m_containerPartsLvl1?.Do(part => part?.SetActive(m_containerUpgradedLvl1));

                if (m_containerUpgradedLvl1 && m_container.m_width < 7)
                    m_container.m_width = 7;

                if (m_containerUpgradedLvl1 && m_container.GetInventory().GetWidth() < 7)
                    m_container.GetInventory().m_width = 7;
            }

            m_storageUpgrade?.SetActive(!m_containerUpgradedLvl2);

            if (m_protectiveParts != null && m_wnt && m_healthUpgraded != m_zdo.GetBool(s_healthUpgraded))
            {
                m_healthUpgraded = m_zdo.GetBool(s_healthUpgraded);
                m_protectiveParts.Do(part => part?.SetActive(m_healthUpgraded));

                if (m_healthUpgraded && m_wnt.m_health < 1500)
                    m_wnt.m_health = 1500;
            }

            m_shieldsStyles?.SetActive(m_healthUpgraded);

            if (m_protectiveParts != null && m_wnt && m_ashlandsUpgraded != m_zdo.GetBool(s_ashlandsUpgraded))
            {
                m_ashlandsUpgraded = m_zdo.GetBool(s_ashlandsUpgraded);

                if (m_ashlandsUpgraded)
                {
                    if (m_wnt.m_health < 2000)
                        m_wnt.m_health = 2000;

                    m_ship.m_ashlandsReady = true;

                    m_wnt.m_ashDamageResist = true;
                    m_wnt.m_damages.Apply(new List<HitData.DamageModPair> { new HitData.DamageModPair() { m_type = HitData.DamageType.Fire, m_modifier = HitData.DamageModifier.VeryResistant } });

                    foreach (MeshRenderer renderer in new List<MeshRenderer> {
                                                            m_wnt.m_new.transform.Find("hull").gameObject.GetComponent<MeshRenderer>(),
                                                            m_wnt.m_worn.transform.Find("hull").gameObject.GetComponent<MeshRenderer>(),
                                                            m_wnt.m_new.transform.Find("skull_head").gameObject.GetComponent<MeshRenderer>(),
                                                            m_wnt.m_worn.transform.Find("skull_head").gameObject.GetComponent<MeshRenderer>()}
                                                            .Union(m_heads.Select(head => head.GetComponent<MeshRenderer>())))
                    {
                        renderer.GetPropertyBlock(s_materialBlock);
                        s_materialBlock.SetTexture("_MainTex", s_ashlandsHull);
                        renderer.SetPropertyBlock(s_materialBlock);
                    }
                }
            }

            if (m_protectiveParts != null && m_healthUpgraded && m_shieldsStyle != m_zdo.GetInt(s_shieldsStyle))
            {
                m_shieldsStyle = m_zdo.GetInt(s_shieldsStyle);
                m_protectiveParts.Do(part => part.GetComponent<ItemStyle>().Setup(m_shieldsStyle));
            }

            m_healthUpgrade?.SetActive(!m_ashlandsUpgraded);

            if (m_tent != null && m_tent.activeInHierarchy && m_tentStyle != m_zdo.GetInt(s_tentStyle))
            {
                m_tentStyle = m_zdo.GetInt(s_tentStyle);

                MeshRenderer renderer = m_tent.GetComponentInChildren<MeshRenderer>();
                if (m_tentStyle == 0)
                {
                    renderer.SetPropertyBlock(null);
                }
                else
                {
                    renderer.GetPropertyBlock(s_materialBlock);
                    if (m_tentStyle == 1)
                        s_materialBlock.SetTexture("_MainTex", s_tentBlue);
                    else if (m_tentStyle == 2)
                        s_materialBlock.SetTexture("_MainTex", s_tentBlack);
                    renderer.SetPropertyBlock(s_materialBlock);
                }
            }

            if (m_heads != null && m_wnt && m_headStyle != m_zdo.GetInt(s_headStyle))
            {
                m_headStyle = m_zdo.GetInt(s_headStyle);

                for (int i = 0; i < m_heads.Length; i++)
                    m_heads[i].SetActive(m_headStyle == i + 1);

                foreach (GameObject head in new GameObject[] {
                                                            m_wnt.m_new.transform.Find("skull_head").gameObject,
                                                            m_wnt.m_worn.transform.Find("skull_head").gameObject })
                    head.SetActive(m_headStyle == 0);
            }
        }

        private void UpdateLights()
        {
            m_insects?.SetActive(isNightTime && m_isLampLightOn);

            m_lightParts?.Do(part => part?.SetActive(m_isLampLightOn));

            if (m_lampRenderer)
            {
                m_lampRenderer.GetPropertyBlock(s_materialBlock);
                s_materialBlock.SetColor("_EmissionColor", m_isLampLightOn ? lampColor : Color.grey);
                m_lampRenderer.SetPropertyBlock(s_materialBlock);
            }
        }

        private void InitializeParts()
        {
            m_mast = transform.Find("ship/visual/Mast").gameObject;
            m_ropes = transform.Find("ship/visual/ropes").gameObject;

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
                m_protectiveParts.Do(part => part.AddComponent<ItemStyle>());
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

                Transform mastBeamCollider = AddCollider(m_beamMast.transform, "mast_beam", typeof(BoxCollider));
                mastBeamCollider.localPosition = new Vector3(0f, 1.58f, -0.48f);
                mastBeamCollider.localScale = new Vector3(0.16f, 0.16f, 2.5f);

                Transform lanternBeamCollider = AddCollider(beam, "lantern_beam", typeof(BoxCollider));
                lanternBeamCollider.localPosition = new Vector3(0f, 1.58f, -1.35f);
                lanternBeamCollider.localScale = new Vector3(0.16f, 0.16f, 0.8f);

                Transform tentBeamCollider = AddCollider(beam, "tent_beam", typeof(BoxCollider));
                tentBeamCollider.localPosition = new Vector3(0f, 1.58f, 0.2f);
                tentBeamCollider.localScale = new Vector3(0.16f, 0.16f, 2.1f);

                LongshipPartController lanternController = lanternBeamCollider.gameObject.AddComponent<LongshipPartController>();
                lanternController.m_name = "Lantern";
                lanternController.m_zdoPartUpgraded = s_lanternUpgraded;
                lanternController.m_zdoPartActive = s_lanternAdded;
                lanternController.m_messageAdd = "Hang";
                lanternController.m_messageRemove = "Remove";
                lanternController.m_nview = m_nview;
                lanternController.m_useDistance = 3f;

                LongshipPartController tentController = tentBeamCollider.gameObject.AddComponent<LongshipPartController>();
                tentController.m_name = "Tent";
                tentController.m_zdoPartUpgraded = s_tentUpgraded;
                tentController.m_zdoPartActive = s_tentAdded;
                tentController.m_messageAdd = "Place";
                tentController.m_messageRemove = "Remove";
                tentController.m_nview = m_nview;
                tentController.m_useDistance = 3f;
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

                for (int i = 0; i < tentColliders.childCount; i++)
                {
                    LongshipPartController tentController = tentColliders.GetChild(i).gameObject.AddComponent<LongshipPartController>();
                    tentController.m_name = "Tent";
                    tentController.m_nview = m_nview;
                    tentController.m_zdoPartVariant = s_tentStyle;
                    tentController.m_variants = 3;
                }
            }

            GameObject lanternItem = ObjectDB.instance.GetItemPrefab("Lantern")?.transform.Find("attach/equiped")?.gameObject;
            if (lanternItem)
            {
                m_lantern = new GameObject("Lantern")
                {
                    layer = 28 // vehicle
                };

                Transform lanternParent = m_lantern.transform;
                lanternParent.SetParent(customize, worldPositionStays: false);
                lanternParent.localScale = Vector3.one * 0.45f;

                Transform lantern = Instantiate(lanternItem, lanternParent).transform;
                lantern.name = "Lamp";
                lantern.localPosition = new Vector3(0.23f, 1.9f, 0f);
                lantern.gameObject.layer = 28; // vehicle

                Light light = lantern.GetComponentInChildren<Light>();
                light.color = new Color(0.957f, 0.78f, 0.684f, 1f);

                m_lightParts = new GameObject[] { light.gameObject, lantern.Find("flare").gameObject };

                m_lampRenderer = lantern.Find("default").GetComponent<MeshRenderer>();

                if (lampSharedMaterial == null)
                {
                    lampSharedMaterial = new Material(m_lampRenderer.sharedMaterial);
                    lampColor = lampSharedMaterial.GetColor("_EmissionColor");
                }

                m_lampRenderer.sharedMaterial = lampSharedMaterial;

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

                LongshipPartController lampController = lanternParent.gameObject.AddComponent<LongshipPartController>();
                lampController.m_name = "Lamp";
                lampController.m_zdoPartActive = s_lightsOn;
                lampController.m_messageAdd = "Light up";
                lampController.m_messageRemove = "Put out";
                lampController.m_nview = m_nview;
                lampController.m_useDistance = 2f;

                lantern.gameObject.SetActive(true);
                m_lantern.SetActive(false);
            }

            GameObject interactables = new GameObject("interactive");

            Transform interactableParent = interactables.transform;
            interactableParent.SetParent(customize, worldPositionStays: false);

            // Mast controller
            if (m_beamMast)
            {
                Transform mastControllerCollider = AddCollider(interactableParent, "mast_controller", typeof(BoxCollider));
                mastControllerCollider.localPosition = new Vector3(-0.05f, 0.08f, 0f);
                mastControllerCollider.localScale = new Vector3(0.6f, 0.17f, 0.26f);

                m_mastUpgrade = mastControllerCollider.gameObject;
                m_mastUpgrade.layer = 16; // piece_nonsolid
                m_mastUpgrade.SetActive(true);

                LongshipPartController mastController = mastControllerCollider.gameObject.AddComponent<LongshipPartController>();
                mastController.m_name = "Mast";
                mastController.m_zdoPartUpgraded = s_mastUpgraded;
                mastController.m_zdoPartActive = s_mastRemoved;
                mastController.m_messageAdd = "Remove";
                mastController.m_messageRemove = "Put up";
                mastController.m_nview = m_nview;
                mastController.m_useDistance = 2.5f;
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
                storageController.m_zdoPartUpgraded = s_containerUpgradedLvl1;
                storageController.m_zdoPartUpgradedLvl2 = s_containerUpgradedLvl2;
                storageController.m_nview = m_nview;

                m_storageUpgrade = storageUpgradeCollider.gameObject;
                m_storageUpgrade.layer = 16; // piece_nonsolid
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
                    healthController.m_name = "Hull";
                    healthController.m_zdoPartUpgraded = s_healthUpgraded;
                    healthController.m_zdoPartUpgradedLvl2 = s_ashlandsUpgraded;
                    healthController.m_nview = m_nview;
                    healthController.m_zdoPartVariant = s_shieldsStyle;
                    healthController.m_variants = 4;

                    healthUpgradeCollider.gameObject.SetActive(true);
                    healthUpgradeCollider.gameObject.layer = 16; // piece_nonsolid
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
                storageUpgradeCollider4.localPosition = new Vector3(-0.23f, 0.27f, -1.16f);
                storageUpgradeCollider4.localScale = new Vector3(0.92f, 0.45f, 0.05f);
                storageUpgradeCollider4.localEulerAngles = new Vector3(0, 2f, 0f);

                Transform storageUpgradeCollider5 = AddCollider(shieldsParent, "shield_controller_left (2)", typeof(BoxCollider));
                storageUpgradeCollider5.localPosition = new Vector3(1.3f, 0.23f, -1.1f);
                storageUpgradeCollider5.localScale = new Vector3(1.4f, 0.45f, 0.05f);
                storageUpgradeCollider5.localEulerAngles = new Vector3(0, 352f, 0f);

                for (int i = 0; i < shieldsParent.childCount; i++)
                {
                    LongshipPartController shieldController = shieldsParent.GetChild(i).gameObject.AddComponent<LongshipPartController>();
                    shieldController.m_name = "Shields";
                    shieldController.m_nview = m_nview;
                    shieldController.m_zdoPartVariant = s_shieldsStyle;
                    shieldController.m_variants = 4;
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
                headsControllerCollider.localPosition = new Vector3(-3.50f, 0.37f, 0f);
                headsControllerCollider.localScale = new Vector3(0.04f, 0.35f, 0.60f);
                headsControllerCollider.localEulerAngles = new Vector3(0f, 0f, 63f);

                headsControllerCollider.gameObject.layer = 16; // piece_nonsolid
                headsControllerCollider.gameObject.SetActive(true);

                LongshipPartController headsController = headsControllerCollider.gameObject.AddComponent<LongshipPartController>();
                headsController.m_name = "Head";
                headsController.m_nview = m_nview;
                headsController.m_zdoPartVariant = s_headStyle;
                headsController.m_variants = 4;
            }
            // TODO
            // upgrade delay
            // upgrade cost
            // upgrade return
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
            lampSharedMaterial = null;
        }

        private static void FixPrefab()
        {
            if (prefabFixed)
                return;

            prefabFixed = true;

            GameObject prefab = Resources.FindObjectsOfTypeAll<Ship>().FirstOrDefault(ws => ws.name == prefabName)?.gameObject;
            if (prefab == null)
                return;

            Shader standard = prefab.transform.Find("ship/visual/Customize/TraderLamp/fi_vil_light_lamp01_03").GetComponent<MeshRenderer>().sharedMaterial.shader;

            // Flickering fix
            prefab.transform.Find("ship/visual/Customize/storage/default (4)").GetComponent<MeshRenderer>().sharedMaterial.shader = standard;
            prefab.transform.Find("ship/visual/hull_worn/plank").GetComponent<MeshRenderer>().sharedMaterial.shader = standard;
            
            Transform beam = prefab.transform.Find("ship/visual/Customize/ShipTen2_beam");
            beam.localPosition += new Vector3(0f, 0.1f, 0f);
            beam.gameObject.layer = 28; // vehicle

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
            
            collider.gameObject.layer = 28; // vehicle

            return collider;
        }
    }
}
