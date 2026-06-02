using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CustomMaps.GetCoursesInfo;
using static CustomMaps.Plugin;
using Newtonsoft.Json;

namespace CustomMaps
{
    class AssetImports
    {
        public static Sprite LoadSprite(string imagePath)
        {
            byte[] imageData = File.ReadAllBytes(imagePath);

            Texture2D tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, imageData);

            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f) // pivot point (centered)
            );
        }

        //Loads all of the scenes and asset bundles
        public static void LoadBundles()
        {
            string modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var searchFolders = new[]
            {
                Path.Combine(modFolder, "Maps")
            };

            foreach (string rootFolder in searchFolders)
            {
                if (!Directory.Exists(rootFolder))
                {
                    Plugin.Log.LogWarning("Folder not found, skipping: " + rootFolder);
                    continue;
                }

                // Top-level files + one level of subfolders
                var bundlePaths = Directory.GetFiles(rootFolder)
                    .Concat(Directory.GetDirectories(rootFolder)
                    .SelectMany(subDir => Directory.GetFiles(subDir)))
                    .Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

                foreach (string bundlePath in bundlePaths)
                {
                    var bundle = AssetBundle.LoadFromFile(bundlePath);
                    if (bundle == null)
                    {
                        Plugin.Log.LogError("Bundle failed to load: " + bundlePath);
                        continue;
                    }

                    string[] scenePaths = bundle.GetAllScenePaths();

                    if (scenePaths.Length > 0)
                    {
                        // Scene bundle — register one entry per scene
                        foreach (string scenePath in scenePaths)
                        {
                            string guid = Guid.NewGuid().ToString("N");
                            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

                            Plugin.LoadedBundles.Add(new LoadedBundle
                            {
                                Bundle = bundle,
                                ScenePath = scenePath,
                                SceneGuid = guid
                            });

                            Plugin.SceneNameToGuid[sceneName] = guid;
                            Plugin.SceneBundles[sceneName] = bundlePath;
                            Plugin.Log.LogDebug($"Scene registered: {scenePath} guid: {guid}");
                            
                        }
                    }
                    else
                    {
                        // Asset bundle — retain it, no scene info
                        Plugin.LoadedBundles.Add(new LoadedBundle
                        {
                            Bundle = bundle,
                            ScenePath = null,
                            SceneGuid = null
                        });

                        Plugin.Log.LogDebug("Asset bundle loaded: " + bundlePath);
                    }
                }
            }
        }

    }

    //Loads the configurations for the holes from the json files
    public static class ConfigLoader
    {
        public static List<(string bundleFileName, BundleConfig config)> LoadAllConfigs()
        {
            string mapsFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Maps");
            var results = new List<(string, BundleConfig?)>();
            var bundlePaths = Directory.GetFiles(mapsFolder)
                .Concat(Directory.GetDirectories(mapsFolder)
                    .SelectMany(subDir => Directory.GetFiles(subDir)))
                .Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

            foreach (var bundlePath in bundlePaths)
            {
                string fileName = Path.GetFileName(bundlePath);
                string configPath = bundlePath + ".json";

                //No file
                if (!File.Exists(configPath))
                {
                    Plugin.Log.LogWarning($"No config found for bundle: {fileName}, using filler");
                    results.Add((fileName, null));
                    continue;
                }

                //File failed to read
                string json;
                try
                {
                    json = File.ReadAllText(configPath, Encoding.UTF8).TrimStart('\uFEFF');
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to read config file for {fileName}: {e.Message}, using filler");
                    results.Add((fileName, null));
                    continue;
                }

                //File failed to parse
                BundleConfig config;
                try
                {
                    config = JsonConvert.DeserializeObject<BundleConfig>(json);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to parse config for {fileName}: {e.Message}, using filler");
                    results.Add((fileName, null));
                    continue;
                }

                //No/null holes in file
                if (config?.holes == null || config.holes.Count == 0)
                {
                    Plugin.Log.LogWarning($"No holes defined in config for {fileName}, using filler");
                    results.Add((fileName, null));
                    continue;
                }

                //Add holes to list
                Plugin.Log.LogDebug($"Loaded config for {fileName}: {config.holes.Count} enabled hole(s)");
                results.Add((fileName, config));
            }

            return results;
        }
    }

    //JSON file
    [Serializable]
    public class HoleConfig
    {
        public string sceneName; //Has to match unity
        public string holeName = "No name given"; //display name in game
        public int par = 4;
        public string difficulty = "None"; // "Beginner", "Intermediate", "Expert", "None"
        public bool enabled = true; //show in the game

    }

    //Also JSON file
    [Serializable]
    public class BundleConfig
    {
        public List<HoleConfig> holes = new List<HoleConfig>();
    }

}
    
