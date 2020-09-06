using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
#if AI || HS2
using AIChara;
#endif

namespace Sideloader
{
    /// <summary>
    /// Contains data about the loaded blacklist
    /// </summary>
    public class Blacklist
    {
        /// <summary>
        /// Full contents of the xml
        /// </summary>
        public readonly XDocument BlacklistDocument;
        /// <summary>
        /// Dictionary of blacklisted GUID and the associated blacklist info
        /// </summary>
        public Dictionary<string, BlacklistInfo> BlacklistItems = new Dictionary<string, BlacklistInfo>();

        internal Blacklist(string filePath)
        {
            if (!File.Exists(filePath)) return;

            BlacklistDocument = XDocument.Load(filePath);
            if (BlacklistDocument.Element("sideloaderBlacklist") == null) return;

            foreach (var element in BlacklistDocument.Element("sideloaderBlacklist").Elements("blacklist"))
            {
                var blacklistItem = new BlacklistInfo(element);
                if (!blacklistItem.GUID.IsNullOrEmpty())
                    BlacklistItems[blacklistItem.GUID] = blacklistItem;
            }
        }
    }

    /// <summary>
    /// Contains data about blacklisted mods
    /// </summary>
    public class BlacklistInfo
    {
        /// <summary>
        /// GUID of the mod
        /// </summary>
        public string GUID;
        /// <summary>
        /// Reason for blacklisting the mod, optional
        /// </summary>
        public string Reason;

        internal BlacklistInfo(XElement element)
        {
            try
            {
                GUID = element.Attribute("guid")?.Value;
                Reason = element.Attribute("reason")?.Value;
            }
            catch (Exception ex)
            {
                Sideloader.Logger.LogError($"Could not load blacklist data, skipping line.");
                Sideloader.Logger.LogError(ex);
            }
        }
    }
}
