using System;
using System.ComponentModel;
using BepisPlugins;
using BepInEx;
using BepInEx.Logging;

namespace ConfigurationManager
{
    [BepInPlugin("com.bepis.bepinex.commonsettings", "BepInEx settings", Metadata.PluginsVersion)]
    [Advanced(true)]
    public class BepInExSettings : BaseUnityPlugin
    {
        private const string LogFiltersGroup = "Log filtering";

        [DisplayName("Enable Unity message logging")]
        [DefaultValue(false)]
        [Description("Changes take effect after game restart.")]
        [Category(LogFiltersGroup)]
        public static bool LogUnity
        {
            get => Boolean.Parse(BepInEx.Config.GetEntry("chainloader-log-unity-messages", "false", "BepInEx"));
            set => BepInEx.Config.SetEntry("chainloader-log-unity-messages", value.ToString(), "BepInEx");
        }

        [DisplayName("Show system console")]
        [DefaultValue(false)]
        [Description("Changes take effect after game restart.")]
        public static bool ShowConsole
        {
            get => Boolean.Parse(BepInEx.Config.GetEntry("console", "false", "BepInEx"));
            set => BepInEx.Config.SetEntry("console", value.ToString(), "BepInEx");
        }

        [Browsable(true)]
        [DisplayName("Enable DEBUG logging")]
        [DefaultValue(false)]
        [Category(LogFiltersGroup)]
        public static bool LogDebug
        {
            get => GetDebugFlag(LogLevel.Debug);
            set => SetDebugFlag(value, LogLevel.Debug);
        }

        [Browsable(true)]
        [DisplayName("Enable INFO logging")]
        [DefaultValue(false)]
        [Category(LogFiltersGroup)]
        public static bool LogInfo
        {
            get => GetDebugFlag(LogLevel.Info);
            set => SetDebugFlag(value, LogLevel.Info);
        }

        [Browsable(true)]
        [DisplayName("Enable MESSAGE logging")]
        [DefaultValue(true)]
        [Category(LogFiltersGroup)]
        public static bool LogMessage
        {
            get => GetDebugFlag(LogLevel.Message);
            set => SetDebugFlag(value, LogLevel.Message);
        }

        private static void SetDebugFlag(bool value, LogLevel level)
        {
            if (value)
                Logger.CurrentLogger.DisplayedLevels |= level;
            else
                Logger.CurrentLogger.DisplayedLevels &= ~level;

            BepInEx.Config.SetEntry("logger-displayed-levels", Logger.CurrentLogger.DisplayedLevels.ToString(), "BepInEx");
        }

        private static bool GetDebugFlag(LogLevel level)
        {
            return (Logger.CurrentLogger.DisplayedLevels & level) == level;
        }
    }
}