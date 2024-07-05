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

namespace Sicklines.AiPaths
{
    [Serializable]
    public class CustomAiPath
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Custom Path");
        private static readonly string CONFIG_PATH = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME);

        public PlayerAIPath RuntimePath { get; }
        public GameObject rootObject;
        public GameObject triggerLine;
        public bool Loaded { get; private set; }

        //Serial Data
        public int pathID;
        public Characters character;
        public MoveStyle movestyleEquipped;
        public bool startUsingMovestyle;
        public int outfit;
        public Stage stage;
        
        public CustWaypointData[] pointData;
        public CustPathConfig pathConfig;

        public CustomAiPath(GameObject Root, Player _target = null)
        {
            RuntimePath = Root.AddComponent<PlayerAIPath>();
            GenerateNewPathID();
            rootObject = Root;
            RuntimePath.pathID = pathID;
            DebugLog.LogMessage($"RuntimePath | {RuntimePath}");
            outfit = 0;
            Loaded = false;
            startUsingMovestyle = false;
            movestyleEquipped = MoveStyle.ON_FOOT;

            if (_target != null)
            {
                character = _target.character;
                movestyleEquipped = _target.moveStyleEquipped;
                outfit = Core.Instance.SaveManager.CurrentSaveSlot.GetCharacterProgress(character).outfit;
            }

            stage = Reptile.Utility.GetCurrentStage();

        }

        public bool loadAiPathFromJsonFile(string filePath)
        {
            bool success = parseJsonPath(filePath);
            Loaded = success;
            return success;
        }

        public void pathToJson()
        {
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
            }

            generateWaypointData();

            try
            {
                string path = Path.Combine(CONFIG_PATH, generatePathFileName() + ".path");
                string file = JsonUtility.ToJson(this);
                
                foreach (CustWaypointData point in pointData)
                {
                    pathConfig.pointArray.Add(JsonUtility.ToJson(point));
                }

                file += "\n*\n";
                file += JsonUtility.ToJson(pathConfig);

                File.WriteAllText(path, file);
                DebugLog.LogMessage($"Saved Path : {path}");

                PathConstructor.setLineTriggerColour(triggerLine, SickLines_Trigger.Linestate.unCompleted);

                Loaded = true; //Convert to loaded path
            }
            catch
            {
                DebugLog.LogError($"Failed to Save path");
            }
            
        }

        public bool parseJsonPath(string filePath)
        {
            DebugLog.LogMessage("parseJsonPath");

            if (!File.Exists(filePath)) { DebugLog.LogError($"failed to find file: {filePath}");  return false; }

            string text = File.ReadAllText(filePath);

            string[] parts = text.Split('*');

            if (parts.Length != 2) { DebugLog.LogError($"failed to split text for: {filePath}"); return false; }

            CustomAiPath obj = JsonUtility.FromJson<CustomAiPath>(parts[0]);

            this.character = obj.character;
            this.movestyleEquipped = obj.movestyleEquipped;
            this.outfit = obj.outfit;
            this.stage = obj.stage;
            this.pathID = obj.pathID;

            DebugLog.LogMessage("Part 2");
            DebugLog.LogMessage(parts[1]);
            this.pathConfig = JsonUtility.FromJson<CustPathConfig>(parts[1]);

            this.RuntimePath.waypoints = new List<PlayerAIWaypoint>();

            foreach (string data in pathConfig.pointArray)
            {
                CustWaypointData custWaypoint = JsonUtility.FromJson<CustWaypointData>(data);
                
                this.RuntimePath.waypoints.Add(custWaypoint.dataToObject(this));
                DebugLog.LogMessage("Added waypoint");

            }
            if (this.RuntimePath.waypoints.Count < 2) { DebugLog.LogError($"Not enough Waypoints constructed"); return false; }

            this.RuntimePath.firstWaypoint = this.RuntimePath.waypoints[0];
            this.RuntimePath.lastWaypoint = this.RuntimePath.waypoints[this.RuntimePath.waypoints.Count - 1];

            pointData = null;
            pathConfig = null;

            return true;
        }

        public string generatePathFileName()
        {
            string time = SystemClock.now.Hour.ToString() + SystemClock.now.Minute.ToString() + SystemClock.now.Second.ToString();

            string name = "";
            name += this.stage + "_" + this.character + "_" + this.movestyleEquipped.ToString() + "_" + time + "." + this.stage;

            return name;
        }

        public bool generateWaypointData()
        {
            List<CustWaypointData> data = new List<CustWaypointData>();
            
            foreach (PlayerAIWaypoint point in this.RuntimePath.waypoints)
            {
                CustWaypointData var = new CustWaypointData();

                //Set CustomVariables if applicable
                if (point is CustomPlayerAiWaypoint)
                {
                    CustomPlayerAiWaypoint custPoint = point as CustomPlayerAiWaypoint;

                    var.custom = true;
                    var.TrickId = custPoint.TrickId;
                    var.movestyle = (int)custPoint.movestyle;
                    var.fromJump = custPoint.fromJump;
                    var.end = custPoint.end;
                    var.dance = custPoint.dance;
                }

                var.pos = point.transform.position;
                var.rot = point.transform.rotation;
                var.boost = point.boost;
                var.jumpPointBehavoir = (int)point.jumpPointBehavoir;
                var.autoTeleport = point.autoTeleport;
                var.grindTiltCorner = point.grindTiltCorner;
                var.trick = point.trick;
                var.slide = point.slide;
                var.unequipMovestyle = point.unequipMovestyle;
                var.walk = point.walk;
                var.wait = point.wait;

                
                if (point.IsJumper())
                {
                    var.jump = true;
                    var.jumpPos = point.jumpPosition;
                    var.jumpPointScale = point.jumpPointScale;
                }

                data.Add(var);
            }

            pointData = data.ToArray();

            pathConfig = new CustPathConfig();
            pathConfig.pointArray = new List<string>();

            return data.Count > 0 ;
        }

        public int GenerateNewPathID()
        {
            return PathConstructor.PathIDOffset + PathConstructor.CustomPathsList.Count;
        }

        public void OnDestroy()
        {
            //Clean up GameObjects
            UnityEngine.Object.Destroy(this.RuntimePath.gameObject);
            UnityEngine.Object.Destroy(this.triggerLine);
            UnityEngine.Object.Destroy(rootObject);
        }
    }

    [Serializable]
    public class CustWaypointData
    {
        public Vector3 pos = new Vector3(0.0f,0.0f,0.0f);
        public Quaternion rot = new Quaternion();

        public bool boost = false;
        public int jumpPointBehavoir = 0 ;
        public bool autoTeleport = false;
        public float grindTiltCorner = 0.0f;
        public bool trick = false;
        public bool slide = false;
        public bool unequipMovestyle = false;
        public bool walk = false;
        public float wait = 0.0f;

        //Jump
        public bool jump = false;
        public Vector3 jumpPos = new Vector3(0.0f, 0.0f, 0.0f);
        public float jumpPointScale = 0.5f;

        //Custom properties
        public bool custom = false;
        public int TrickId = 0;
        public int movestyle = 0;
        public bool fromJump = false;
        public int dance = -1;
        public bool end = false;

        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} CustWaypointData");

        public PlayerAIWaypoint dataToObject(CustomAiPath Parent)
        {
            PlayerAIWaypoint component;
            GameObject rootObj = new GameObject();

            if (this.custom)
            {
                component = rootObj.AddComponent<CustomPlayerAiWaypoint>();
                CustomPlayerAiWaypoint cust = component as CustomPlayerAiWaypoint;

                cust.TrickId = this.TrickId;
                cust.movestyle = (MoveStyle)this.movestyle;
                cust.fromJump = this.fromJump;
                cust.dance = this.dance;
                cust.end = this.end;
            }
            else 
            {
                component = rootObj.AddComponent<PlayerAIWaypoint>();
            }

            component.boost = this.boost;
            component.jumpPointBehavoir = (PlayerAIWaypoint.JumpPointBehavoir)this.jumpPointBehavoir;
            component.autoTeleport = this.autoTeleport;
            component.grindTiltCorner = this.grindTiltCorner;
            component.trick = this.trick;
            component.slide = this.slide;
            component.unequipMovestyle = this.unequipMovestyle;
            component.walk = this.walk;
            component.wait = this.wait;
            component.gameObject.transform.SetPositionAndRotation(this.pos, this.rot);

            /**
            LineRenderer line1 = rootObj.AddComponent<LineRenderer>();
            line1.SetWidth(0.2f, 0.2f);
            line1.positionCount = 2;
            line1.SetPosition(0, rootObj.transform.position);
            line1.SetPosition(1, rootObj.transform.position + new Vector3(0, 1, 0));
            **/

            //Setup Jump Position
            if (this.jump)
            {
                GameObject jumpObj = new GameObject();
                jumpObj.transform.SetParent(rootObj.transform, true);
                jumpObj.transform.position = this.jumpPos;
                jumpObj.transform.localScale = new Vector3(this.jumpPointScale, 1, 1);

                /**
                LineRenderer line = jumpObj.AddComponent<LineRenderer>();
                line.SetWidth(0.2f, 0.2f);
                line.positionCount = 2;
                line.SetPosition(0, jumpObj.transform.position);
                line.SetPosition(1, jumpObj.transform.position + new Vector3(0, this.jumpPointScale, 0));
                line.SetColors(Color.blue, Color.blue);
                **/
            }

            return component;
        }

    }

    [Serializable]
    public class CustPathConfig
    {
        public List<String> pointArray;
    }

}
