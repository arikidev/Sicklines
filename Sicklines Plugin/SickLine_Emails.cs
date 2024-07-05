using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using UnityEngine;
using EmailApi;
using Reptile.Phone;
using HarmonyLib;
using BepInEx;
using Reptile;
using Sicklines.Utility;
using CommonAPI;

namespace Sicklines
{
    public static class SickLine_Emails
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} SickLine_Emails");

        static int msgSenderID = 9092;
        static UnityEngine.Color msgSenderColor = new UnityEngine.Color(1f, 0.9f, 0.9f);

        static public CustomContact contact;

        static public string tutorialEmailID = PluginInfo.PLUGIN_NAME + "_Tutorial";
        static EmailMessage tutorialEmail;
        static List<string> tutorialMessage = new List<string>() {
        "To create your own lines use the create button in the app",
        "Saved lines will be loaded next time you open the game",
        "You can share your lines with others by sharing .path files",
        "they are found in the sicklines/config folder"}; 

        public static void Initialize()
        {
            Texture2D texture = TextureUtil.GetTextureFromBitmap(Properties.Resources.SickLinesEmail, FilterMode.Point);
            contact = EmailManager.RegisterCustomContact("Sick Lines", msgSenderID, texture);

            tutorialEmail = EmailManager.CreateEmailMessage(tutorialEmailID, msgSenderID, "Tutorial", msgSenderColor, tutorialMessage);
            EmailManager.AddEmailMessage(tutorialEmail);
        }
    }

    [HarmonyPatch(typeof(Player))]
    class PlayerEmailPatch
    {
        private static ManualLogSource DebugLog = BepInEx.Logging.Logger.CreateLogSource($"{PluginInfo.PLUGIN_NAME} SickLine_Emails");


        [HarmonyPostfix]
        [HarmonyPatch("Init")]
        public static void InitPatch(Player __instance)
        {
            if (__instance != WorldHandler.instance.currentPlayer) { return; }

            //DebugLog.LogMessage("Init Path");
            //If it has not been read then try the email notification for movestyler
            //DebugLog.LogMessage($"Email State Check {EmailSave.Instance.getMessageState(SickLine_Emails.tutorialEmailID)}");
            if (!EmailSave.Instance.getMessageState(SickLine_Emails.tutorialEmailID))
            {
                //DebugLog.LogMessage("EmailNotificationDelayed");
                EmailManager.EmailNotificationDelayed(SickLine_Emails.tutorialEmailID, true, 5.0f);
            }
        }
    }

}
