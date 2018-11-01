using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace MessageCenter
{
    [BepInPlugin("com.bepis.messagecenter", "Message Center", "1.2")]
    public class MessageCenter : BaseUnityPlugin
    {
        private readonly List<LogEntry> _shownLogLines = new List<LogEntry>();

        private float _showCounter;
        private string _shownLogText = string.Empty;

        public MessageCenter()
        {
            Enabled = new ConfigWrapper<bool>("enabled", this, true);
        }

        [DisplayName("Show messages in UI")]
        [Description("Allow plugins to show pop-up messages")]
        [Advanced(true)]
        public ConfigWrapper<bool> Enabled { get; }

        private void Awake()
        {
            BepInEx.Logger.EntryLogged += OnEntryLogged;
        }

        private void OnEntryLogged(LogLevel level, object log)
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

        private void OnGUI()
        {
            if (_showCounter > 0)
            {
                _showCounter -= Time.deltaTime;

                var textColor = Color.white;
                var outlineColor = Color.black;

                if (_showCounter <= 1)
                {
                    textColor.a = _showCounter;
                    outlineColor.a = _showCounter;
                }

                ShadowAndOutline.DrawOutline(new Rect(40, 20, 600, 160), _shownLogText, new GUIStyle
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 20
                }, outlineColor, textColor, 3f);
            }
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