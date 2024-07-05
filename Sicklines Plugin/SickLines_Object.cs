using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Reptile;
using Sicklines.AiPaths;
using CommonAPI;
using BepInEx.Logging;

namespace Sicklines
{
    public class SickLines_Trigger : CustomInteractable
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Player Patches");

        int PathID;

        private Player aiPlayer;

        CustomAiPath Path;

        Sicklines_Encounter _Encounter;

        public void SickLinesTrigger_init(CustomAiPath _path)
        {
            Path = _path;
            SetupEncounter();
        }

        private void Awake()
        {

        }

        public override void Interact(Player player)
        { 
            StartEncounter(2.0f);
        }

        public void TriggerLine()
        {
            if (Path == null) { return; }

            Transform spawnPosition = this.transform;

            if (aiPlayer != null) { WorldHandler.instance.RemovePlayer(aiPlayer); }

            aiPlayer = WorldHandler.instance.SetupAIPlayerAt(spawnPosition, Path.character, PlayerType.NONE, Path.outfit, Path.movestyleEquipped);
            aiPlayer.AI.SetPath(Path.RuntimePath, true, true);
            aiPlayer.AI.state = PlayerAI.PlayerAIState.NORMAL_MOVE;
            PathConstructor.setAiPlayerMaterial(aiPlayer, !Path.Loaded);

            aiPlayer.AI.alignToNPCWaitTimer = -66;
        }

        private void SetupEncounter()
        {
            DebugLog.LogMessage("SetupEncounter");

            GameObject comboEncounterObj = new GameObject($"ComboEncounter_{this.PathID}");
            GameObject trackObj = new GameObject($"trackObj");
            trackObj.transform.SetParent(comboEncounterObj.transform, false );

            List<Collider> triggers = new List<Collider>();

            _Encounter = comboEncounterObj.AddComponent<Sicklines_Encounter>();
            DebugLog.LogMessage("AddComponent<Sicklines_Encounter>");

            _Encounter.Path = Path;
            _Encounter.playerSpawner = this.Path.RuntimePath.firstWaypoint.transform;
            _Encounter.trackSetup = trackObj;
            _Encounter.playerInStartTriggerTimer = 15f;
            _Encounter.OnIntro = new UnityEngine.Events.UnityEvent();
            _Encounter.OnStart = new UnityEngine.Events.UnityEvent();
            _Encounter.OnOutro = new UnityEngine.Events.UnityEvent();
            _Encounter.OnFailed = new UnityEngine.Events.UnityEvent();
            _Encounter.OnCompleted = new UnityEngine.Events.UnityEvent();
            _Encounter.makeUnavailableDuringEncounter = new GameObject[0];
            _Encounter.OnRegisterToSceneObjectsRegister(WorldHandler.instance.sceneObjectsRegister);

            GameObject spawner  = new GameObject("spawner");
            spawner.transform.SetParent(trackObj.transform, false);
            spawner.transform.position = this.Path.RuntimePath.firstWaypoint.transform.position;
            spawner.transform.rotation = this.Path.RuntimePath.firstWaypoint.transform.rotation;

            foreach (PlayerAIWaypoint waypoint in this.Path.RuntimePath.waypoints)
            {
                string name = "checkpointTrigger";
                
                if (waypoint == this.Path.RuntimePath.lastWaypoint) { continue; }
                else if (waypoint == this.Path.RuntimePath.waypoints[0]) 
                {
                    name = "startTrigger";
                }

                CustomPlayerAiWaypoint cust = waypoint as CustomPlayerAiWaypoint;
                bool isCustWaypoint = cust != null;
                //DebugLog.LogMessage($"Custom : {isCustWaypoint}");

                if (isCustWaypoint) 
                {
                    if (cust.noEncounter || cust.waypointType == WaypointType.hidden || cust.waypointType == WaypointType.airdash) { continue; }
                }
                GameObject comboCheckPointRoot = new GameObject(name);
                comboCheckPointRoot.transform.SetParent(trackObj.transform, false);
                SphereCollider col = comboCheckPointRoot.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.5f;
                comboCheckPointRoot.layer = 19;
                comboCheckPointRoot.tag = "Encounter";
                comboCheckPointRoot.transform.position = waypoint.transform.position;

                SickLines_WaypointType type = comboCheckPointRoot.AddComponent<SickLines_WaypointType>();
                type.WaypointType = cust.waypointType;

                if (waypoint == this.Path.RuntimePath.waypoints[0]) { continue; }
                
                //Setup Waypoint visuals
                /**
                    waypointRound;
                    waypointPointer;
                    waypointCube;
                    waypointCubeSuper; 
                **/

                GameObject point = null;
                Quaternion rotationOffset = Quaternion.identity;
                Vector3 posOffset = Vector3.zero;
                switch (cust.waypointType)
                {
                    default:

                        break;

                    case WaypointType.basic:
                        
                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointRound, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.jump:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointJump, comboCheckPointRoot.transform, false);
                        posOffset = new Vector3(0, -0.35f, 0f);
                        break;

                    case WaypointType.landing:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointPointer, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.trick:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointCube, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.trickBoosted:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointCubeSuper, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.slide:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointSlide, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.slideEnd:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointPointer, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.wallrun:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointPointer, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.wallrunLeft:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointPointer, comboCheckPointRoot.transform, false);
                        rotationOffset = Quaternion.Euler( 0.0f, 0.0f, -90.0f);
                        break;

                    case WaypointType.wallrunRight:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointPointer, comboCheckPointRoot.transform, false);
                        rotationOffset = Quaternion.Euler(0.0f, 0.0f, 90.0f);
                        break;

