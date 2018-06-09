using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using Harmony;
using Manager;

namespace CardCacher
{
    static class Hooks
    {
        public static HarmonyInstance Instance { get; private set; }

        public static void InstallHooks()
        {
            Instance = HarmonyInstance.Create("com.bepis.bepinex.cardcacher");
            Instance.PatchAll(typeof(Hooks));
        }

        private static FieldInfo listCtrl_info =
            typeof(CustomCharaFile).GetField("listCtrl", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPrefix, HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
        public static bool InitializePrehook(CustomCharaFile __instance)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();


            int modeSex = Singleton<CustomBase>.Instance.modeSex;
            FolderAssist folderAssist = new FolderAssist();
            string[] searchPattern = new string[]
            {
                "*.png"
            };
            string folder = UserData.Path + ((modeSex != 0) ? "chara/female/" : "chara/male/");
            folderAssist.CreateFolderInfoEx(folder, searchPattern, true);

            CustomFileListCtrl listCtrl = (CustomFileListCtrl)listCtrl_info.GetValue(__instance);
            
            listCtrl.ClearList();
            int fileCount = folderAssist.GetFileCount();
            int num = 0;
            for (int i = 0; i < fileCount; i++)
            {
                ChaFileControl chaFileControl = new ChaFileControl();
                if (!chaFileControl.LoadCharaFile(folderAssist.lstFile[i].FullPath, 255, false, true))
                {
                    int lastErrorCode = chaFileControl.GetLastErrorCode();
                }
                else
                {
                    string club = string.Empty;
                    string personality = string.Empty;
                    if (modeSex != 0)
                    {
                        VoiceInfo.Param param;
                        if (!Singleton<Voice>.Instance.voiceInfoDic.TryGetValue(chaFileControl.parameter.personality, out param))
                        {
                            personality = "不明";
                        }
                        else
                        {
                            personality = param.Personality;
                        }
                        ClubInfo.Param param2;
                        if (!Game.ClubInfos.TryGetValue((int)chaFileControl.parameter.clubActivities, out param2))
                        {
                            club = "不明";
                        }
                        else
                        {
                            club = param2.Name;
                        }
                    }
                    else
                    {
                        listCtrl.DisableAddInfo();
                    }
                    listCtrl.AddList(num, chaFileControl.parameter.fullname, club, personality, folderAssist.lstFile[i].FullPath, folderAssist.lstFile[i].FileName, folderAssist.lstFile[i].time, false);
                    num++;
                }
            }
            stopwatch.Stop();
            Logger.Log(LogLevel.Error, $"Cards load stage 1: [{fileCount}] {stopwatch.Elapsed}");

            stopwatch.Reset();
            stopwatch.Start();
            listCtrl.Create(__instance.OnChangeSelect);
            Logger.Log(LogLevel.Error, $"Cards load stage 2: {stopwatch.Elapsed}");
            stopwatch.Stop();

            return false;
        }
    }
}
