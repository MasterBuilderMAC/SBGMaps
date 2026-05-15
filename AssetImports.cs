using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        public static void LoadScenes()
        {
            string modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            /*
            //Load testsplat for rendering/shader stuff
            var assetBundle = AssetBundle.LoadFromFile(Path.Combine(modFolder, "Assets\\testsplat"));
            if (assetBundle == null)
                Plugin.Log.LogError("Asset bundle failed to load!");
            else
            {
                Plugin.Log.LogDebug("Asset bundle loaded successfully");

                foreach (var name in assetBundle.GetAllAssetNames())
                {
                    Plugin.Log.LogDebug("Asset in asset bundle: " + name);
                }
            }
            */

            //loop through each bundle and load the maps from them
            string mapsFolder = Path.Combine(modFolder, "Maps");
            foreach (string bundlePath in Directory.GetFiles(mapsFolder))
            {
                var sceneBundle = AssetBundle.LoadFromFile(bundlePath);
                if (sceneBundle == null)
                {
                    Plugin.Log.LogError("Scene bundle failed to load: " + bundlePath);
                    continue;
                }

                foreach (var name in sceneBundle.GetAllAssetNames())
                {
                    Plugin.Log.LogDebug("Asset in scene bundle: " + name);
                }

                string[] paths = sceneBundle.GetAllScenePaths();
                if (paths.Length == 0)
                {
                    Plugin.Log.LogDebug("Bundle has no scenes, skipping: " + bundlePath);
                    continue;
                }

                foreach (string scenePath in paths)
                {
                    string guid = Guid.NewGuid().ToString("N"); // "N" format = 32 hex chars, no dashes
                    Plugin.ScenePaths.Add(scenePath);
                    Plugin.SceneGuids.Add(guid);
                    Plugin.Log.LogDebug("Scene registered: " + scenePath + " guid: " + guid);
                }
            }

        }
    }

}
    
