using Reptile;
using UnityEngine;
using System;
using BepInEx.Logging;
using HarmonyLib;
using Sicklines.AiPaths;


namespace Sicklines
{
    class Sicklines_Encounter : ComboEncounter
    {

        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Sicklines_Encounter");

        public CustomAiPath Path;

        public void StartIntro()
        {
            this.ReadyPlayerOverride();
        }

        public override void StartMainEvent()
        {
            //Reset checkpoints
            currentCheckpoint = 0;

            this.trackSetup.SetActive(true);
            
            this.StartPlayer();

            foreach (Collider trigger in this.checkpointTriggers)
            {
                trigger.gameObject.SetActive(true);
            }
        }

        public override void TriggerDetectPlayer(Collider trigger, Player player)
        {
            if (player.isAI) { return; }

            if (currentCheckpoint >= checkpointTriggers.Count) { return; }

            bool isNextTrigger = this.checkpointTriggers[currentCheckpoint] == trigger;

            SickLines_WaypointType type = trigger.gameObject.GetComponent<SickLines_WaypointType>();

            if (type == null)
            {
                DebugLog.LogMessage($"Not SickLines Collision");
                return;
            }
                
            bool CorrectAction = IsDoingWaypointAction(type.WaypointType);

            if (isNextTrigger && CorrectAction)
            {
                //DebugLog.LogMessage("Triggered Current Checkpoint");

                currentCheckpoint++;

                PlayCollectSound();

                trigger.gameObject.SetActive(false);

                base.TriggerDetectPlayer(trigger, player);
            }

        }

        public override void EnterEncounterState(Encounter.EncounterState setState)
        {
            if (setState == Encounter.EncounterState.MAIN_EVENT_SUCCES_DECAY)
            {
                PlaySuccessSound();
                successfulRun();
            }
            else if (setState == Encounter.EncounterState.MAIN_EVENT_FAILED_DECAY)
            {
                PlayFailSound();
                FailedRun();
            }

            base.EnterEncounterState(setState);
        }

        private void PlayCollectSound()
        {
            //DebugLog.LogMessage($"Sound | AudioSource : {player.audioManager.audioSources[0]} | hits {this.checkpointTriggersHit.Count}");
            Core.Instance.AudioManager.PlaySfxGameplay(SfxCollectionID.EnvironmentSfx, AudioClipID.MascotHit, 0f);
            //Core.Instance.AudioManager.PlaySfxGameplayPitched(SfxCollectionID.EnvironmentSfx, AudioClipID.MascotHit, player.audioManager.audioSources[0], 1f + 0.07f * (float)this.checkpointTriggersHit.Count);
        }
        private void PlaySuccessSound()
        {
            Core.Instance.AudioManager.PlaySfxGameplay(SfxCollectionID.EnvironmentSfx, AudioClipID.MascotUnlock, 0f);
        }
        private void PlayFailSound()
        {
            Core.Instance.AudioManager.PlaySfxGameplay(SfxCollectionID.EnvironmentSfx, AudioClipID.MineExplosion, 0f);
            //Core.Instance.AudioManager.PlaySfxGameplay(SfxCollectionID.EnvironmentSfx, AudioClipID.MascotUnlock, 0f);
            //DebugLog.LogMessage("Play Fail Sound");
        }

        public void ReadyPlayerOverride()
        {
            Player currentPlayer = WorldHandler.instance.GetCurrentPlayer();

            if(currentPlayer == null) { DebugLog.LogMessage("currentPlayer == null"); }
            currentPlayer.cam.ResetCameraPositionRotation();
            WorldHandler.instance.PlaceCurrentPlayerAt(this.playerSpawner, true);
            PlayerSpawner component = this.playerSpawner.GetComponent<PlayerSpawner>();
            if (component != null)
            {
                component.SetReached();
            }
            this.checkpointTriggersHit.Clear();
            //DebugLog.LogMessage("CurrentPlayer");
            currentPlayer.ClearMultipliersDone();
            currentPlayer.lastScore = 0f;
            currentPlayer.tricksInLastCombo = 0;
            currentPlayer.lastCornered = 0;
            //setup movestyle
            currentPlayer.SetCurrentMoveStyleEquipped(Path.movestyleEquipped);
            if (Path.startUsingMovestyle)
            {
                currentPlayer.SwitchToEquippedMovestyle(true, false, true, false);
            }
            else
            {
                currentPlayer.SwitchToEquippedMovestyle(false, false, true, false);
            }

            //currentPlayer.ActivateAbility(currentPlayer.switchMoveStyleAbility);

            //Disable Input on setup
            currentPlayer.userInputEnabled = false;
        }