                    case WaypointType.airtrick:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointAirTrick, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.airtrickBoosted:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointAirTrickBoost, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.airdash:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointAirdash, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.grind:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointPointer, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.grindLeft:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointPointer, comboCheckPointRoot.transform, false);
                        rotationOffset = Quaternion.Euler(0.0f, 0.0f, 45.0f);
                        break;

                    case WaypointType.grindRight:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointPointer, comboCheckPointRoot.transform, false);
                        rotationOffset = Quaternion.Euler(0.0f, 0.0f, -45.0f);
                        break;

                    case WaypointType.grindTrick:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointCube, comboCheckPointRoot.transform, false);
                        rotationOffset = Quaternion.Euler(0.0f, 0.0f, -45.0f);
                        break;

                    case WaypointType.grindTrickBoosted:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointCubeSuper, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.boost:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointBoost, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.boostEnd:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointBoost, comboCheckPointRoot.transform, false);
                        
                        break;

                    case WaypointType.movestyleSwitch:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointPointer, comboCheckPointRoot.transform, false);
                        break;

                    case WaypointType.dance:

                        point = UnityEngine.Object.Instantiate(PathConstructor.waypointStar, comboCheckPointRoot.transform, false);
                        posOffset = new Vector3(0.0f,0.5f,0.0f);
                        break;

                    case WaypointType.hidden:

                        break;
                    
                }
                
                if (point == null)
                {
                    point = UnityEngine.Object.Instantiate(PathConstructor.waypointRound, waypoint.transform, false);
                }

                rotationOffset *= Quaternion.Euler(0.0f, 0.0f, 90.0f);

                //DebugLog.LogMessage($"Set Point Vis Transform");
                point.transform.position =  waypoint.transform.position + new Vector3(0,0.35f,0f) + posOffset;
                point.transform.rotation =  waypoint.transform.rotation * rotationOffset;
                point.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);              
            }
            //DebugLog.LogMessage($"_Encounter.InitSceneObject()");
            _Encounter.checkpointTriggers = triggers;
            _Encounter.InitSceneObject();
        }

        public void StartEncounter( float delay)
        {
            if (_Encounter == null) { return; }
            
            StartCoroutine(DelayStartEncounter(delay));
        }

        private IEnumerator DelayStartEncounter(float delay)
        {
            _Encounter.StartIntro();

            yield return new WaitForSeconds(0.5f);
            TriggerLine();

            yield return new WaitForSeconds(0.25f);

            _Encounter.ActivateEncounterInstantIntro();
        }

        public void OnFailed()
        {
            if (aiPlayer != null) { WorldHandler.instance.RemovePlayer(aiPlayer); }
        }

        public enum Linestate
        { 
            local,
            unCompleted,
            Complete,
        }
    }

    public class SickLines_WaypointType : MonoBehaviour
    {
        public WaypointType WaypointType;
    }
}
