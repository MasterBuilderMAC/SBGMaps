using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace CustomMaps
{
    // This BepInAutoPlugin attribute comes from the Hamunii.BepInEx.AutoPlugin
    // NuGet package, and it will generate the BepInPlugin attribute for you!
    // For more info, see https://github.com/Hamunii/BepInEx.AutoPlugin
    [BepInAutoPlugin]
    public partial class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;
        public static Plugin? Instance { get; private set; }

        //for the menu to have the custom tab with names
        public static CourseData CustomCourse { get; set; } = null!;
        public static Dictionary<string, string> CustomLocalizedStrings = new Dictionary<string, string>();

        //stuff cached for reuse
        public static UnityEngine.Rendering.PostProcessing.PostProcessResources? CachedPostProcessResources;
        public static Dictionary<string, TMPro.TMP_FontAsset> CachedFonts = new Dictionary<string, TMPro.TMP_FontAsset>();
        public static object? CachedPropData = null;
        public static object? CachedKeywordSO = null;
        public static object? CachedTemplateMaterial = null;
        public static Dictionary<string, Material> CachedMaterials = new Dictionary<string, Material>();
        public static Dictionary<string, Shader> CachedShaders = new Dictionary<string, Shader>();
        public static Shader GlowShader { get; set; }

        //for loading custom scenes
        public static Dictionary<string, string> SceneNameToGuid = new Dictionary<string, string>();
        public static Dictionary<string, string> SceneBundles = new Dictionary<string, string>();

        public struct LoadedBundle
        {
            public AssetBundle Bundle;
            public string ScenePath; // null if asset bundle
            public string SceneGuid; // null if asset bundle
            public bool IsSceneBundle => ScenePath != null;
        }

        public static List<LoadedBundle> LoadedBundles = new List<LoadedBundle>();
        


        private void Awake()
        {
            Instance = this;
            Log = Logger;
            AssetImports.LoadBundles();

            var harmony = new Harmony("com.github.MasterBuilderMAC.SBGMaps");

            SceneManager.sceneLoaded += IngamePatches.OnSceneLoaded;

            harmony.PatchAll();
            Log.LogInfo($"Plugin {Name} is loaded!");
        }

        

    }


}
