using CommonAPI;
using CommonAPI.Phone;
using Sicklines.Utility;
using Reptile;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Logging;
using Sicklines.AiPaths;


namespace Sicklines.App
{
    public class SickLinesApp : CustomApp
    {

        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} Player Patches");

        static Sprite Icon = null;

        static List<Player> SpawnedAi;

        static PathLoader pathLoader;

        public static void Initialize()
        {
            Texture2D texture = TextureUtil.GetTextureFromBitmap(Properties.Resources.phoneAppIcon);
            Icon = TextureUtility.CreateSpriteFromTexture(texture);
            PhoneAPI.RegisterApp<SickLinesApp>("Crew", Icon);
            SpawnedAi = new List<Player>();
            pathLoader = new PathLoader();
        }

        public override void OnAppInit()
        {
            base.OnAppInit();

            CreateTitleBar("CallCrew", Icon);
            ScrollView = PhoneScrollView.Create(this);

            var bingoButton = PhoneUIUtility.CreateSimpleButton("Call");
            bingoButton.OnConfirm += () =>
            {
                Player player = WorldHandler.instance.GetCurrentPlayer();
                PlayerSpawner Spawner = WorldHandler.instance.GetDefaultPlayerSpawnPoint();
                if (Spawner == null)
                {
                    DebugLog.LogMessage("Spawner is Null");
                }

                Transform spawnPosition = Spawner.gameObject.transform;

                GetAiPathCharacter(out Characters _character, out int _outfit, out MoveStyle _movestyle);

                Player aiPlayer = WorldHandler.instance.SetupAIPlayerAt(spawnPosition, _character, PlayerType.NONE, _outfit, _movestyle);
                SpawnedAi.Add(aiPlayer);

                SetAiPath(aiPlayer);
                aiPlayer.AI.state = PlayerAI.PlayerAIState.NORMAL_MOVE;

                PathConstructor.setAiPlayerMaterial(aiPlayer, true);
                aiPlayer.AI.alignToNPCWaitTimer = -66;

            };

            ScrollView.AddButton(bingoButton);

            var btnStartPath = PhoneUIUtility.CreateSimpleButton("Create Path");
            btnStartPath.OnConfirm += () =>
            {
                Player player = WorldHandler.instance.GetCurrentPlayer();
                if (PathConstructor.isPlayerMakingPath(player))
                {
                    btnStartPath.Label.fontStyle = TMPro.FontStyles.Normal;
                    PathConstructor.endPath();
                    btnStartPath.Label.text = "Create Path";
                    return;
                }
                else
                {
                    if (!PathConstructor.requestNewPath(player))
                    {
                        btnStartPath.Label.fontStyle = TMPro.FontStyles.Bold;
                        btnStartPath.Label.text = "Create Path";

                    }
                    else
                    {
                        //btnStartPath.Label.fontStyle = TMPro.FontStyles.Normal;
                        //btnStartPath.Label.text = "End Path";
                    }
                }
            };

            ScrollView.AddButton(btnStartPath);

            var btnClearAi = PhoneUIUtility.CreateSimpleButton("Clear Ai");
            btnClearAi.OnConfirm += () =>
            {
                foreach (Player player in SpawnedAi)
                {
                    WorldHandler.instance.RemovePlayer(player);
                    Destroy(player.characterVisual);
                    Destroy(player);
                }
                SpawnedAi.Clear();
            };

            ScrollView.AddButton(btnClearAi);

            var btnSave = PhoneUIUtility.CreateSimpleButton("Save Path");
            btnSave.OnConfirm += () =>
            {
                int PathID = PathConstructor.CustomPathsList.Count - 1;
                if (PathID < 0) { return; }
                CustomAiPath path = PathConstructor.CustomPathsList[PathID];
                path.pathToJson();

            };

            ScrollView.AddButton(btnSave);

            var btnTestLoad = PhoneUIUtility.CreateSimpleButton("Load Path");
            btnTestLoad.OnConfirm += () =>
            {
                pathLoader.OnNewScene();

                DebugLog.LogMessage($"path count {pathLoader.pathsToLoad.Count} ");

                if (pathLoader.pathsToLoad.Count > 0)
                {
                    DebugLog.LogMessage($"Trying to load: {pathLoader.pathsToLoad[0]}");
                    pathLoader.LoadPath(pathLoader.pathsToLoad[0]);   
                }

            };
            //ScrollView.AddButton(btnTestLoad);


        }

        //Sets AI path to random path
        public void SetAiPath(Player ai)
        {
            
            List<PlayerAIPath> Paths = WorldHandler.instance.sceneObjectsRegister.paths;

            //Testing use the last custom path

            int PathID = PathConstructor.CustomPathsList.Count - 1;

            if (PathID < 0) { return; }

            ai.boostCharge = 80.0f;

            PlayerAIPath path = PathConstructor.CustomPathsList[PathID].RuntimePath;

            ai.AI.SetPath(path, true, true);
            
            ai.transform.position = path.firstWaypoint.position;

        }

        public void GetAiPathCharacter(out Characters character , out int Outfit, out MoveStyle moveStyle)
        {

            character = Characters.metalHead;
            Outfit = 0;
            moveStyle = MoveStyle.SKATEBOARD;

            List<PlayerAIPath> Paths = WorldHandler.instance.sceneObjectsRegister.paths;

            int PathID = PathConstructor.CustomPathsList.Count - 1;
            if (PathID < 0) { return; }

            CustomAiPath path = PathConstructor.CustomPathsList[PathID];

            character =  path.character;
            Outfit = path.outfit;
            moveStyle = path.movestyleEquipped;
        }
    }
}
