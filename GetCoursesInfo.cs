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
        
        public static CourseData getCourse()
        {
            string imagePath = "";
            string modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            CourseData courseData = ScriptableObject.CreateInstance<CourseData>();

            Traverse tCourse = Traverse.Create(courseData);

            LocalizedString localizedCourseName = new LocalizedString("Custom Courses", "Custom Courses");
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

            //TODO: post processing (optional?)

            tCourse.Field("includeAllHoles").SetValue(false);
            tCourse.Field("difficultyCourse").SetValue(false);


            //TODO: Holes. For now, 1 for proof of concept
            HoleData holeData = new HoleData();

            Traverse tHole = Traverse.Create(holeData);

            LocalizedString localizedHoleName = new LocalizedString("Custom Hole", "Custom Hole");
            tHole.Property("LocalizedName").SetValue(localizedHoleName);

            //TODO: Scene reference
            SceneReference sceneReference = new SceneReference(); //add valid scene # to args
            tHole.Property("Scene").SetValue(sceneReference);

            tHole.Property("Par").SetValue(12);
            tHole.Property("Difficulty").SetValue(HoleData.DifficultyLevel.Expert);

            //TODO: Screenshots thumbnail, using placeholder
            List<Sprite> sprites = new List<Sprite>();
            //imagePath = Path.Combine(modFolder, "Assets\\courseIcon.png");
            sprites.Add(courseIcon);
            tHole.Property("ScreenshotsThumbnail").SetValue(sprites);

            //TODO: Event reference music event

            HoleData[] allHoles = [holeData];
            tCourse.Property("Holes").SetValue(allHoles);

            return courseData;
        }
    }

    
}
