namespace BepisPlugins
{
    internal static class Constants
    {
#if AI
        internal const string StudioProcessName = "StudioNEOV2";
        internal const string GameProcessName = "AI-Syoujyo";
#elif EC
        internal const string GameProcessName = "EmotionCreators";
#elif HS
        internal const string StudioProcessName = "StudioNEO_64";
        internal const string StudioProcessName32bit = "StudioNEO_32";
        internal const string GameProcessName = "HoneySelect_64";
        internal const string GameProcessName32bit = "HoneySelect_32";
        internal const string BattleArenaProcessName = "BattleArena_64";
        internal const string BattleArenaProcessName32bit = "BattleArena_32";
#elif HS2
        internal const string StudioProcessName = "StudioNEOV2";
        internal const string GameProcessName = "HoneySelect2";
        internal const string VRProcessName = "HoneySelect2VR";
#elif KK
        internal const string StudioProcessName = "CharaStudio";
        internal const string GameProcessName = "Koikatu";
        internal const string GameProcessNameSteam = "Koikatsu Party";
        internal const string VRProcessName = "KoikatuVR";
        internal const string VRProcessNameSteam = "Koikatsu Party VR";
#elif PH
        internal const string GameProcessName = "PlayHome64bit";
        internal const string GameProcessName32bit = "PlayHome32bit";
        internal const string StudioProcessName = "PlayHomeStudio64bit";
        internal const string StudioProcessName32bit = "PlayHomeStudio32bit";
#endif
    }
}
