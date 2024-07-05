using BepInEx;
using BepInEx.Bootstrap;
using Reptile;
using HarmonyLib;
using Sicklines.App;
using Sicklines.AiPaths;
using UnityEngine;
using System.Collections;

namespace Sicklines
{
    //[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInPlugin("Ariki.Sicklines", "Sicklines", PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(CommonAPIGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(EmailAPIGUID, BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string CharacterAPIGuid = "com.Viliger.CharacterAPI";
        private const string CommonAPIGUID = "CommonAPI";
        private const string EmailAPIGUID = "EmailApi";

        private void Awake()
        {

            Logger.LogMessage($"{PluginInfo.PLUGIN_GUID} v{PluginInfo.PLUGIN_VERSION} starting...");

            if (true)
            {
                Harmony harmony = new Harmony("ariki.Sicklines");
                harmony.PatchAll();

            }

            Logger.LogMessage($"Init SaveManager");
            SickLines_SaveManager.Initialize();

            Logger.LogMessage($"Init PathConstructor");
            PathConstructor.Initialize(Config);

            Logger.LogMessage($"Init Phone App");
            SickLinesApp.Initialize();

            Logger.LogMessage($"Init Emails");
            SickLine_Emails.Initialize();
        }

        private void Update()
        {
            float time = Time.deltaTime;

            if (PathConstructor.CurrentPath != null) //Has Active Path
            {
                //Has started a Combo and should test for end
                if (PathConstructor.newPath && PathConstructor._target.IsComboing())
                {
                    PathConstructor.newPath = false;
                    PathConstructor.startedCombo = true;

                    return;
                }

                if (PathConstructor.startedCombo && !PathConstructor._target.IsComboing())
                {
                    //Combo Ended stop Path
                    ResetPosition(1.0f);
                    PathConstructor.startedCombo = false;
                }

            }
        }


        public void ResetPosition(float delay)
        {
            StartCoroutine(DelayResetPosition(delay));
        }

        private IEnumerator DelayResetPosition(float delay)
        {
            yield return new WaitForSeconds(delay);
            PathConstructor.endPath(true);
        }

        private IEnumerator LoopAddAirWaypoint(float delay)
        {
            yield return new WaitForSeconds(delay);
            PathConstructor.endPath(true);
        }
    }
}
