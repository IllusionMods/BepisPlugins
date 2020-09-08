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
        public Dictionary<string, BlacklistInfo> BlacklistInfos = new Dictionary<string, BlacklistInfo>();

        internal Blacklist(string filePath)
        {
            if (!File.Exists(filePath)) return;

            BlacklistDocument = XDocument.Load(filePath);
            if (BlacklistDocument.Element("sideloaderBlacklist") == null) return;

            foreach (var element in BlacklistDocument.Element("sideloaderBlacklist").Elements("blacklist"))
            {
                var blacklistInfo = new BlacklistInfo(element);
                if (!blacklistInfo.GUID.IsNullOrEmpty())
                    BlacklistInfos[blacklistInfo.GUID] = blacklistInfo;
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
        /// <summary>
        /// List of individually blacklisted items, if any
        /// </summary>
        public List<BlacklistItemInfo> BlacklistItemInfos = new List<BlacklistItemInfo>();
        /// <summary>
        /// List of individually blacklisted studio items, if any
        /// </summary>
        public List<BlacklistItemInfo> BlacklistStudioItemInfos = new List<BlacklistItemInfo>();

        internal BlacklistInfo(XElement element)
        {
            try
            {
                GUID = element.Attribute("guid")?.Value;
                Reason = element.Attribute("reason")?.Value;

                foreach (var blacklistItemElement in element.Elements("item"))
                {
                    var blacklistItemInfo = new BlacklistItemInfo(blacklistItemElement);
                    if (blacklistItemInfo.ID == -1)
                        Sideloader.Logger.LogWarning($"Invalid blacklist item ID for GUID {GUID}, skipping");
                    else
                        BlacklistItemInfos.Add(blacklistItemInfo);
                }

                foreach (var blacklistItemElement in element.Elements("studioItem"))
                {
                    var blacklistItemInfo = new BlacklistItemInfo(blacklistItemElement);
                    if (blacklistItemInfo.ID == -1)
                        Sideloader.Logger.LogWarning($"Invalid blacklist item ID for GUID {GUID}, skipping");
                    else
                        BlacklistStudioItemInfos.Add(blacklistItemInfo);
                }
            }
            catch (Exception ex)
            {
                Sideloader.Logger.LogError($"Could not load blacklist data, skipping entry.");
                Sideloader.Logger.LogError(ex);
            }
        }
    }

    /// <summary>
    /// Contains data about individually blacklisted items
    /// </summary>
    public class BlacklistItemInfo
    {
        /// <summary>
        /// Category number
        /// </summary>
        public int Category;
        /// <summary>
        /// Group number
        /// </summary>
        public int Group;
        /// <summary>
        /// ID number
        /// </summary>
        public int ID;

        internal BlacklistItemInfo(XElement element)
        {
            int.TryParse(element.Attribute("category")?.Value, out int category);
            Category = category;
            int.TryParse(element.Attribute("group")?.Value, out int group);
            Group = group;
            if (int.TryParse(element.Attribute("id")?.Value, out int id))
                ID = id;
            else
                ID = -1;
        }
    }
}
