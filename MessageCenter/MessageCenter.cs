using System.ComponentModel;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Logger = BepInEx.Logger;

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
        
	    private void Awake()
	    {
		    Logger.EntryLogged += (level, log) =>
		    {
		        if (Enabled.Value && (level & LogLevel.Message) != LogLevel.None)
		        {
		            if (showCounter == 0)
		                TotalShowingLog = string.Empty;

		            showCounter = 600;
		            TotalShowingLog = $"{log}\r\n{TotalShowingLog}";
		        }
		    };
	    }

	    private void OnGUI()
	    {
			if (showCounter != 0)
			{
				showCounter--;

				Color color = Color.white;

				if (showCounter < 100)
					color.a = (float)showCounter / 100;

				GUI.Label(new Rect(40, 20, 600, 160), TotalShowingLog, new GUIStyle
				{
					alignment = TextAnchor.UpperLeft,
					fontSize = 26,
					normal = new GUIStyleState
					{
						textColor = color
					}
				});
			}
	    }
    }
}
