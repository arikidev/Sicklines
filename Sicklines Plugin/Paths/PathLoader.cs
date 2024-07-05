using UnityEngine;
using Reptile;
using System.IO;
using System.Collections.Generic;
using System;
using BepInEx.Logging;
using BepInEx;
using BepInEx.Configuration;
using Sicklines.Asset;
using SicklinesMono;
using CommonAPI;
using System.Collections;
//using Reptile.Utility;

namespace Sicklines.AiPaths
{

    public class PathLoader
    {

        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} PathLoader");

        private static readonly string CONFIG_PATH = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME);

        public static PathLoader Instance { get; private set; }

        //list of avaliable paths with scene

        public static Dictionary<string, Stage> _PathFiles;

        public List<string> pathsToLoad;
        private static Dictionary<CustomAiPath, string> pathsLoaded;
        
        //list of paths to be loaded
        //list of loaded paths

        //Init
        public PathLoader()
        {
            Instance = this;

            _PathFiles = new Dictionary<string, Stage>();

            pathsToLoad = new List<string>();
            pathsLoaded = new Dictionary<CustomAiPath, string>();

            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
                DebugLog.LogWarning($"No config directory found creating.");
                return;
            }

            DebugLog.LogMessage($"Load Paths from: {CONFIG_PATH} ");

            GatherPathFiles();

        }

        public void GatherPathFiles()
        {
            string[] files = Directory.GetFiles(CONFIG_PATH, "*.path");
            _PathFiles.Clear();

            foreach (string filePath in files)
            {
                // Ideally This should be in the file header :(
                //DebugLog.LogMessage($" found path: {filePath} ");

                string fileName = Path.GetFileName(filePath);
                string sceneName = fileName.Split(@".".ToCharArray())[1];

               // DebugLog.LogMessage($"tryParse | {sceneName} ");

                Stage stage;
                if (Enum.TryParse(sceneName, out stage))
                {
                    _PathFiles.Add(filePath, stage);

                   // DebugLog.LogMessage($"Added Path | {stage} : {filePath}");
                }
                else 
                {
                    DebugLog.LogWarning($" Failed to add path: {filePath} ");
                }
            }
        }
        //Functions

        //Make a lazy load system where paths will be loaded overtime

        // Init on scene loaded clear and add to paths to be loaded
        //  Request path load from load queue
        //  Remove from queue after load
        //

        //Load random path from scene A

        public void OnNewScene()
        {
            
            GatherPathFiles();

            Stage stage = Reptile.Utility.GetCurrentStage();

            pathsToLoad.Clear();
            //Get all paths to Load
            foreach (KeyValuePair<string, Stage> pair in _PathFiles)
            {
                if (pair.Value == stage)
                {
                    pathsToLoad.Add(pair.Key);
                }
            }

            GameObject newRoot = new GameObject($"PathLoader");
            PathLoaderObject obj = newRoot.AddComponent<PathLoaderObject>();
            obj.Init(this);
        }

        public bool LoadPath(string pathFile)
        {
            GameObject newRoot = new GameObject($"LoadedCustomPath_");

            CustomAiPath custPath = new CustomAiPath(newRoot);

            bool success = custPath.loadAiPathFromJsonFile(pathFile);

            pathsToLoad.Remove(pathFile);

            if (success)
            {
                SickLines_Trigger.Linestate state = SickLines_Save.Instance.hasCompletedPath(pathFile) ? SickLines_Trigger.Linestate.Complete : SickLines_Trigger.Linestate.unCompleted;
                PathConstructor.setupLineTrigger(custPath, state);
                pathsLoaded.Add(custPath , pathFile);
            }
            
            return success;
        }

        public bool tryGetPathfileFromCustomPath(CustomAiPath obj, out string pathfile)
        {
            pathfile = "";
            return pathsLoaded.TryGetValue(obj, out pathfile);
        }
    }

    public class PathLoaderObject : MonoBehaviour
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} PathLoaderObject");


        PathLoader loader;

        public void Init(PathLoader _loader)
        {
            //DebugLog.LogMessage("Init PathLoaderObject");

            loader = _loader;

            StartCoroutine(LazyLoadPathCoroutine());
        }

        IEnumerator LazyLoadPathCoroutine()
        {
            //DebugLog.LogMessage("LazyLoadPathCorutine");

            while (loader.pathsToLoad.Count > 0)
            {
                //Try load path
                loader.LoadPath(loader.pathsToLoad[loader.pathsToLoad.Count - 1]);
                yield return null;
            }
        }
    }
}
