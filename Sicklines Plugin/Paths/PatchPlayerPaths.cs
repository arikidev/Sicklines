using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reptile;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;

namespace Sicklines.AiPaths
{
    [HarmonyPatch(typeof(Player))]
    class PlayerPatchPaths
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Player Patches");

        [HarmonyPostfix]
        [HarmonyPatch("Jump")]
        public static void JumpPatch(Player __instance)
        {
            if (!PathConstructor.isPlayerMakingPath(__instance)) { return; }

            //DebugLog.LogMessage($"Jump ");

            bool slide = false; //__instance.slideButtonHeld;
            bool boosting = __instance.boostButtonHeld;
            bool movestyleFlag = __instance.moveStyle == MoveStyle.ON_FOOT;

            CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(boosting, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, slide, movestyleFlag, false, 0.0f, false, true);
            //obj.transform.position += obj.transform.forward * 0.35f;
            obj.transform.localScale = new Vector3(0.35f, 1.0f, 1.0f);
            
            obj.fromJump = true;
            obj.waypointType = WaypointType.jump;

            CustomPlayerAiWaypoint oldWaypoint = (CustomPlayerAiWaypoint)PathConstructor.PrevWaypoint;
            WaypointType prevWaypointType = oldWaypoint.waypointType;
            if (Vector3.Distance(oldWaypoint.transform.position, obj.transform.position) < 0.25f) //prevWaypointType == WaypointType.landing && 
            {
                oldWaypoint.waypointType = WaypointType.hidden;
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch("OnLanded")]
        public static void OnLandedPatch(Player __instance)
        {
            if (!PathConstructor.isPlayerMakingPath(__instance)) { return; }

            DebugLog.LogMessage($"on Land");

            //If player hasn't airdashed or air tricked add jump node at land point.
            if (PathConstructor.CurrentWaypoint == null) { return; }

            // Test is you have boosted and create a landway point to adjust inputs early
            bool boosting = __instance.boostButtonHeld;
            bool movestyleFlag = __instance.moveStyle == MoveStyle.ON_FOOT;
            bool slide = __instance.slideButtonHeld;

            bool makeJumpPoint = false;
            CustomPlayerAiWaypoint cust = PathConstructor.CurrentWaypoint as CustomPlayerAiWaypoint;

            if (cust != null)
            {
                makeJumpPoint = cust.fromJump && !cust.IsJumper();
            }

            if (makeJumpPoint)
            {
                //DebugLog.LogMessage($" from jump. add Jump point ");
                PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT);
            }

            CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(boosting, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, slide, movestyleFlag, false, 0.0f);
            PathConstructor.CurrentWaypoint.transform.localScale = new UnityEngine.Vector3(1.0f, 1.0f, 1.0f);
            obj.waypointType = WaypointType.landing;

            CustomPlayerAiWaypoint prev = (CustomPlayerAiWaypoint)PathConstructor.PrevWaypoint;
            if (prev.waypointType == WaypointType.slide && Vector3.Distance(prev.transform.position, obj.transform.position) < 0.5f)
            {
                obj.waypointType = WaypointType.hidden;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("ActivateAbility")]
        public static void ActivateAbilityPatch(Player __instance, Ability a)
        {
            if (!PathConstructor.isPlayerMakingPath(__instance)) { return; }

            bool inAir = false; 
            bool fromJump = false;
            bool hasJumpPoint = PathConstructor.CurrentWaypoint.IsJumper();
            WaypointType prevWaypointType = WaypointType.basic;

            CustomPlayerAiWaypoint Cust = PathConstructor.CurrentWaypoint as CustomPlayerAiWaypoint;
            if (Cust != null) { 
                inAir = Cust.inAir; 
                fromJump = Cust.fromJump;
                prevWaypointType = Cust.waypointType;
            }

            bool movestyleFlag = __instance.moveStyle == MoveStyle.ON_FOOT;
            bool boostFlag = __instance.boostButtonHeld;
            bool slideFlag = __instance.slideButtonHeld;

            //Switch on type
            if (a.GetType() == typeof(SlideAbility))
            {
                DebugLog.LogMessage($" start slide ");

                CustomPlayerAiWaypoint obj = null;

                if (PathConstructor.CurrentWaypoint.IsJumper()) //If has a jumpPoint make waypoint
                {
                    obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, true, movestyleFlag, false, 0.0f);
                }
                else if (fromJump && !PathConstructor.CurrentWaypoint.IsJumper()) // If has jumped an does not have a jump pooint make jump point.
                {
                    PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT);
                    obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, true, movestyleFlag, false, 0.0f);

                    if (prevWaypointType == WaypointType.landing && Vector3.Distance(Cust.transform.position, obj.transform.position) < 0.5f)
                    {
                        DebugLog.LogMessage($" Hide Previous Waypoint");
                        Cust.waypointType = WaypointType.hidden;
                    }
                    else if (prevWaypointType == WaypointType.trick && Vector3.Distance(Cust.transform.position, obj.transform.position) < 5.0f)
                    {
                        obj.waypointType = WaypointType.end;
                    }
                }
                else
                {
                    obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, true, movestyleFlag, false, 0.0f);

