using System.ComponentModel;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace MessageCenter
{
    [BepInPlugin("com.bepis.messagecenter", "Message Center", "1.0")]
    public class MessageCenter : BaseUnityPlugin
    {
        [DisplayName("Show messages in UI")]
        [Description("Allows plugins to show pop-up messages")]
        [Advanced(true)]
        public ConfigWrapper<bool> Enabled { get; }

        public MessageCenter()
        {
            Enabled = new ConfigWrapper<bool>("enabled", this, true);
        }

        private int showCounter = 0;
        private string TotalShowingLog = string.Empty;

        protected void Awake()
        {
            BepInEx.Logger.EntryLogged += (level, log) =>
            {
                if(Enabled.Value && (level & LogLevel.Message) != LogLevel.None)
                {
                    if(showCounter == 0)
                        TotalShowingLog = string.Empty;

                    showCounter = 600;
                    TotalShowingLog = $"{log}\r\n{TotalShowingLog}";
                }
            };
        }

        protected void OnGUI()
        {
            if(showCounter != 0)
            {
                showCounter--;

                Color color = Color.white;
                Color color2 = Color.black;

                if(showCounter < 100)
                {
                    var a = (float)showCounter / 100;
                    color.a = a;
                    color2.a = a;
                }

                ShadowAndOutline.DrawOutline(new Rect(40, 20, 600, 160), TotalShowingLog, new GUIStyle
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 20,
                }, color2, color, 3f);
            }
        }
    }
}
