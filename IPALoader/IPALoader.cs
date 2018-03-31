using BepInEx;
using BepInEx.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using IllusionPlugin;
using UnityEngine;

namespace IPALoader
{
    [BepInPlugin(GUID: "com.bepis.bepinex.resourceredirector", Name: "IPA Plugin Loader", Version: "1.0")]
    public class IPALoader : BaseUnityPlugin
    {
        public static GameObject IPAManagerObject { get; private set; }

        internal static IPlugin pluginToLoad;

        public string IPAPluginDir => Path.Combine(Utility.PluginsDirectory, "IPA");

        public IPALoader()
        {
            //only required for ILMerge
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name == "IllusionPlugin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")
                    return Assembly.GetExecutingAssembly();
                else
                    return null;
            };
        }

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
                    BepInLogger.Log($"Error loading IPA plugin {Path.GetFileName(path)}");
                    BepInLogger.Log(ex.ToString());
                }
            }

            IPAManagerObject.SetActive(true);
        }
    }
}