        public void StartPlayer()
        {
            Player currentPlayer = WorldHandler.instance.GetCurrentPlayer();
            currentPlayer.userInputEnabled = true;
            this.playerInStartTriggerTimer = 3f;
        }

        public void FailedRun()
        {
            Player currentPlayer = WorldHandler.instance.GetCurrentPlayer();

            if (currentPlayer == null) { DebugLog.LogMessage("currentPlayer == null"); }
            currentPlayer.cam.ResetCameraPositionRotation();
            WorldHandler.instance.PlaceCurrentPlayerAt(this.playerSpawner, true);

            foreach (Collider trigger in this.checkpointTriggers)
            {
                trigger.gameObject.SetActive(false);
            }

            //Reset the LineTrigger on Fail
            this.Path.triggerLine.GetComponent<SickLines_Trigger>().OnFailed();
        }

        public void successfulRun()
        {
            //If we have a pathfile on record
            if (PathLoader.Instance.tryGetPathfileFromCustomPath(this.Path, out string filePath))
            {
                // Save the completion of the path.
                SickLines_Save.Instance.setPathState(filePath, true);
            }

            PathConstructor.setLineTriggerColour(this.Path.triggerLine, SickLines_Trigger.Linestate.Complete);
        }

        public bool IsDoingWaypointAction( WaypointType waypointType)
        {
            bool success = false;
            
            switch (waypointType)
            {
                default:
                    success = true;
                    break;

                case WaypointType.basic:

                    success = true;
                    break;

                case WaypointType.jump:

                    //Should It check Jump?
                    success = true;
                    break;

                case WaypointType.landing:

                    success = true;
                    break;

                case WaypointType.trick:

                    success = this.player.ability is GroundTrickAbility;
                    break;

                case WaypointType.trickBoosted:

                    success = this.player.ability is GroundTrickAbility && (this.player.boostButtonHeld || this.player.boostButtonNew);
                    break;

                case WaypointType.slide:

                    success = this.player.ability is SlideAbility;
                    break;

                case WaypointType.slideEnd:

                    success = true;
                    break;

                case WaypointType.wallrun:

                    success = true;
                    break;

                case WaypointType.wallrunLeft:

                    success = true;
                    break;

                case WaypointType.wallrunRight:

                    success = true;
                    break;

                case WaypointType.airtrick:

                    success = this.player.ability is AirTrickAbility;
                    break;

                case WaypointType.airtrickBoosted:

                    success = this.player.ability is AirTrickAbility && (this.player.boostButtonHeld || this.player.boostButtonNew);
                    break;

                case WaypointType.airdash:

                    success = this.player.ability is AirDashAbility;
                    break;

                case WaypointType.grind:

                    success = true;
                    break;

                case WaypointType.grindLeft:

                    if (this.player.ability is GrindAbility)
                    {
                        GrindAbility ab2 = this.player.ability as GrindAbility;
                        success = ab2.cornerBoost > 0.0f;
                    }
                    break;

                case WaypointType.grindRight:

                    if (this.player.ability is GrindAbility)
                    {
                        GrindAbility ab1 = this.player.ability as GrindAbility;
                        success = ab1.cornerBoost > 0.0f;
                    }
                    break;

                case WaypointType.grindTrick:

                    if (this.player.ability is GrindAbility)
                    {
                        GrindAbility ab1 = this.player.ability as GrindAbility;
                        success = ab1.trickTimer > 0.0f && !ab1.curTrickBoost;
                    }
                    break;

                case WaypointType.grindTrickBoosted:

                    if (this.player.ability is GrindAbility)
                    {
                        GrindAbility ab1 = this.player.ability as GrindAbility;
                        success = ab1.trickTimer > 0.0f && ab1.curTrickBoost;
                    }
                    break;

                case WaypointType.boost:

                    success = this.player.boosting;

                    break;

                case WaypointType.boostEnd:

                    success = !this.player.boosting;
                    break;

                case WaypointType.movestyleSwitch:

                    success = this.player.ability is SwitchMoveStyleAbility;
                    break;

                case WaypointType.dance:

                    success = true;
                    break;

                case WaypointType.hidden:

                    success = true;
                    break;

            }

            return success;
        }
    }
}


