using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using CustomMaps;

namespace CustomMaps
{
    internal class Patches
    {
    }

   
    
    [HarmonyPatch(typeof(CourseCollection), nameof(CourseCollection.RuntimeInitialize))]
    public static class CourseCollection_RuntimeInitialize_prefix
    {
        static void Prefix(CourseCollection __instance)
        {

            CourseData course = GetCoursesInfo.getCourse();

            Plugin.Log.LogDebug("SBGMaps - course collection init ran");
            

            // Append your courses to the array before the original loop runs
            var expanded = new CourseData[__instance.Courses.Length + 1];
            __instance.Courses.CopyTo(expanded, 0);
            expanded[expanded.Length - 1] = course;

            // Use Traverse to write back since the setter is private
            Traverse.Create(__instance).Property("Courses").SetValue(expanded);
        }
    }
    
}
