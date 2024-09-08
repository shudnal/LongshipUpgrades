using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace LongshipUpgrades
{
    internal class LongshipCustomizableParts : MonoBehaviour
    {
        public static bool prefabInit = false;

        private ZNetView m_nview;
        private GameObject m_beamMast;
        private GameObject m_insects;
        private GameObject m_fireWarmth;
        private GameObject m_tent;
        private GameObject m_lantern;
        
        private GameObject[] m_lightParts;

        public const string prefabName = "VikingShip";
        private static bool prefabFixed = false;

        private static Material lampSharedMaterial;
        private static Color lampColor;
        private static bool isTimeForInsects;
        private static bool isTimeToLight = true;

        private void Awake()
        {
            m_nview = GetComponent<ZNetView>();
        }

        private void Start()
        {
            if (prefabInit)
                return;

            Transform beam = transform.Find("ship/visual/Customize/ShipTen2_beam");
            if (beam)
            {
                Transform beamCollider = AddCollider(beam, "collider", typeof(BoxCollider));
                beamCollider.localPosition = new Vector3(0f, 1.6f, -0.25f);
                beamCollider.localScale = new Vector3(0.15f, 0.15f, 3f);

                m_beamMast = Instantiate(beam.gameObject, beam.parent);
                m_beamMast.name = "ShipTen2_mast";
                m_beamMast.transform.localEulerAngles += new Vector3(90f, 0.1f, 0f);
                m_beamMast.transform.localPosition = new Vector3(0.58f, 0.35f, 0f);
                m_beamMast.SetActive(false);

                beam.localPosition += new Vector3(0.1f, 0f, 0f);

                m_tent = transform.Find("ship/visual/Customize/ShipTen2 (1)").gameObject;
                Transform tentColliders = new GameObject("colliders").transform;
                tentColliders.SetParent(m_tent.transform, false);

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

                Transform lamp = transform.Find("ship/visual/Customize/TraderLamp");
                lamp.gameObject.SetActive(false);

                m_lantern = new GameObject("Lantern")
                {
                    layer = 28 // vehicle
                };

                Transform lanternParent = m_lantern.transform;
                lanternParent.SetParent(transform.Find("ship/visual/Customize"), false);
                lanternParent.localScale = Vector3.one * 0.45f;

                GameObject lanternItem = ObjectDB.instance.GetItemPrefab("Lantern");
                Transform lantern = Instantiate(lanternItem.transform.Find("attach/equiped").gameObject, lanternParent).transform;
                lantern.name = "Lamp";
                lantern.localPosition = new Vector3(0.23f, 1.9f, 0f);
                lantern.gameObject.layer = 28; // vehicle

                Light light = lantern.GetComponentInChildren<Light>();
                light.color = lamp.GetComponentInChildren<Light>().color;

                m_lightParts = new GameObject[] { light.gameObject, lantern.Find("flare").gameObject };

                MeshRenderer lampRenderer = lantern.Find("default").GetComponent<MeshRenderer>();

                if (lampSharedMaterial == null)
                {
                    lampSharedMaterial = new Material(lampRenderer.sharedMaterial);
                    lampColor = lampSharedMaterial.GetColor("_EmissionColor");
                }

                lampRenderer.sharedMaterial = lampSharedMaterial;

                ConfigurableJoint joint = lantern.GetComponent<ConfigurableJoint>();
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedBody = GetComponent<Rigidbody>();
                joint.connectedAnchor = new Vector3(0f, 3.01f, -0.45f);

                Transform lanternCollider = AddCollider(lanternParent, "collider", typeof(BoxCollider));
                lanternCollider.localPosition = new Vector3(0.2f, 2f, 0f);
                lanternCollider.localScale = new Vector3(0.02f, 0.02f, 0.5f);
                lanternCollider.localEulerAngles = new Vector3(90f, 0f, 0f);

                m_insects = Instantiate(lamp.Find("insects").gameObject, lanternParent);
                m_insects.name = "insects";
                m_insects.SetActive(false);
                m_insects.transform.localPosition = new Vector3(0.42f, 1.85f, 0f);
                m_insects.layer = 8; // effect

                m_fireWarmth = AddCollider(lanternParent, "FireWarmth", typeof(SphereCollider)).gameObject;
                m_fireWarmth.layer = 14; // character_trigger
                m_fireWarmth.transform.localPosition = new Vector3(-1f, 0f, 0f);

                SphereCollider fireWarmthCollider = m_fireWarmth.GetComponent<SphereCollider>();
                fireWarmthCollider.radius = 3;
                fireWarmthCollider.isTrigger = true;

                EffectArea fireWarmth = m_fireWarmth.gameObject.AddComponent<EffectArea>();
                fireWarmth.m_type = EffectArea.Type.Fire | EffectArea.Type.Heat;

                lantern.gameObject.SetActive(true);

                // TODO interactive
                // Mast removal (ship/colliders/mast/Cube)
                // Light
                // storage
                // tent
                // shields
                // heads
                // On ship destroy spawn spent mats
            }
        }

        private void FixedUpdate()
        {
            if (m_lantern.activeInHierarchy && (isTimeForInsects != IsTimeForInsects() || isTimeToLight != IsTimeToLight()))
            {
                isTimeForInsects = IsTimeForInsects();
                isTimeToLight = IsTimeToLight();
                UpdateLights();
            }
        }

        private void UpdateLights()
        {
            m_insects?.SetActive(isTimeForInsects);

            m_lightParts?.Do(part => part?.SetActive(isTimeToLight));
            lampSharedMaterial?.SetColor("_EmissionColor", isTimeToLight ? lampColor : Color.grey);
        }

        private static bool IsTimeForInsects()
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
            isTimeForInsects = false;
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

            prefab.transform.Find("ship/visual/Customize").gameObject.SetActive(true);
            prefab.transform.Find("ship/visual/Customize/storage/default (4)").GetComponent<MeshRenderer>().sharedMaterial.shader =
                prefab.transform.Find("ship/visual/Customize/TraderLamp/fi_vil_light_lamp01_03").GetComponent<MeshRenderer>().sharedMaterial.shader;

            Transform beam = prefab.transform.Find("ship/visual/Customize/ShipTen2_beam");
            beam.localPosition += new Vector3(0f, 0.1f, 0f);
            beam.gameObject.layer = 28; // vehicle

            Transform tent = prefab.transform.Find("ship/visual/Customize/ShipTen2 (1)");
            tent.localPosition += new Vector3(0f, 0.08f, 0f);

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
            collider.SetParent(transform, false);
            
            collider.gameObject.layer = 28; // vehicle

            return collider;
        }
    }
}
