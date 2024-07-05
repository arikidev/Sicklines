using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using BepInEx.Logging;
using Reptile;


namespace Sicklines.Asset
{
    static public class BundleLoader
    {

        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Player Patches");

        public static bool LoadBundle(string filePath, out GameObject[] gameObjects )
        {
            bool success = false;

            gameObjects = null;

            DebugLog.LogMessage("Start Loading Asset Bundle");

            if (File.Exists(filePath))            
            {
                AssetBundle bundle = null;
                try
                {
                    DebugLog.LogMessage("Loading Bundle");
                    bundle = AssetBundle.LoadFromFile(filePath);
                    
                }

                catch (Exception)
                {
                    DebugLog.LogWarning($"Error looking for: {filePath} ");
                }

                if (bundle != null)
                {
                    gameObjects = bundle.LoadAllAssets<GameObject>();
                    DebugLog.LogMessage("Loading GameObjects");
                    success = true;
                    //bundle.Unload(false);
                }
            }
            return success;
        }
    }
}
