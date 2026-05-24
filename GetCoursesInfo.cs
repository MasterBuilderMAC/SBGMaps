using Eflatun.SceneReference;
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Localization;

namespace CustomMaps
{
    static public class GetCoursesInfo
    {

        //Create the course, the custom tab at the top
        public static CourseData getCourse()
        {
            
            string imagePath = "";
            string modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            CourseData courseData = ScriptableObject.CreateInstance<CourseData>();

            courseData.name = "Custom Courses";

            Traverse tCourse = Traverse.Create(courseData);

            LocalizedString localizedCourseName = new LocalizedString("UI", "Mac.CustomMaps");
            Plugin.CustomLocalizedStrings["Mac.CustomMaps"] = "Custom Holes";
            tCourse.Property("LocalizedName").SetValue(localizedCourseName);

            
            imagePath = Path.Combine(modFolder, "Assets\\courseIcon.png");
            Sprite courseIcon = AssetImports.LoadSprite(imagePath);
            tCourse.Property("CategoryIcon").SetValue(courseIcon);

            imagePath = Path.Combine(modFolder, "Assets\\menuBackground.png");
            Sprite menuBackground = AssetImports.LoadSprite(imagePath);
            tCourse.Property("MenuBackground").SetValue(menuBackground);

            Color holeColor = Color.green;
            tCourse.Property("HoleLabelColor").SetValue(holeColor);

            Color windBGColor = Color.red;
            tCourse.Property("WindBackroundColor").SetValue(windBGColor);

            //TODO: wind vfx

            tCourse.Property("WindAmbienceType").SetValue(WindManager.WindAudioAmbienceType.Default);

            //no post processing, null in all courses

            tCourse.Field("includeAllHoles").SetValue(false);
            tCourse.Field("difficultyCourse").SetValue(false);


            //Holes
            HoleData[] allHolesData = CreateHolesFromConfig(courseIcon);

            tCourse.Property("Holes").SetValue(allHolesData);



            Plugin.CustomCourse = courseData;

            return courseData;
        }

        static HoleData[] CreateHolesFromConfig(Sprite courseIcon)
        {
            var allHoles = new List<HoleData>();
            var configs = ConfigLoader.LoadAllConfigs();

            foreach (var (bundleFileName, config) in configs)
            {
                // Get all scene guids that belong to this bundle
                var bundleBaseName = Path.GetFileNameWithoutExtension(bundleFileName);
                var bundleScenes = Plugin.SceneNameToGuid
                    .Where(kvp => Plugin.SceneBundles.TryGetValue(kvp.Key, out var b) &&
                                  Path.GetFileNameWithoutExtension(b) == bundleBaseName)
                    .ToList();

                // If config is null or has no holes, create filler for every scene in the bundle
                if (config?.holes == null || config.holes.Count == 0)
                {
                    Plugin.Log.LogWarning($"No valid config for {bundleFileName}, using filler for all {bundleScenes.Count} scenes");
                    foreach (var kvp in bundleScenes)
                        allHoles.Add(CreateFillerHole(kvp.Key, kvp.Value, bundleBaseName, courseIcon));
                    continue;
                }

                foreach (var holeConfig in config.holes)
                {
                    // Validate scene name
                    if (!Plugin.SceneNameToGuid.TryGetValue(holeConfig.sceneName, out string sceneGuid))
                    {
                        Plugin.Log.LogError($"No scene guid for '{holeConfig.sceneName}' in {bundleFileName}, using filler");
                        // Use first available scene from bundle as fallback
                        var fallback = bundleScenes.FirstOrDefault();
                        if (fallback.Key != null)
                            allHoles.Add(CreateFillerHole(fallback.Key, fallback.Value, bundleBaseName, courseIcon));
                        continue;
                    }

                    // Validate and fill individual fields
                    string holeName = string.IsNullOrEmpty(holeConfig.holeName) ? bundleBaseName : holeConfig.holeName;

                    int par = holeConfig.par > 0 ? holeConfig.par : 4;

                    HoleData.DifficultyLevel difficulty;
                    if (!Enum.TryParse(holeConfig.difficulty, out difficulty))
                    {
                        Plugin.Log.LogWarning($"Invalid difficulty '{holeConfig.difficulty}' in {bundleFileName}, defaulting to None");
                        difficulty = HoleData.DifficultyLevel.None;
                    }

                    var holeData = ScriptableObject.CreateInstance<HoleData>();
                    var tHole = Traverse.Create(holeData);

                    string locKey = $"Mac.{holeConfig.sceneName}";
                    Plugin.CustomLocalizedStrings[locKey] = holeName;
                    tHole.Property("LocalizedName").SetValue(new LocalizedString("UI", locKey));
                    holeData.name = holeName;
                    tHole.Property("Scene").SetValue(new SceneReference(sceneGuid));
                    tHole.Property("Par").SetValue(par);
                    tHole.Property("Difficulty").SetValue(difficulty);
                    tHole.Property("ScreenshotsThumbnail").SetValue(new List<Sprite> { courseIcon });

                    var musicGuid = new FMOD.GUID { Data1 = -55891042, Data2 = 1301584857, Data3 = 1030450600, Data4 = 1380137089 };
                    tHole.Property("MusicEvent").SetValue(new FMODUnity.EventReference { Guid = musicGuid });

                    allHoles.Add(holeData);
                    Plugin.Log.LogInfo($"Loaded hole: {holeName} (scene: {holeConfig.sceneName}, par: {par})");
                }
            }

            return allHoles.ToArray();
        }

        static HoleData CreateFillerHole(string sceneName, string sceneGuid, string bundleName, Sprite courseIcon)
        {
            var holeData = ScriptableObject.CreateInstance<HoleData>();
            var tHole = Traverse.Create(holeData);

            string locKey = $"FILLER_TEMPLATE.{sceneName}";
            Plugin.CustomLocalizedStrings[locKey] = $"{bundleName}.{sceneName}";
            tHole.Property("LocalizedName").SetValue(new LocalizedString("UI", locKey));
            holeData.name = $"{bundleName}.{sceneName}";
            tHole.Property("Scene").SetValue(new SceneReference(sceneGuid));
            tHole.Property("Par").SetValue(4);
            tHole.Property("Difficulty").SetValue(HoleData.DifficultyLevel.None);
            tHole.Property("ScreenshotsThumbnail").SetValue(new List<Sprite> { courseIcon });

            var musicGuid = new FMOD.GUID { Data1 = -55891042, Data2 = 1301584857, Data3 = 1030450600, Data4 = 1380137089 };
            tHole.Property("MusicEvent").SetValue(new FMODUnity.EventReference { Guid = musicGuid });

            Plugin.Log.LogWarning($"Created filler hole for scene '{sceneName}' from bundle '{bundleName}'");
            return holeData;
        }
    }

    
}
