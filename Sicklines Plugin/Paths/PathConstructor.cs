using System.Collections.Generic;
using UnityEngine;
using Reptile;
using System.IO;
using System;
using BepInEx.Logging;
using BepInEx;
using BepInEx.Configuration;
using Sicklines.Asset;
using SicklinesMono;

namespace Sicklines.AiPaths
{

    static public class PathConstructor
    {

        private static bool _debug = false;

        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Player Patches");

        private static readonly string ASSET_PATH = Path.Combine(Paths.PluginPath, PluginInfo.PLUGIN_NAME);

        static public CustomAiPath CurrentPath;
        static public CustomAiPath LastPath;
        static public PathLoader pathLoader;
        static public bool newPath = false;
        static public bool startedCombo = false;

        static private Material holoMaterial;
        static private GameObject triggerLine;
        static public GameObject waypointVisual;
        static public GameObject waypointRound;
        static public GameObject waypointPointer;
        static public GameObject waypointCube;
        static public GameObject waypointCubeSuper;
        static public GameObject waypointStar;
        static public GameObject waypointSlide;
        static public GameObject waypointBoost;
        static public GameObject waypointAirTrick;
        static public GameObject waypointAirTrickBoost;
        static public GameObject waypointAirdash;
        static public GameObject waypointJump;

        static public GameObject rootObject;

        static public PlayerAIWaypoint StartingWaypoint;
        static public PlayerAIWaypoint CurrentWaypoint;
        static public PlayerAIWaypoint PrevWaypoint;

        static public List<CustomAiPath> CustomPathsList;

        static public Player _target { get; private set; }

        static public int PathIDOffset = 300;

        //static private Dictionary<string, Stage> pathFiles;

        public static void Initialize(ConfigFile config)
        {
            CustomPathsList = new List<CustomAiPath>();
            pathLoader = new PathLoader();

            //Load Custom Paths
            String filepath = ASSET_PATH + @"\" + "sicklines.assets";

            //Bundle Loading
            //DebugLog.LogMessage($"Bundle load filepath: {filepath}");

            if (BundleLoader.LoadBundle(filepath, out GameObject[] Objects))
            {
                //DebugLog.LogMessage("Loaded Bundle");

                //Find Mono and Material
                foreach (GameObject obj in Objects)
                {
                    DebugLog.LogMessage(obj.name);

                    SickLines_Mono comp = obj.GetComponent<SickLines_Mono>();
                    if (comp != null)
                    {
                        holoMaterial = comp.HoloMat;
                        //DebugLog.LogMessage("Found Holo Material");
                        break;
                    }
                }

                //Find Trigger Obj
                foreach (GameObject obj in Objects)
                {
                    if (obj.name == "SickLineTrigger")
                    {
                        triggerLine = obj;
                        //DebugLog.LogMessage("Found Line Trigger");
                        break;
                    }
                }

                //Find Trigger Obj
                foreach (GameObject obj in Objects)
                {
                    if (obj.name == "Waypoints")
                    {
                        waypointVisual = obj;
                        //DebugLog.LogMessage("Found SickLine Waypoint");

                        foreach (Transform childTransform in obj.transform.GetAllChildren())
                        {

                            if (childTransform.gameObject.name == "SickLinesWaypoint_Round")
                            {
                                waypointRound = childTransform.gameObject;
                                //DebugLog.LogMessage("waypointRound");
                            }
                            else if (childTransform.gameObject.name == "SickLinesWaypoint_Pyramid")
                            {
                                waypointPointer = childTransform.gameObject;
                                //DebugLog.LogMessage("waypointPointer");
                            }
                            else if (childTransform.gameObject.name == "SickLinesWaypoint_Cube")
                            {
                                waypointCube = childTransform.gameObject;
                                //DebugLog.LogMessage("waypointCube");
                            }
                            else if (childTransform.gameObject.name == "SickLinesWaypoint_Cube_Super")
                            {
                                waypointCubeSuper = childTransform.gameObject;
                                //DebugLog.LogMessage("waypointCubeSuper");
                            }
                            else if (childTransform.gameObject.name == "SickLinesWaypoint_Star")
                            {
                                waypointStar = childTransform.gameObject;
                                //DebugLog.LogMessage("waypoint SickLinesWaypoint_Star");
                            }
                            else if (childTransform.gameObject.name == "SickLinesWaypoint_Slide")
                            {
                                waypointSlide = childTransform.gameObject;
                                //DebugLog.LogMessage("waypoint SickLinesWaypoint_Slide");
                            }
                            else if (childTransform.gameObject.name == "SickLinesWaypoint_Boost")
                            {
                                waypointBoost = childTransform.gameObject;
                                //DebugLog.LogMessage("waypoint SickLinesWaypoint_Boost");
                            }
                            else if (childTransform.gameObject.name == "SickLinesWaypoint_AirTrick")
                            {
                                waypointAirTrick = childTransform.gameObject;
                                //DebugLog.LogMessage("waypoint SickLinesWaypoint_AirTrick");
                            }
                            else if (childTransform.gameObject.name == "SickLinesWaypoint_AirTrick_Boost")
                            {
                                waypointAirTrickBoost = childTransform.gameObject;
                                //DebugLog.LogMessage("waypoint SickLinesWaypoint_AirTrickBoost");
                            }
                            else if (childTransform.gameObject.name == "SickLinesWaypoint_Airdash")
                            {
                                waypointAirdash = childTransform.gameObject;
                                //DebugLog.LogMessage("waypoint SickLinesWaypoint_Airdash");
                            }
                            else if (childTransform.gameObject.name == "SickLinesWaypoint_Jump")
                            {
                                waypointJump = childTransform.gameObject;
                                //DebugLog.LogMessage("waypoint SickLinesWaypoint_Jump");
                            }

                        }

                        break;

                    }
                }
            }
            else 
            {
                DebugLog.LogMessage($"Failed to find the Asset bundle please reinstall the mod");   
            }
        }

