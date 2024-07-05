using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Reptile;
using UnityEngine;
using BepInEx.Logging;
using Sicklines.AiPaths;

namespace Sicklines.AiPaths
{
    [HarmonyPatch(typeof(WorldHandler))]
    public class WorldHandlerPatches
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} WorldHandler Patches");


        [HarmonyPrefix]
        [HarmonyPatch("SetupAIPlayerAt")]
        public static void SetupAIPlayerAtPatch(WorldHandler __instance, Transform spawner, Characters character, PlayerType playerType)
        {
            DebugLog.LogMessage($"{__instance} | {spawner} | {character} | {playerType}");
        }

    }

    [HarmonyPatch(typeof(StageManager))]
    public class StageManagerPatches
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} StageManager");


        [HarmonyPostfix]
        [HarmonyPatch("SetupStage")]
        public static void SetupStagePatch()
        {
            DebugLog.LogMessage("Setup Stage");
            PathConstructor.pathLoader.OnNewScene();
        }

    }


}
