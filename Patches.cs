using BepInEx.Logging;
using CustomMaps;
using Eflatun.SceneReference;
using HarmonyLib;
using JBooth.MicroSplat;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace CustomMaps
{
    internal class Patches
    {
    }

   
    
    [HarmonyPatch(typeof(CourseCollection), nameof(CourseCollection.RuntimeInitialize))]
    public static class CourseCollection_RuntimeInitialize
    {
        static void Prefix(CourseCollection __instance)
        {
            //Force table to load for patching localization later
            var tables = Resources.FindObjectsOfTypeAll<UnityEngine.Localization.Tables.StringTable>();

            //Course is the tab at the top, this one is for custom maps
            CourseData course = GetCoursesInfo.getCourse();

            Plugin.Log.LogDebug("SBGMaps - course created, injecting...");
            

            // Append courses to the array before the original loop runs
            var expanded = new CourseData[__instance.Courses.Length + 1];
            __instance.Courses.CopyTo(expanded, 0);
            expanded[expanded.Length - 1] = course;

            // Use Traverse to write back since the setter is private
            Traverse.Create(__instance).Property("Courses").SetValue(expanded);
        }
    }

    //Patch to add the guid/path combos for custom holes to the dictionary
    [HarmonyPatch(typeof(SceneGuidToPathMapProvider), "GetSceneGuidToPathMap")]
    public static class SceneGuidToPathMapProvider_Patch
    {
        static void Postfix(ref IReadOnlyDictionary<string, string> __result)
        {
            
            // Convert to a mutable dictionary
            var mutable = new Dictionary<string, string>(__result);

            // Add custom scenes
            foreach (var entry in Plugin.LoadedBundles.Where(b => b.IsSceneBundle))
            {
                mutable[entry.SceneGuid] = entry.ScenePath;
            }
            
            //return new dictionary
            __result = mutable;
        }
    }

    //Patch Localization Strings to include our own
    public static class LocalizationFixes
    {
        [HarmonyPatch]
        static class StringTable_Inject_Patch
        {
            static bool _injected = false;

            static MethodBase TargetMethod()
            {
                // Patch OnAfterDeserialize on the base type
                return typeof(UnityEngine.Localization.Tables.StringTable).BaseType
                    .GetMethod("OnAfterDeserialize", BindingFlags.Public | BindingFlags.Instance);
            }

            static void Postfix(object __instance)
            {
                if (_injected) return;

                var table = __instance as UnityEngine.Localization.Tables.StringTable;
                if (table == null || table.TableCollectionName != "UI") return;
                _injected = true;

                foreach (var kvp in Plugin.CustomLocalizedStrings)
                {
                    if (table.GetEntry(kvp.Key) == null)
                        table.AddEntry(kvp.Key, kvp.Value);
                }
            }
        }

    }

    public static class IngamePatches
    {

        //Grabs the postprocess resources from driving range and attaches to the custom hole on load
        //Grabs the text mesh pro assets and attaches to custom hole on load
        //Grabs the microsplat terrain shaders
        public static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            bool isCustomScene = Plugin.LoadedBundles.Any(b => b.IsSceneBundle && b.ScenePath.Contains(scene.name));
            bool isDrivingRange = scene.name.Equals("Driving range");
            Plugin.Log.LogDebug("Scene Loaded: " + scene.name);

            if (!isCustomScene)
            {
                if (isDrivingRange)
                {
                    //Driving range
                    CachePostProcessing();
                    CacheMicroSplat();
                }

                //Vanilla level
                CacheText();
                CacheMaterials();
                CacheShaders();
            }
            else
            {
                //Modded level
                SetPostProcessing();
                SetText();
                SetShaders();
                SetSkybox();
            }
        }

        public static void CachePostProcessing()
        {
            // Cache post process
            var workingLayer = GameObject.FindFirstObjectByType<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
            if (workingLayer != null)
            {
                Plugin.CachedPostProcessResources = Traverse.Create(workingLayer).Field("m_Resources")
                    .GetValue<UnityEngine.Rendering.PostProcessing.PostProcessResources>();
                Plugin.Log.LogDebug("Cached PostProcessResources");
            }
        }
        public static void CacheText()
        {
            //cache text mesh pro
            var allText = Resources.FindObjectsOfTypeAll(typeof(TMPro.TMP_Text));
            foreach (var obj in allText)
            {
                var text = obj as TMPro.TMP_Text;
                if (text == null) continue;
                if (text.font != null && !Plugin.CachedFonts.ContainsKey(text.font.name))
                {
                    Plugin.CachedFonts[text.font.name] = text.font;
                    Plugin.Log.LogDebug("Cached font: " + text.font.name);
                }
            }
        }
        public static void CacheMicroSplat()
        {
            //cache microsplat terrain stuff
            var microSplat = GameObject.FindFirstObjectByType<MicroSplatTerrain>();
            if (microSplat != null)
            {
                Traverse tGet = Traverse.Create(microSplat);
                Plugin.CachedPropData = tGet.Field("propData").GetValue();
                Plugin.CachedKeywordSO = tGet.Field("keywordSO").GetValue();
                Plugin.CachedTemplateMaterial = tGet.Field("templateMaterial").GetValue();
                Plugin.Log.LogDebug("Cached MicroSplat assets");
            }
        }
        public static void CacheMaterials()
        {
            //Used in setShaders
            // Cache all materials from base game scenes
            var allMaterials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var material in allMaterials)
            {
                if (material == null) continue;
                if (!Plugin.CachedMaterials.ContainsKey(material.name))
                {
                    Plugin.CachedMaterials[material.name] = material;
                    //Plugin.Log.LogDebug("Cached material: " + material.name);
                }

            }
        }
        public static void CacheShaders()
        {
            //Cache all shaders from base game scenes
            var allShaders = Resources.FindObjectsOfTypeAll<Shader>();
            foreach (var shader in allShaders)
            {
                if (shader == null) continue;
                if (!Plugin.CachedShaders.ContainsKey(shader.name))
                {
                    Plugin.CachedShaders[shader.name] = shader;
                    //Plugin.Log.LogDebug("Cached shader: " + shader.name);
                }
            }
        }


        public static void SetPostProcessing()
        {
            // Custom scene - replace resources with cached version
            var layer = GameObject.FindFirstObjectByType<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
            if (layer == null)
            {
                Plugin.Log.LogError("No PostProcessLayer found in custom scene");
                return;
            }

            if (Plugin.CachedPostProcessResources != null)
            {
                Traverse.Create(layer).Field("m_Resources").SetValue(Plugin.CachedPostProcessResources);
                Plugin.Log.LogDebug("Replaced PostProcessResources with cached version");
            }
            else
            {
                Plugin.Log.LogError("No cached PostProcessResources available");
            }
        }
        public static void SetText()
        {
            //replace text with cached text
            var customText = Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>();
            foreach (var text in customText)
            {
                if (text.font != null && Plugin.CachedFonts.ContainsKey(text.font.name))
                    text.font = Plugin.CachedFonts[text.font.name];
            }
        }
        public static void SetShaders()
        {
            // Fix broken shaders on all renderers in custom scene
            var customRenderers = GameObject.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var renderer in customRenderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;

                    // If we have a cached copy of this exact material, replace it entirely
                    if (Plugin.CachedMaterials.TryGetValue(mat.name, out var cachedMat))
                    {
                        // Replace shader from cached material
                        mat.shader = cachedMat.shader;
                        //Plugin.Log.LogDebug("Replaced shader on: " + mat.name);
                        continue;
                    }

                    // Otherwise just try to fix the shader by name
                    if (mat.shader != null && Plugin.CachedShaders.TryGetValue(mat.shader.name, out var cachedShader))
                    {
                        mat.shader = cachedShader;
                        //Plugin.Log.LogDebug("Replaced shader by name on: " + mat.name);
                    }
                }
            }
        }
        public static void SetSkybox()
        {
            // Fix skybox material
            var skybox = RenderSettings.skybox;
            Plugin.Log.LogDebug("Skybox material: " + (RenderSettings.skybox == null ? "NULL" : RenderSettings.skybox.name));
            Plugin.Log.LogDebug("Skybox shader: " + (RenderSettings.skybox?.shader == null ? "NULL" : RenderSettings.skybox.shader.name));

            // Always inject skybox - the scene's reference doesn't survive asset bundle loading
            if (Plugin.CachedMaterials.TryGetValue("Unlit_Skybox", out var cachedSkybox))
            {
                RenderSettings.skybox = cachedSkybox;
                DynamicGI.UpdateEnvironment();
                Plugin.Log.LogDebug("Injected skybox: " + cachedSkybox.name);
            }
            else
            {
                Plugin.Log.LogWarning("Unlit_Skybox not in cache");
            }
        }
    }

    //Load microsplatterrain data
    [HarmonyPatch(typeof(MicroSplatTerrain), "Sync")]
    public static class MicroSplatTerrain_Sync_Patch
    {
        static void Prefix(MicroSplatTerrain __instance)
        {
            // Only inject in custom scenes
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!Plugin.LoadedBundles.Any(b => b.IsSceneBundle && b.ScenePath.Contains(activeScene))) return;

            if (Plugin.CachedPropData == null) return;

            Traverse tSet = Traverse.Create(__instance);
            tSet.Field("propData").SetValue(Plugin.CachedPropData);
            tSet.Field("keywordSO").SetValue(Plugin.CachedKeywordSO);
            tSet.Field("templateMaterial").SetValue(Plugin.CachedTemplateMaterial);
            Plugin.Log.LogDebug("MicroSplat assets injected");
        }
    }
}