        static public bool requestNewPath(Player target)
        {
            //Do tests
            bool noActivePath = CurrentPath == null;

            if (noActivePath)
            {
                startNewPath(target);
                newPath = true;

                if (LastPath != null)
                {
                    if (LastPath.Loaded != true)
                    {
                        CustomPathsList.Remove(LastPath);
                        LastPath.OnDestroy();
                    }
                }
            }

            return noActivePath;
        }

        static public void startNewPath(Player target)
        {
            if (target == null) { return; }
            
            GameObject newRoot = new GameObject($"CustomPath_{CustomPathsList.Count}");

            CurrentPath = new CustomAiPath(newRoot, target);

            _target = target;

            StartingWaypoint = makeWaypointFromPlayerPos(target, true);

            if (StartingWaypoint == null) { DebugLog.LogError("Failed to make Waypoint");  return; }

            //CustomWaypoint
            CustomPlayerAiWaypoint point = StartingWaypoint as CustomPlayerAiWaypoint;
            if (point != null)
            {
                point.movestyle = _target.moveStyleEquipped;
            }

            if (target.ability != null)
            {
                StartingWaypoint.boost = target.ability.GetType() == typeof(BoostAbility);
                StartingWaypoint.slide = target.ability.GetType() == typeof(SlideAbility);
            }
            StartingWaypoint.unequipMovestyle = target.moveStyle == MoveStyle.ON_FOOT;
            StartingWaypoint.walk = target.walkButtonHeld;

            CurrentWaypoint = StartingWaypoint;

            CurrentPath.RuntimePath.firstWaypoint = StartingWaypoint;
            CurrentPath.RuntimePath.waypoints.Add(StartingWaypoint);
            CurrentPath.movestyleEquipped = target.moveStyleEquipped;
            CurrentPath.startUsingMovestyle = target.moveStyleEquipped != MoveStyle.ON_FOOT;

            CustomPathsList.Add(CurrentPath);

            //DebugLog.LogMessage($"Start new path| {target} | {CurrentPath.pathID}");

            //Store Player Boost stats
        }

        static public PlayerAIWaypoint addWaypointOnPlayer(bool boost, PlayerAIWaypoint.JumpPointBehavoir jumpPointBehavoir, bool autoTeleport, float grindTiltCorner, bool trick, bool slide, bool unequipMovestyle, bool walk, float wait , bool skipIfCantReach = false)
        {
            if (CurrentPath == null) { DebugLog.LogError( "Current path is null, why has a new path not been create?"); return null;  };
            if (_target == null) { DebugLog.LogError("Current target is null"); return null; };

            PlayerAIWaypoint waypoint = makeWaypointFromPlayerPos(_target);
            PrevWaypoint = CurrentWaypoint;
            CurrentWaypoint = waypoint;

            waypoint.boost = boost;
            waypoint.jumpPointBehavoir = jumpPointBehavoir;
            waypoint.autoTeleport = autoTeleport;
            waypoint.grindTiltCorner = grindTiltCorner;
            waypoint.trick = trick;
            waypoint.slide = slide;
            waypoint.unequipMovestyle = unequipMovestyle;
            waypoint.walk = walk;
            waypoint.wait = wait;

            CurrentPath.RuntimePath.waypoints.Add(waypoint);

            //DebugLog.LogMessage($" normal Waypoint");

            if (_debug)
            {
                LineRenderer line = waypoint.gameObject.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.SetPositions(new Vector3[] { waypoint.transform.position, waypoint.transform.position + new Vector3(0f, 1.0f, 0.0f) });
                line.startColor = Color.green;
                line.endColor = Color.green;
                line.SetWidth(0.1f, 0.1f);
            }

            return waypoint;
        }

