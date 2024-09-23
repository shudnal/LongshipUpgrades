using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace LongshipUpgrades
{
    public static class LongshipPropertyBlocks
    {
        public static readonly int _StyleTex = Shader.PropertyToID("_StyleTex");

        private static readonly Dictionary<MaterialMan.PropertyContainer, GameObject> s_objectProperties = new Dictionary<MaterialMan.PropertyContainer, GameObject>();

        [HarmonyPatch(typeof(MaterialMan), nameof(MaterialMan.RegisterRenderers))]
        public static class MaterialMan_RegisterRenderers_KeepPropertyBlocks
        {
            public static void Postfix(MaterialMan __instance, GameObject gameObject)
            {
                if (!LongshipCustomizableParts.HasComponent(gameObject))
                    return;

                if (__instance.m_blocks.TryGetValue(gameObject.GetInstanceID(), out MaterialMan.PropertyContainer propertyContainer))
                    s_objectProperties[propertyContainer] = gameObject;
            }
        }

        [HarmonyPatch(typeof(MaterialMan), nameof(MaterialMan.ResetValue))]
        public static class MaterialMan_ResetValue_KeepPropertyBlocks
        {
            public static void Postfix(MaterialMan __instance, GameObject go)
            {
                if (!LongshipCustomizableParts.HasComponent(go))
                    return;

                if (__instance.m_blocks.TryGetValue(go.GetInstanceID(), out MaterialMan.PropertyContainer propertyContainer))
                    s_objectProperties[propertyContainer] = go;
            }
        }

        [HarmonyPatch(typeof(MaterialMan.PropertyContainer), nameof(MaterialMan.PropertyContainer.UpdateBlock))]
        public static class MaterialMan_PropertyContainer_UpdateBlock_AfterPropertyBlock
        {
            public static void Postfix(MaterialMan.PropertyContainer __instance, MaterialPropertyBlock ___m_propertyBlock)
            {
                if (s_objectProperties.TryGetValue(__instance, out GameObject go))
                {
                    if (!___m_propertyBlock.isEmpty)
                        LongshipCustomizableParts.CombinePropertyBlocks(go, ___m_propertyBlock);
                    else
                        LongshipCustomizableParts.UpdatePropertyBlocks(go);
                }
            }
        }

        [HarmonyPatch(typeof(MaterialMan), nameof(MaterialMan.UnregisterRenderers))]
        public static class MaterialMan_UnregisterRenderers_Remove
        {
            public static void Prefix(MaterialMan __instance, GameObject gameObject)
            {
                if (__instance.m_blocks.TryGetValue(gameObject.GetInstanceID(), out MaterialMan.PropertyContainer propertyContainer))
                    s_objectProperties.Remove(propertyContainer);
            }
        }
    }
}
