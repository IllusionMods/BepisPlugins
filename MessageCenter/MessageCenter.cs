using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace BepisPlugins
{
    [BepInPlugin(GUID, "Message Center", Version)]
    public class MessageCenter : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.messagecenter";
        public const string Version = Metadata.PluginsVersion;

        private static readonly List<LogEntry> _shownLogLines = new List<LogEntry>();

        private static float _showCounter;
        private static string _shownLogText = string.Empty;

        static MessageCenter()
        {
            Enabled = new ConfigWrapper<bool>("enabled", GUID, true);
            BepInEx.Logger.EntryLogged += OnEntryLogged;
        }

        [DisplayName("Show messages in UI")]
        [Description("Allow plugins to show pop-up messages")]
        [Advanced(true)]
        public static ConfigWrapper<bool> Enabled { get; }
        
        private static void OnEntryLogged(LogLevel level, object log)
        {
            if ((level & LogLevel.Message) != LogLevel.None && Enabled.Value)
            {
                if (_showCounter <= 0)
                    _shownLogLines.Clear();

                _showCounter = Mathf.Max(_showCounter, 7f);

                var logText = log.ToString();

                var logEntry = _shownLogLines.FirstOrDefault(x => x.Text.Equals(logText, StringComparison.Ordinal));
                if (logEntry == null)
                {
                    logEntry = new LogEntry(logText);
                    _shownLogLines.Add(logEntry);

                    _showCounter = _showCounter + 0.8f;
                }

                logEntry.Count++;

                var logLines = _shownLogLines.Select(x => x.Count > 1 ? $"{x.Count}x {x.Text}" : x.Text).ToArray();
                _shownLogText = string.Join("\r\n", logLines);
            }
        }

        private void Update()
        {
            if (_showCounter > 0)
                _showCounter -= Time.deltaTime;
        }

        private void OnGUI()
        {
            if (_showCounter <= 0) return;

            var textColor = Color.black;
            var outlineColor = Color.white;

            if (_showCounter <= 1)
            {
                textColor.a = _showCounter;
                outlineColor.a = _showCounter;
            }

            ShadowAndOutline.DrawOutline(new Rect(40, 20, Screen.width - 80, 160), _shownLogText, new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 20
            }, textColor, outlineColor, 3);
        }

        private sealed class LogEntry
        {
            public LogEntry(string text)
            {
                Text = text;
            }

            public string Text { get; }
            public int Count { get; set; }
        }
    }
}