        //Adds a Custom Child that has more functionality
        static public CustomPlayerAiWaypoint addCustomWaypointOnPlayer(bool boost, PlayerAIWaypoint.JumpPointBehavoir jumpPointBehavoir, bool autoTeleport, float grindTiltCorner, bool trick, bool slide, bool unequipMovestyle, bool walk, float wait, bool skipIfCantReach = false, bool isJump = false, bool noEncounter = false)
        {
            if (CurrentPath == null) { DebugLog.LogError("Current path is null, why has a new path not been create?"); return null; };
            if (_target == null) { DebugLog.LogError("Current target is null"); return null; };

            PlayerAIWaypoint waypoint = makeWaypointFromPlayerPos(_target, true);

            //Customwaypoint
            CustomPlayerAiWaypoint custWaypoint = (CustomPlayerAiWaypoint)waypoint;
            if (custWaypoint != null)
            {
                custWaypoint.movestyle = _target.moveStyleEquipped;
                custWaypoint.fromJump = isJump;
                custWaypoint.noEncounter = noEncounter;
            }

            PrevWaypoint = CurrentWaypoint;
            CurrentWaypoint = waypoint;
            waypoint.boost = boost;
            waypoint.jumpPointBehavoir = jumpPointBehavoir;
            waypoint.autoTeleport = autoTeleport;
            waypoint.grindTiltCorner = grindTiltCorner;
            waypoint.trick = trick;
            waypoint.slide = slide;
            waypoint.unequipMovestyle = unequipMovestyle;
            waypoint.walk = walk;
            waypoint.wait = wait;

            CurrentPath.RuntimePath.waypoints.Add(waypoint);

            //DebugLog.LogMessage($"Add waypoint | {waypoint.position} | {boost} | {slide}");
            //DebugLog.LogMessage($"Cust Waypoint");

            if (_debug)
            {
                LineRenderer line = waypoint.gameObject.AddComponent<LineRenderer>();
                line.SetPositions(new Vector3[] { waypoint.transform.position, waypoint.transform.position + new Vector3(1.0f, 1.0f, 0f) });
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = Color.blue;
                line.endColor = Color.blue;
                line.SetWidth(0.1f, 0.1f);
            }

            return custWaypoint;
        }

        static public PlayerAIWaypoint addJumpWaypoint( PlayerAIWaypoint.JumpPointBehavoir jumpPointBehavoir)
        {
            if (CurrentPath == null) { DebugLog.LogError("Current path is null, why has a new path not been create?"); return null; };
            if (CurrentWaypoint == null) { DebugLog.LogError("Current Waypoint is null"); return null; };
            if (_target == null) { DebugLog.LogError("Current target is null"); return null; };

            DebugLog.LogMessage($"Jump Waypoint");

            PlayerAIWaypoint waypoint = makeWaypointFromPlayerPos(_target);

            waypoint.transform.SetParent(CurrentWaypoint.transform, true);
            
            CurrentWaypoint.jumpPointBehavoir = jumpPointBehavoir;

            if (_debug)
            {
                LineRenderer line = waypoint.gameObject.AddComponent<LineRenderer>();
                line.SetPositions(new Vector3[] { waypoint.transform.position, waypoint.transform.position + new Vector3(-1.0f, 1.0f, 0f), waypoint.transform.position + new Vector3(1.0f, 1.0f, 0f) });
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = Color.red;
                line.endColor = Color.red;
                line.SetWidth(0.1f, 0.1f);
            }

            return waypoint;
        }

