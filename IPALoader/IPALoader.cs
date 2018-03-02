using BepInEx;
using BepInEx.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using IllusionInjector;
using IllusionPlugin;
using UnityEngine;

namespace IPALoader
{
    public class IPALoader : BaseUnityPlugin
    {
        public override string ID => "com.bepis.bepinex.resourceredirector";

        public override string Name => "IPA Plugin Loader";

        public override Version Version => new Version("1.0");

        public static GameObject IPAManagerObject { get; private set; }

        internal static IPlugin pluginToLoad;

        public string IPAPluginDir => Path.Combine(Utility.PluginsDirectory, "IPA");

        void Start()
        {
            if (!Directory.Exists(IPAPluginDir))
            {
                BepInLogger.Log("No IPA plugin directory, skipping load");
                return;
            }

            if (IPAManagerObject != null)
                Destroy(IPAManagerObject);

            IPAManagerObject = new GameObject("IPA_Manager");

            DontDestroyOnLoad(IPAManagerObject);
            IPAManagerObject.SetActive(false);

            BepInLogger.Log("Loading IPA plugins");

            foreach (string path in Directory.GetFiles(IPAPluginDir, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFile(path);

                    foreach (Type t in assembly.GetTypes())
                    {
                        if (t.IsAbstract || t.IsInterface)
                            continue;

                        if (t.Name == "CompositePlugin")
                            continue;

                        if (typeof(IPlugin).IsAssignableFrom(t))
                        {
                            pluginToLoad = (IPlugin)Activator.CreateInstance(t);

                            Component c = Chainloader.ManagerObject.AddComponent<IPAPlugin>();

                            BepInLogger.Log($"Loaded IPA plugin [{pluginToLoad.Name}]");

                            pluginToLoad = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    BepInLogger.Log($"Error loading IPA plugin {Path.GetFileName(path)} : {ex.ToString()}");
                }
            }

            IPAManagerObject.SetActive(true);
        }
    }
}
