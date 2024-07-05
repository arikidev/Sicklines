using System.Collections.Generic;
using UnityEngine;
using Reptile;
using System.IO;
using System;
using BepInEx.Logging;
using BepInEx;
using BepInEx.Configuration;
using CommonAPI;

namespace Sicklines
{

    static public class SickLines_SaveManager
    {
        
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} SickLines_SaveManager");

        private static readonly string ASSET_PATH = Path.Combine(Paths.PluginPath, PluginInfo.PLUGIN_NAME);

        public static SickLines_Save save;

        public static void Initialize()     //(ConfigFile config)
        {
            save = new SickLines_Save();
        }
    }

    public class SickLines_Save : CustomSaveData
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} MoveStylerEmailManager");

        public static SickLines_Save Instance { get; private set; }

        private readonly Dictionary<string, bool> pathsCompleted;

        public SickLines_Save() : base(PluginInfo.PLUGIN_NAME, "{0}_paths.save", SaveLocations.Documents)
        {
            Instance = this;
            pathsCompleted = new Dictionary<string, bool>();
        }

        public bool hasReadData = false;

        public void setPathState(string fileName, bool state)
        {
            if (pathsCompleted.ContainsKey(fileName))
            {
                pathsCompleted[fileName] = state;
                return;
            }
            pathsCompleted.Add(fileName, state);
        }

        public bool getPathState(string fileName)
        {
            if (pathsCompleted.ContainsKey(fileName))
            {
                return pathsCompleted[fileName];
            }
            return false;
        }

        // Starting a new save - start from zero.
        public override void Initialize()
        {
            pathsCompleted.Clear();
        }

        public override void Read(BinaryReader reader)
        {
            var version = reader.ReadByte();
            var numPaths = reader.ReadInt32();

            for (var i = 0; i < numPaths; i++)
            {
                var pathFile = reader.ReadString();
                var pathCompleted = reader.ReadBoolean();

                if (!pathsCompleted.ContainsKey(pathFile))
                {
                    pathsCompleted.Add(pathFile, pathCompleted);
                }
            }
            hasReadData = true;
        }

        public override void Write(BinaryWriter writer)
        {
            // Version
            writer.Write((byte)0);
            writer.Write(pathsCompleted.Count);
            foreach (var pathState in pathsCompleted)
            {
                writer.Write(pathState.Key);
                writer.Write(pathState.Value);
            }
        }

        public bool hasCompletedPath(string filePath)
        {
            if (pathsCompleted.ContainsKey(filePath))
            {
                return pathsCompleted[filePath];
            }
            return false;
        }

        public bool hasSave(string filePath)
        {
            if (pathsCompleted.ContainsKey(filePath))
            {
                return true;
            }
            return false;
        }
    }
}