        static public PlayerAIWaypoint makeWaypointFromPlayerPos(Player player, bool custom = false)
        {
            
            GameObject waypointObj = new GameObject($"CustomPath_{CustomPathsList.Count}_waypoint_{CurrentPath.RuntimePath.waypoints.Count}");
            PlayerAIWaypoint waypoint;
            waypointObj.transform.SetParent(CurrentPath.rootObject.transform, true);

            if (custom)
            {
                waypoint = waypointObj.AddComponent<CustomPlayerAiWaypoint>();
            }
            else 
            { 
                waypoint = waypointObj.AddComponent<PlayerAIWaypoint>();
            }

            waypoint.transform.position = player.transform.position;
            waypoint.transform.rotation = player.transform.rotation;

            return waypoint;
        }

        static public void testDistanceNewWaypoint()
        {
            if (CurrentWaypoint == null) { DebugLog.LogError("Current Waypoint is null"); return; };
            if (_target == null) { DebugLog.LogError("Current target is null"); return; };

            Vector3 distVec = _target.transform.position - CurrentWaypoint.transform.position;
            distVec.z = 0.0f;

            if (distVec.magnitude > 5.0)
            {
                //DebugLog.LogMessage($"Making new waypoint due to distance");
                PlayerAIWaypoint waypoint = makeWaypointFromPlayerPos(_target);
                PrevWaypoint = CurrentWaypoint;
                CurrentWaypoint = waypoint;
                CurrentPath.RuntimePath.waypoints.Add(waypoint);
            }
        }

        static public void endPath(bool returnToStart = false, bool loop = false)
        {
            if (CurrentPath == null) { return; }

            //DebugLog.LogMessage($"End path");
            
            //Make Ghost dance before disapearing.
            PlayerAIWaypoint waypointDance = makeWaypointFromPlayerPos(_target, true);
            waypointDance.wait = 10.0f;
            CustomPlayerAiWaypoint custWaypoint = waypointDance as CustomPlayerAiWaypoint;
            custWaypoint.dance = 0;
            custWaypoint.unequipMovestyle = true;
            custWaypoint.waypointType = WaypointType.dance;
            CurrentPath.RuntimePath.waypoints.Add(waypointDance);

            //Kill Ai at end
            PlayerAIWaypoint waypointEnd = makeWaypointFromPlayerPos(_target, true);
            waypointEnd.unequipMovestyle = true;
            CustomPlayerAiWaypoint custEndWaypoint = waypointEnd as CustomPlayerAiWaypoint;
            custEndWaypoint.end = true;
            waypointEnd.transform.position += waypointEnd.transform.forward * 2.0f;

            CurrentPath.RuntimePath.waypoints.Add(waypointEnd);
            CurrentPath.RuntimePath.lastWaypoint = waypointEnd;

            // Should it loop?
            if (loop)
            {
                CurrentPath.RuntimePath.nextPathID = CurrentPath.RuntimePath.pathID;
            }

            //Setup Trigger
            //DebugLog.LogMessage($"Setup Trigger");
            setupLineTrigger(CurrentPath, SickLines_Trigger.Linestate.local);

            //Save the path or something

            //Go back to start
            if (returnToStart)
            {
                WorldHandler.instance.PlaceCurrentPlayerAt(CurrentPath.RuntimePath.waypoints[0].gameObject.transform, true);
            }

            // Clear Current Path
            LastPath = CurrentPath;
            CurrentPath = null;
            _target = null;
            StartingWaypoint = null;
            CurrentWaypoint = null;
            PrevWaypoint = null;

        }

        static public bool isPlayerMakingPath(Player player)
        {
            bool flag = CurrentPath != null && player == _target;

            return flag;
        }