                    if (prevWaypointType == WaypointType.landing && Vector3.Distance(Cust.transform.position, obj.transform.position) < 0.5f)
                    {
                        DebugLog.LogMessage($" Hide Previous Waypoint");
                        Cust.waypointType = WaypointType.hidden;
                    }
                    else if (prevWaypointType == WaypointType.trick && Vector3.Distance(Cust.transform.position, obj.transform.position) < 5.0f)
                    {
                        obj.waypointType = WaypointType.end;
                    }
                }

                if (obj != null)
                {
                    obj.transform.localScale = new UnityEngine.Vector3(2.0f, 1.0f, 1.0f);
                    obj.waypointType = WaypointType.slide;
                }

            }
            else if (a.GetType() == typeof(BoostAbility))
            {
                //DebugLog.LogMessage($" BoostAbility add point ");

                if (PathConstructor.CurrentWaypoint.IsJumper())
                {

                    //Stupid verison
                    CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(true, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f);
                    //Set the new point to the old jump position.
                    obj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    obj.waypointType = WaypointType.boost; 

                    Vector3 posOld = PathConstructor.PrevWaypoint.jumpPosition;
                    posOld = Vector3.Lerp(posOld, obj.transform.position, 0.25f);
                    obj.transform.position = posOld + Vector3.up * 0.35f;

                    PlayerAIWaypoint obj2 = PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT);
                    obj2.transform.localScale = new Vector3(1.5f, 1.0f, 1.0f);
                    
                    obj.inAir = true;

                    DebugLog.LogMessage($" Already Jumped Air Boost Waypoint ");
                }
                else if (!__instance.IsGrounded())
                {
                    if (fromJump)
                    {
                        //Add JumpPoint
                        PlayerAIWaypoint obj2 = PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT);
                        obj2.transform.localScale = new Vector3(1.5f, 1.0f, 1.0f);
                    }

                    CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(true, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f);
                    obj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    obj.inAir = true;
                    obj.waypointType = WaypointType.boost;

                    DebugLog.LogMessage($" New Jump Air Boost Waypoint ");
                }
                else
                {
                    DebugLog.LogMessage($" Ground Boost");
                    CustomPlayerAiWaypoint obj2 = PathConstructor.addCustomWaypointOnPlayer(true, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f);
                    obj2.waypointType = WaypointType.boost;
                }
            }
            else if (a.GetType() == typeof(GrindAbility))
            {
                //DebugLog.LogMessage("Grind Start");
                
                bool flag = false;
                CustomPlayerAiWaypoint cust = PathConstructor.CurrentWaypoint as CustomPlayerAiWaypoint;
                flag = cust != null;

                if (flag)
                {
                    if (cust.fromJump && !cust.IsJumper())
                    {
                        PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT);
                    }
                }
                //else
                {
                    CustomPlayerAiWaypoint obj2 = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f);
                    obj2.waypointType = WaypointType.grind;
                }
            }
            else if (a.GetType() == typeof(AirDashAbility))
            {
                DebugLog.LogMessage($"airDash ");
                if (fromJump && !hasJumpPoint)
                {
                    DebugLog.LogMessage($"a ");
                    PlayerAIWaypoint obj = PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT);
                    obj.transform.localScale = new UnityEngine.Vector3(1f, 0.3f, 0.3f);
                }
                // else  // Removed this to place a visual indicator
                {
                    DebugLog.LogMessage($"b ");
                    CustomPlayerAiWaypoint obj2 = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.AIRDASH_AT_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f);
                    obj2.waypointType = WaypointType.airdash;
                    PlayerAIWaypoint obj = PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.AIRDASH_AT_JUMP_POINT);
                    obj.transform.localScale = new UnityEngine.Vector3(1.0f, 0.3f, 0.3f);
                    obj2.transform.localScale = new UnityEngine.Vector3(1.0f, 0.3f, 0.3f);
                }

                //stabilize boost direction 
                Vector3 dir = __instance.GetVelocity().normalized;
                dir.y = 0f;
                CustomPlayerAiWaypoint obj3 = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, false, false, 0.0f, false, false, true);
                obj3.waypointType = WaypointType.hidden;
                obj3.transform.position += dir.normalized * 1.5f;
                obj3.transform.localScale = new Vector3(1.25f, 0.0f, 0.0f);

                /**
                LineRenderer line = obj3.gameObject.GetComponent<LineRenderer>();
                line.SetPositions(new Vector3[] { obj3.transform.position, obj3.transform.position + new Vector3(1.0f, 1.0f, 0f) });
                line.startColor = Color.yellow;
                line.endColor = Color.yellow;
                **/

            }
            else if (a.GetType() == typeof(AirTrickAbility))
            {
                //DebugLog.LogMessage($" AirTrick ");
                
                bool createJumpPoint = fromJump && !PathConstructor.CurrentWaypoint.IsJumper();

                if (createJumpPoint)
                {
                    PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT);
                }

                //if (PathConstructor.CurrentWaypoint.IsJumper())
                {
                    CustomPlayerAiWaypoint n1 = (CustomPlayerAiWaypoint)PathConstructor.PrevWaypoint;

                    CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.TRICK_AT_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f);
                    CustomPlayerAiWaypoint n2 = (CustomPlayerAiWaypoint)PathConstructor.PrevWaypoint;

                    if (__instance.boostButtonHeld && PathConstructor.PrevWaypoint is CustomPlayerAiWaypoint)
                    {

                        if ( ( n2.waypointType == WaypointType.boostEnd || n2.waypointType == WaypointType.boost )
                            && (Vector3.Distance(n2.transform.position, obj.transform.position) < 1.0f))
                        {
                            //RemoveOldWaypoint
                            n2.waypointType = WaypointType.hidden;
                            DebugLog.LogMessage("Set Boost End hidden for Boost Trick");

                        }
                        if (n1 != null)
                        {
                            if (n1.waypointType == WaypointType.boost
                                && (Vector3.Distance(n1.position, obj.transform.position) < 1.0f))
                            {
                                //RemoveOldWaypoint
                                n1.waypointType = WaypointType.hidden;
                                DebugLog.LogMessage("Set Boost Start hidden for Boost Trick");
                            }
                        }
                    }
                    
                    obj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);


                    if (__instance.boostButtonHeld || __instance.boostButtonNew)
                    {
                        obj.waypointType = WaypointType.airtrickBoosted;
                    }
                    else 
                    {
                        obj.waypointType = WaypointType.airtrick;
                    }
                    
                    PlayerAIWaypoint obj2 = PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.TRICK_AT_JUMP_POINT);
                    obj2.transform.localScale = new Vector3(1.5f, 1.0f, 1.0f);

                    obj.TrickBoosted = __instance.boostButtonHeld;
                    //DebugLog.LogMessage($" AirTrick Air Waypoint | boosted {obj.TrickBoosted} ");
                }
                
                //bool flag = __instance.boostButtonHeld;
                //PathConstructor.CurrentWaypoint.boost = flag;

            }
            else if (a.GetType() == typeof(GroundTrickAbility))
            {
                DebugLog.LogMessage("GroundTrickAbility");
                CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(boostFlag, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, true, false, movestyleFlag, false, 0.0f);
                obj.TrickBoosted = boostFlag;
                if (boostFlag)
                {
                    obj.waypointType = WaypointType.trickBoosted;
                }
                else 
                {
                    obj.waypointType = WaypointType.trick;
                }
            }
            else if (a.GetType() == typeof(WallrunLineAbility))
            {
                // Get wallrun normal to offset the waypoint closer to the wall
                WallrunLineAbility wr = a as WallrunLineAbility;
                Vector3 Offset = wr.wallrunFaceNormal * -1.0f * 0.5f;
                

                bool flag = false;
                CustomPlayerAiWaypoint cust = PathConstructor.CurrentWaypoint as CustomPlayerAiWaypoint;
                flag = cust != null;
                CustomPlayerAiWaypoint obj;

                if (flag)
                {
                    if(cust.fromJump && !cust.IsJumper())
                    {
                        PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT);
                    }
                }

                obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f);
                
                if (wr.animSide == Side.RIGHT)
                {
                    obj.waypointType = WaypointType.wallrunRight;
                }
                else if (wr.animSide == Side.LEFT)
                {
                    obj.waypointType = WaypointType.wallrunLeft;
                }
                else
                {
                    obj.waypointType = WaypointType.wallrun;
                }

                if (obj != null)
                {
                    obj.transform.localScale = new UnityEngine.Vector3(1f, 0.3f, 0.3f);
                    obj.transform.position += Offset;
                }
            }
            else if (a.GetType() == typeof(SwitchMoveStyleAbility))
            {
                CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f);
                obj.waypointType = WaypointType.movestyleSwitch;
            }
            else if (a.GetType() == typeof(SpecialAirAbility))
            {
                CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f, false, true);
                obj.waypointType = WaypointType.airtrickBoosted;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("StopCurrentAbility")]
        public static void StopAbilityPatch(Player __instance, Ability ___preAbility, Ability ___ability)
        {

            if (!PathConstructor.isPlayerMakingPath(__instance)) { return; }

            if (___preAbility == null) { DebugLog.LogMessage($"Stop Ability | p.preAbility is null"); return; };

            //DebugLog.LogMessage($"Stop Ability pre: {___preAbility}  post: {___ability}");

            //DebugLog.LogMessage($"StopCurrentAbility | {___ability}");
            bool movestyleFlag = __instance.moveStyle == MoveStyle.ON_FOOT;
            bool boostFlag = __instance.boostButtonHeld;
            bool slideFlag = __instance.slideButtonHeld;

            /**
            bool doubleWaypoint = (__instance.transform.position - PathConstructor.CurrentWaypoint.position).magnitude < 0.15f;
            if (doubleWaypoint)
            {
                DebugLog.LogMessage("Double Waypoint");
            }
            **/

            bool isJumping = __instance.jumpRequested;
            if (isJumping) { DebugLog.LogMessage("Is Jump Early out"); ;  return; }

            bool fromJump = false;
            bool trickboosted = false;
            CustomPlayerAiWaypoint cust = PathConstructor.CurrentWaypoint as CustomPlayerAiWaypoint;
            if (cust != null)
            {
                fromJump = cust.fromJump;
                trickboosted = cust.TrickBoosted;
            }

            //If in the air and not a jump point add jump point
            if ( !PathConstructor.CurrentWaypoint.IsJumper() && fromJump ) //!__instance.IsGrounded() &&
            {
                //DebugLog.LogMessage("Stop Ability add jumpPoint");
                PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT);
            }

            //Switch on type
            if (___preAbility.GetType() == typeof(SlideAbility))
            {
                if (___ability != null || isJumping) { return; } //Dont bother if there is already another ability activated
                DebugLog.LogMessage($"Stop Slide");
                CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(boostFlag, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, slideFlag, movestyleFlag, false, 0.0f);
                obj.waypointType = WaypointType.hidden;
                obj.transform.localScale = new Vector3(1.5f,1,1);
            }
            else if (___preAbility.GetType() == typeof(BoostAbility))
            {
                DebugLog.LogMessage($"Stop BoostAbility");
                if (___ability != null || isJumping) { return; }
                CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(boostFlag, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, slideFlag, movestyleFlag, false, 0.0f);
                obj.waypointType = WaypointType.boostEnd;
                obj.transform.localScale = new Vector3(1.5f, 1, 1);
            }
            // On trick end make a null point to stop continous tricking
            else if (___preAbility.GetType() == typeof(GroundTrickAbility))
            {
                if (___ability != null) { return; }
                //DebugLog.LogMessage($"Stop GroundTrick");
                CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(boostFlag, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f);
                obj.waypointType = WaypointType.hidden;
                obj.transform.localScale = new Vector3(1.5f, 1, 1);
            }
            else if (___preAbility.GetType() == typeof(GrindAbility))
            {
                //DebugLog.LogMessage($"Stop Grind");
                if (___ability != null) { return; }
                CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(boostFlag, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, slideFlag, movestyleFlag, false, 0.0f);
                obj.waypointType = WaypointType.hidden;
                obj.transform.localScale = new Vector3(1.5f, 1, 1);
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch("CheckBoostTrick")]
        public static void CheckBoostTrickPatch(Player __instance, ref bool __result)
        {
            //If AI Overwrite the result
            if (__instance.isAI)
            {
                bool flag = false;

                CustomPlayerAiWaypoint cust = __instance.AI.comingFromWaypoint as CustomPlayerAiWaypoint;
                if (cust != null)
                {
                    flag = cust.TrickBoosted;
                    //DebugLog.LogMessage($" CheckBoost = {flag}");
                }

                __result = __instance.boostButtonHeld || flag && !__instance.boostAbility.locked;
            }
        }

        //Grind Leaning make nodes?
        [HarmonyPostfix]
        [HarmonyPatch("HardCornerGrindLine")]
        public static void HardCornerGrindLinePatch(Player __instance, GrindNode node)
        {
            if (!PathConstructor.isPlayerMakingPath(__instance)) { return; }

            PathConstructor.CurrentWaypoint.grindTiltCorner = __instance.grindAbility.grindTilt.x;

            CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, false, false, 0.0f);

            if (__instance.grindAbility.grindTilt.x > 0.0f)
            {
                obj.waypointType = WaypointType.grindLeft;
            }
            else
            {
                obj.waypointType = WaypointType.grindRight;
            }
            
        }

        /**
        [HarmonyPrefix]
        [HarmonyPatch("OnTriggerStay")]
        public static void OnTriggerStayPatch(Player __instance, Collider other)
        {
            DebugLog.LogMessage($"TriggerStay | Layer : {other.gameObject.layer} | Tag : {other.gameObject.tag}  ");
        }
        **/

        [HarmonyPostfix]
        [HarmonyPatch("SetMoveStyle")]
        public static void SetMoveStylePatch(Player __instance)
        {
            //Increase the roation rate for AI
            if (__instance.isAI)
            {
                __instance.stats.rotSpeedAtMaxSpeed *= 2.0f;
                __instance.stats.rotSpeedInAir *= 2.0f;
            }
        }
        

    }

    [HarmonyPatch(typeof(GrindAbility))]
    class GrindAbilityPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Grind Ability Patches");

        static bool boostToggle = false;

        [HarmonyPostfix]
        [HarmonyPatch("JumpOut")]
        public static void JumpOutPatch(GrindAbility __instance, Player ___p)
        {
            if (!PathConstructor.isPlayerMakingPath(___p)) { return; }


            bool flag = ___p.slideButtonHeld;

            //DebugLog.LogMessage($"Grind Jump");


            // Adding a waypoint to activate slide early so that the jump will process the slide input.
            /**
            if (flag)
            {
                PlayerAIWaypoint obj2 = PathConstructor.addWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, flag, false, false, 0.0f);
                obj2.transform.localScale = new UnityEngine.Vector3(0.4f, 0.4f, 0.4f);
            }
            **/

            //Add a jump root waypoint. Will add jumpPoint on dash/trick or land
            CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, flag, false, false, 0.0f, false, true);
            obj.transform.localScale = new UnityEngine.Vector3(0.2f, 0.2f, 0.1f);
            obj.waypointType = WaypointType.jump;

            if (PathConstructor.PrevWaypoint != null)
            {
                //DebugLog.LogMessage($"Set Prevwaypoint Slide : {flag}");
                PathConstructor.PrevWaypoint.slide = flag;
            }
        }

        /**
        [HarmonyPostfix]
        [HarmonyPatch("RewardTilting")]
        public static void RewardTiltingPatch(GrindAbility __instance)
        {
            if (!PathConstructor.isPlayerMakingPath(__instance.p)) { return; }

            if (!__instance.grindLine.cornerBoost)
            {
                return;
            }

            //Test For Boost
            if (__instance.speed > __instance.p.stats.grindSpeed)
            { 
                
            }


        }
        **/

        [HarmonyPostfix]
        [HarmonyPatch("StartGrindTrick")]
        public static void StartGrindTrickPatch(GrindAbility __instance, bool first)
        {
            if (!PathConstructor.isPlayerMakingPath(__instance.p) || first) { return; }

            //DebugLog.LogMessage($"Grind Trick");
            CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, true, false, false, false, 0.0f);

            obj.waypointType = __instance.curTrickBoost ? WaypointType.grindTrickBoosted : WaypointType.grindTrick;
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateTricks")]
        public static void UpdateTricksPatch(GrindAbility __instance)
        {
            if (!PathConstructor.isPlayerMakingPath(__instance.p)) { return; }

            if (__instance.trickTimer > 0f)
            {
                if (__instance.trickTimer - Core.dt < 0.0f)
                {
                    //Grind Trick End
                    //DebugLog.LogMessage($"Grind Trick End");
                    CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, false, false, 0.0f);
                    obj.waypointType = WaypointType.hidden;
                    obj.noEncounter = true;
                }

            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("FixedUpdateAbility")]
        public static void FixedUpdateAbilityPatch(GrindAbility __instance)
        {
            if (!PathConstructor.isPlayerMakingPath(__instance.p)) { return; }

            if (__instance.p.inputBuffer.boostButtonNew == true)
            {
                //DebugLog.LogMessage($"Grind Trick");
                CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(true, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, false, false, 0.0f);
                obj.waypointType = WaypointType.boost;
                boostToggle = true;
            }
            else if (boostToggle == true && __instance.p.inputBuffer.boostButtonHeld == false)
            {
                CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, false, false, 0.0f);
                obj.waypointType = WaypointType.boostEnd;
                boostToggle = false;
            }
            
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnStopAbility")]
        public static void OnStopAbilityPatch(GrindAbility __instance)
        {
            boostToggle = false;
        }
    }

    [HarmonyPatch(typeof(WallrunLineAbility))]
    class WallrunAbilityPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Wallrun Ability Patches");

        [HarmonyPostfix]
        [HarmonyPatch("Jump")]
        public static void JumpPatch(WallrunLineAbility __instance, Player ___p)
        {
            if (!PathConstructor.isPlayerMakingPath(___p)) { return; }

            //DebugLog.LogMessage("Wallrun Jump");

            bool movestyleFlag = ___p.moveStyle == MoveStyle.ON_FOOT;

            bool flag = ___p.slideButtonHeld;

            //Add a jump root waypoint. Will add jumpPoint on dash/trick or land
            CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, flag, movestyleFlag, false, 0.0f, false, true);
            obj.transform.localScale = new UnityEngine.Vector3(0.5f, 1.0f, 1.0f);
            obj.waypointType = WaypointType.jump;

            Vector3 Offset = __instance.wallrunFaceNormal * -1.0f * 0.25f;
            obj.transform.position += Offset;
            obj.transform.localPosition -= new Vector3(0.5f, 0f, 0f);

        }

        [HarmonyPostfix]
        [HarmonyPatch("RunOff")]
        public static void RunOffPatch(WallrunLineAbility __instance, Player ___p)
        {
            if (!PathConstructor.isPlayerMakingPath(___p)) { return; }

            // DebugLog.LogMessage("Wallrun Runoff");

            bool movestyleFlag = ___p.moveStyle == MoveStyle.ON_FOOT;

            bool flag = ___p.slideButtonHeld;

            CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(___p.boosting, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, flag, movestyleFlag, false, 0.0f);
            obj.transform.localScale = new UnityEngine.Vector3(0.5f, 1.0f, 1.0f);
            obj.waypointType = WaypointType.wallrun;

            Vector3 Offset = __instance.wallrunFaceNormal * -1.0f * 0.5f;
            obj.transform.position += Offset;
        }

        /**
        [HarmonyPostfix]
        [HarmonyPatch("OnStartAbility")]
        public static void OnStartAbilityPatch(WallrunLineAbility __instance, Player ___p)
        {
            if (!PathConstructor.isPlayerMakingPath(___p)) { return; }

            bool movestyleFlag = ___p.moveStyle == MoveStyle.ON_FOOT;

            DebugLog.LogMessage("Wallrun Start Waypoint");

            if (PathConstructor.CurrentWaypoint.IsJumper())
            {
                PlayerAIWaypoint obj = PathConstructor.addWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.REACH_WITH_AIRDASH_IF_FAR, false, 0.0f, false, false, movestyleFlag, false, 0.0f);
                obj.transform.localScale = new UnityEngine.Vector3 (0.2f, 1.0f, 1.0f );
            }
            else 
            {
                PlayerAIWaypoint obj = PathConstructor.addJumpWaypoint(PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT);
                obj.transform.localScale = new UnityEngine.Vector3(0.2f, 1.0f, 1.0f);
            }

        }
        **/
    }

    [HarmonyPatch(typeof(GroundTrickAbility))]
    class GroundTrickAbilityPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} GroundTrickAbility Ability Patches");

        [HarmonyPostfix]
        [HarmonyPatch("OnStopAbility")]
        public static void OnStopAbility(GroundTrickAbility __instance, Player ___p)
        {
            if (!PathConstructor.isPlayerMakingPath(___p)) { return; }

            //DebugLog.LogMessage($"GroundTrick Stop");

            bool movestyleFlag = ___p.moveStyle == MoveStyle.ON_FOOT;

            bool flag = ___p.slideButtonHeld;

            if (___p.ability.GetType() == typeof(GroundTrickAbility))
            {
                CustomPlayerAiWaypoint obj = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f);
                obj.noEncounter = true;
                obj.waypointType = WaypointType.hidden;
            }
        }
    }

    [HarmonyPatch(typeof(DanceAbility))]
    class DanceAbilityPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Player Ai Patches");

        [HarmonyPrefix]
        [HarmonyPatch("OnStartAbility")]
        public static bool OnStartAbilityPatch(DanceAbility __instance)
        {
            if (__instance.p.isAI)
            {
                __instance.danceNumber = 0;
                __instance.p.SwitchToEquippedMovestyle(false, false, true, true);
                __instance.p.PlayAnim(__instance.p.characterVisual.bounceAnimHash, false, false, -1f);
                //__instance.SetState(DanceAbility.State.SELECT);

                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnStopAbility")]
        public static bool OnStopAbilityPatch(DanceAbility __instance)
        {
            if (__instance.p.isAI)
            {
                return false;
            }

            return true;
        }

    }

    [HarmonyPatch(typeof(HandplantAbility))]
    class HandplantAbilityPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} HandplantAbility");

        [HarmonyPostfix]
        [HarmonyPatch("SetToPole")]
        [HarmonyPatch( new Type[] { typeof(Vector3), typeof(SkateboardScrewPole) })]
        public static void SetToPolePatch(HandplantAbility __instance)
        {
            if (!PathConstructor.isPlayerMakingPath(__instance.p)) { return; }

            bool movestyleFlag = __instance.p.moveStyle == MoveStyle.ON_FOOT;

            CustomPlayerAiWaypoint obj2 = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, (float)DateTime.Now.TimeOfDay.TotalSeconds);
            obj2.waypointType = WaypointType.basic;
            //DebugLog.LogMessage($"Pole Waypoint");
        }

        [HarmonyPostfix]
        [HarmonyPatch("SetToPole")]
        [HarmonyPatch(new Type[] { typeof(Vector3) })]
        public static void SetToPolePatch_b(HandplantAbility __instance)
        {
            //DebugLog.LogMessage($"SetToPole");

            if (!PathConstructor.isPlayerMakingPath(__instance.p)) { return; }

            bool movestyleFlag = __instance.p.moveStyle == MoveStyle.ON_FOOT;

            CustomPlayerAiWaypoint obj2 = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, (float)DateTime.Now.TimeOfDay.TotalSeconds);
            obj2.waypointType = WaypointType.basic;
            //DebugLog.LogMessage($"Pole Waypoint");
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnStopAbility")]
        public static void OnStopAbilityPatch(HandplantAbility __instance)
        {
            //DebugLog.LogMessage($"OnStopAbilityPatch");

            if (!PathConstructor.isPlayerMakingPath(__instance.p)) { return; }

            float time= PathConstructor.CurrentWaypoint.wait;

            time = (float)DateTime.Now.TimeOfDay.TotalSeconds - time;

            PathConstructor.CurrentWaypoint.wait = time;
            //DebugLog.LogMessage($"On Handplant Stop wait | {time}");
        }

        [HarmonyPrefix]
        [HarmonyPatch("FixedUpdateAbility")]
        public static bool FixedUpdateAbilityPatch(HandplantAbility __instance)
        {
            if (__instance.p.isAI)
            {
                if (__instance.p.AI.waitTimer < __instance.p.AI.comingFromWaypoint.wait)
                {
                    //DebugLog.LogMessage($"Handplant AI waiting");
                    return false;
                }
            }
            return true;
        }

    }

    [HarmonyPatch(typeof(AirTrickAbility))]
    class AirTrickAbilityPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} AirTrickAbility");

        [HarmonyPostfix]
        [HarmonyPatch("SetupBoostTrick")]
        public static void SetupBoostTrickPatch(AirTrickAbility __instance)
        {
            //Overwrite to Allow Ai to use boostspeed
            if (__instance.p.isAI)
            {
                __instance.p.SetForwardSpeed(__instance.p.boostSpeed);
            }
        }  
    }

    [HarmonyPatch(typeof(SlideAbility))]
    class SlideAbilityPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} AirTrickAbility");

        [HarmonyPostfix]
        [HarmonyPatch("OnJump")]
        public static void OnJumpPatch(SlideAbility __instance)
        {
            /**
            bool movestyleFlag = __instance.p.moveStyle == MoveStyle.ON_FOOT;

            if (!PathConstructor.isPlayerMakingPath(__instance.p)) { return; }
            PlayerAIWaypoint obj2 = PathConstructor.addCustomWaypointOnPlayer(false, PlayerAIWaypoint.JumpPointBehavoir.JUST_MOVE_TO_JUMP_POINT, false, 0.0f, false, false, movestyleFlag, false, 0.0f, false, true);
            DebugLog.LogMessage($"Add Slide Jump waypoint");
            **/
        }
    }

    /**
    [HarmonyPatch(typeof(AirDashAbility))]
    class AirDashAbilityPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} AirTrickAbility");

        [HarmonyPostfix]
        [HarmonyPatch("OnStartAbility")]
        public static void OnStartAbility(AirDashAbility __instance)
        {
            if (!PathConstructor.isPlayerMakingPath(__instance.p)) { return; }

            DebugLog.LogMessage("AirDash Offset Waypoint");

        }
    }**/

    //General Patches

    [HarmonyPatch(typeof(PlayerAI))]
    class PlayerAIPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Player Ai Patches");

        [HarmonyPostfix]
        [HarmonyPatch("SetPath")]
        public static void SetPathPatch(PlayerAI __instance, PlayerAIPath p, bool setWaypointAlso, bool teleport)
        {
            //Set the Ai to the Correct Movestyle when the Path is set

            if (setWaypointAlso)
            {
                CustomPlayerAiWaypoint point = __instance.path.firstWaypoint as CustomPlayerAiWaypoint;
                if (point != null)
                {
                    __instance.self.SetCurrentMoveStyleEquipped(point.movestyle);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateJumping")]
        public static void UpdateJumpingPatch(PlayerAI __instance)
        {
            //Default system cannot process slide jumps on ai. This is a work around to check the state
            //of slide input in the jumpupdate
            if (__instance.preparingToJump)
            {
                __instance.input.boostButtonHeld = __instance.comingFromWaypoint.boost;
                __instance.input.slideButtonHeld = __instance.comingFromWaypoint.slide;
            }
            else //Is in jumpstate
            {
                //Check AirBoost
                __instance.input.boostButtonHeld = __instance.comingFromWaypoint.boost;
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch("SetNextWaypoint")]
        public static void SetNextWaypointPatch(PlayerAI __instance, Player ___self)
        {
            //If the character is already in the air skip prepare to jump step
            if (__instance.comingFromWaypoint.IsJumper())
            {
                bool usingBlockedAbility = false;
                if (___self.ability != null)
                {
                    usingBlockedAbility = ___self.ability.GetType() == typeof(GrindAbility);
                    usingBlockedAbility |= ___self.ability.GetType() == typeof(WallrunLineAbility);
                    usingBlockedAbility |= ___self.ability.GetType() == typeof(FlipOutJumpAbility);
                    usingBlockedAbility |= ___self.ability.GetType() == typeof(HandplantAbility);
                    usingBlockedAbility |= ___self.ability.GetType() == typeof(GroundTrickAbility);
                }

                if (!___self.IsGrounded() && !usingBlockedAbility)
                {
                    __instance.preparingToJump = false;
                }
            }

            if (__instance.nextWaypoint == null)
            {
                CustomPlayerAiWaypoint point = __instance.comingFromWaypoint as CustomPlayerAiWaypoint;
                if (point != null)
                {
                    if (point.end)
                    {
                        //Delete Holo
                        WorldHandler.instance.RemovePlayer(__instance.self);
                        UnityEngine.Object.Destroy(__instance.self.gameObject);
                    }
                }
            }
            else
            {
                CustomPlayerAiWaypoint point = __instance.comingFromWaypoint as CustomPlayerAiWaypoint;
                if (point != null)
                {
                    if (point.dance >= 0)
                    {
                        __instance.self.ActivateAbility(__instance.self.danceAbility);
                        __instance.self.PlayAnim(__instance.self.characterVisual.bounceAnimHash, false, false, -1f);
                        DebugLog.LogMessage($"dance hash | {__instance.self.danceAbility.danceHashes[point.dance]} ");
                    }
                }
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateNormalMove")]
        public static void UpdateNormalMovePatch(PlayerAI __instance)
        {
            if (__instance.comingFromWaypoint)
            {
                //If dancing check for wait to end ability
                if (__instance.comingFromWaypoint.wait < __instance.waitTimer && __instance.AbilityIs(__instance.self.danceAbility))
                {
                    __instance.self.StopCurrentAbility();
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("Rubberbanding")]
        public static bool RubberbandingPatch(PlayerAI __instance)
        {
            if (__instance.alignToNPCWaitTimer == -66)
            {
                return false;
            }
            return true;
        }


    }


    [HarmonyPatch(typeof(Encounter))]
    class EncounterPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Encounter");

        [HarmonyPrefix]
        [HarmonyPatch("SetEncounterState")]
        public static void SetEncounterStatePatch(Encounter __instance, Encounter.EncounterState setState)
        {
            DebugLog.LogMessage($"State: {setState}");

            DebugLog.LogMessage($"OnIntro: {__instance.OnIntro}");
            
            if (__instance.introSequence != null && __instance.currentCheckpoint == -1)
            {
                DebugLog.LogMessage($"Test Checks");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("ChangeSkybox")]
        public static void ChangeSkyboxPatch(Encounter __instance)
        {
            DebugLog.LogMessage($"Change Skybox");
        }

        [HarmonyPrefix]
        [HarmonyPatch("WriteToData")]
        public static bool WriteToDataPatch(Encounter __instance)
        {
            DebugLog.LogMessage($"Write To Data");

            if ((EncounterProgress)__instance.progressableData == null)
            {
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("EnterEncounterState")]
        public static void EnterEncounterStatePatch(Encounter __instance)
        {
            DebugLog.LogMessage($"Enter Encounter State");
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateMainEvent")]
        public static void UpdateMainEventPatch(Encounter __instance)
        {
            //DebugLog.LogMessage($"UpdateMainEvent");
        }
    }

    [HarmonyPatch(typeof(ComboEncounter))]
    class ComboEncounterPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} ComboEncounter");

        [HarmonyPostfix]
        [HarmonyPatch("ReadyPlayer")]
        public static void ReadyPlayerPatch(ComboEncounter __instance, ref float ___playerInStartTriggerTimer)
        {
            ___playerInStartTriggerTimer = 15f;
        }

        [HarmonyPostfix]
        [HarmonyPatch("TriggerDetectPlayer")]
        public static void TriggerDetectPlayerPatch(ComboEncounter __instance)
        {
            DebugLog.LogMessage("TriggerDetectPlayer Combo Encounter");
        }
    }

}
