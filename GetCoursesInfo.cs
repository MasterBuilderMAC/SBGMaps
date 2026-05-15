using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Localization;
using Eflatun.SceneReference;
using Mono.Cecil;

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


            //TODO: Holes. For now, 1 for proof of concept
            HoleData holeData = ScriptableObject.CreateInstance<HoleData>();

            Traverse tHole = Traverse.Create(holeData);

            LocalizedString localizedHoleName = new LocalizedString("UI", "Mac.TestMapName");
            Plugin.CustomLocalizedStrings["Mac.TestMapName"] = "Not Hilltops";
            tHole.Property("LocalizedName").SetValue(localizedHoleName);

            holeData.name = "Not Hilltops";

            SceneReference sceneReference = new SceneReference(Plugin.SceneGuids[0]); //scene guid doesnt matter, injected from patches
            tHole.Property("Scene").SetValue(sceneReference);

            tHole.Property("Par").SetValue(12);
            tHole.Property("Difficulty").SetValue(HoleData.DifficultyLevel.Expert);

            //TODO: Screenshots thumbnail, using placeholder
            List<Sprite> sprites = new List<Sprite>();
            //imagePath = Path.Combine(modFolder, "Assets\\courseIcon.png");
            sprites.Add(courseIcon);
            tHole.Property("ScreenshotsThumbnail").SetValue(sprites);

            //TODO: Event reference music event
            var musicGuid = new FMOD.GUID
            {
                Data1 = -55891042,  
                Data2 = 1301584857,
                Data3 = 1030450600,
                Data4 = 1380137089
            };

            var musicEvent = new FMODUnity.EventReference
            {Guid = musicGuid};
            tHole.Property("MusicEvent").SetValue(musicEvent);

            HoleData[] allHoles = [holeData];
            tCourse.Property("Holes").SetValue(allHoles);



            Plugin.CustomCourse = courseData;

            return courseData;
        }
    }

    
}