        static public void setAiPlayerMaterial(Player player, bool testPath = false)
        {
            if (holoMaterial == null) { DebugLog.LogError("Failed to find Holo Material check that sickLines.assets is located in the plugin folder");   return; }
            
            CharacterVisual charVis = player.characterVisual;
            Material[] mats = charVis.mainRenderer.materials;
            
            foreach (Material mat in mats)
            {
                mat.shader = holoMaterial.shader;
                if (testPath)
                {
                    mat.SetColor("_Color0", new Color(0.9f, 0.5f, 0f));
                    mat.SetColor("_Color2", new Color(0.6f, 0.08f, 0f));
                    mat.SetColor("_OutlineColor_", new Color(1.0f, 0.5f, 0f));
                }
            }

            List<GameObject> objs = new List<GameObject>();

            if (charVis.moveStyleProps.bmxFrame != null)
            { 
                objs.Add(charVis.moveStyleProps.bmxFrame); 
            }
            if (charVis.moveStyleProps.bmxHandlebars != null)
            {
                objs.Add(charVis.moveStyleProps.bmxHandlebars);
            }
            if (charVis.moveStyleProps.bmxWheelF != null)
            { 
                objs.Add(charVis.moveStyleProps.bmxWheelF); 
            }
            if (charVis.moveStyleProps.bmxWheelR != null)
            {
                objs.Add(charVis.moveStyleProps.bmxWheelR);
            }
            if (charVis.moveStyleProps.bmxGear != null)
            {
                objs.Add(charVis.moveStyleProps.bmxGear);
            }
            if (charVis.moveStyleProps.bmxPedalL != null)
            {
                objs.Add(charVis.moveStyleProps.bmxPedalL);
            }
            if (charVis.moveStyleProps.bmxPedalR != null)
            {
                objs.Add(charVis.moveStyleProps.bmxPedalR);
            }
            if (charVis.moveStyleProps.skateboard != null)
            {
                objs.Add(charVis.moveStyleProps.skateboard);
            }
            if (charVis.moveStyleProps.skateR != null)
            {
                objs.Add(charVis.moveStyleProps.skateR);
            }
            if (charVis.moveStyleProps.skateL != null)
            {
                objs.Add(charVis.moveStyleProps.skateL);
            }
            if (charVis.moveStyleProps.specialSkateBoard != null)
            {
                objs.Add(charVis.moveStyleProps.specialSkateBoard);
            }

            foreach (GameObject obj in objs)
            {
                MeshRenderer render;
                if (obj.TryGetComponent<MeshRenderer>(out render))
                {
                    foreach (Material mat in render.materials)
                    {
                        mat.shader = holoMaterial.shader;
                        if (testPath)
                        {
                            mat.SetColor("_Color0", new Color(0.9f, 0.5f, 0f));
                            mat.SetColor("_Color2", new Color(0.6f, 0.08f, 0f));
                            mat.SetColor("_OutlineColor_", new Color(1.0f, 0.5f, 0f));
                        }
                    }
                }

            }
        }

        static public void setupLineTrigger(CustomAiPath customPath, SickLines_Trigger.Linestate state)
        {
            if (triggerLine == null) { DebugLog.LogError("triggerLine not found"); return; };

            //DebugLog.LogMessage("Instantiate");
            GameObject trigger = GameObject.Instantiate(triggerLine, customPath.rootObject.transform);

            trigger.transform.position = customPath.RuntimePath.firstWaypoint.position;

            //DebugLog.LogMessage("AddComponent");
            SickLines_Trigger triggerComp = trigger.AddComponent(typeof(SickLines_Trigger)) as SickLines_Trigger;
            triggerComp.SickLinesTrigger_init(customPath);

            setLineTriggerColour(trigger, state);

            customPath.triggerLine = trigger;

            Pickup pickupComp = trigger.AddComponent(typeof(Pickup)) as Pickup;
            pickupComp.wantedManager = WantedManager.instance;
            pickupComp.pickupType = Pickup.PickUpType.BRIBE;
        }

        static public void setLineTriggerColour( GameObject trigger , SickLines_Trigger.Linestate state)
        {
            
            MeshRenderer[] renders = trigger.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer render in renders)
            {
                if (state == SickLines_Trigger.Linestate.local)
                {
                    render.material.SetColor("_Color0", new Color(0.9f, 0.5f, 0f));
                    render.material.SetColor("_Color2", new Color(0.6f, 0.08f, 0f));
                    render.material.SetColor("_OutlineColor_", new Color(1.0f, 0.5f, 0f));
                }
                else if (state == SickLines_Trigger.Linestate.unCompleted)
                {
                    render.material.SetColor("_Color0", new Color(0.0f, 1.0f, 0.458f));
                    render.material.SetColor("_Color2", new Color(0.0f, 0.435f, 0.141f));
                    render.material.SetColor("_OutlineColor_", new Color(0.0f, 1.0f, 0.458f));
                }
                else if (state == SickLines_Trigger.Linestate.Complete)
                {
                    render.material.SetColor("_Color0", new Color(0.5f, 0.5f, 0.5f) );
                    render.material.SetColor("_Color2", new Color(0.15f, 0.15f, 0.15f));
                    render.material.SetColor("_OutlineColor_", new Color(0.5f, 0.5f, 0.5f));
                }
            }
           
        }

        static public bool isCustomPath(PlayerAIPath path)
        {
            if (path.gameObject.transform.GetComponentInChildren<SickLines_Trigger>())
            {
                //DebugLog.LogMessage("Is Custom Path");
                return true;
            }

            //DebugLog.LogMessage("Is not a Custom Path");
            return false;
        }
    }
}
