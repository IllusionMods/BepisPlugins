using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;

namespace CardCacher
{
    [BepInPlugin(GUID: "com.bepis.bepinex.cardcacher", Name: "Card Cacher", Version: "1.0")]
    public class CardCacher : BaseUnityPlugin
    {
        public FileSystemWatcher FemaleCharaWatcher { get; set; }

        public CardCacher()
        {
            Hooks.InstallHooks();
        }

        public void Start()
        {
            FemaleCharaWatcher  = new FileSystemWatcher(@"M:\koikatu\UserData\chara\female");
            
            FemaleCharaWatcher.Changed += EventHappened;
            FemaleCharaWatcher.Renamed += EventHappened;
            FemaleCharaWatcher.Created += EventHappened;
            FemaleCharaWatcher.Deleted += EventHappened;

            FemaleCharaWatcher.EnableRaisingEvents = true;
            FemaleCharaWatcher.IncludeSubdirectories = true;
            FemaleCharaWatcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.FileName;
        }

        public void EventHappened(object sender, FileSystemEventArgs args)
        {
            Logger.Log(LogLevel.Message, $"Changed [{args.ChangeType}]: {args.Name} : {args.FullPath}");
        }
    }
}
