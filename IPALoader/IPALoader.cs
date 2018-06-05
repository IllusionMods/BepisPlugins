using BepInEx;
using BepInEx.Common;
using System;
using System.IO;
using System.Reflection;
using BepInEx.Bootstrap;
using IllusionPlugin;
using UnityEngine;
using BepInEx.Logging;

namespace IPALoader
{
	[BepInPlugin(GUID: "com.bepis.bepinex.ipapluginloader", Name: "IPA Plugin Loader", Version: "1.2")]
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
				
				return null;
			};
		}

		void Start()
		{
		    string managedDir = Path.Combine(Utility.ExecutingDirectory,
		        $@"{System.Diagnostics.Process.GetCurrentProcess().ProcessName}_Data\Managed");

		    if (File.Exists(Path.Combine(managedDir, "IllusionPlugin.dll")))
		        BepInEx.Logger.Log(LogLevel.Warning, "IPA has been detected to be installed! IPALoader may not function correctly!");


			if (!Directory.Exists(IPAPluginDir))
			{
				BepInEx.Logger.Log(LogLevel.Info, "No IPA plugin directory, skipping load");
				return;
			}

			if (IPAManagerObject != null)
				Destroy(IPAManagerObject);

			IPAManagerObject = new GameObject("IPA_Manager");

			DontDestroyOnLoad(IPAManagerObject);
			IPAManagerObject.SetActive(false);

			BepInEx.Logger.Log(LogLevel.Info, "Loading IPA plugins");

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
							pluginToLoad = (IPlugin) Activator.CreateInstance(t);

							Component c = Chainloader.ManagerObject.AddComponent<IPAPlugin>();

							BepInEx.Logger.Log(LogLevel.Info, $"Loaded IPA plugin [{pluginToLoad.Name}]");

							pluginToLoad = null;
						}
					}
				}
				catch (Exception ex)
				{
					BepInEx.Logger.Log(LogLevel.Error, $"Error loading IPA plugin {Path.GetFileName(path)}");
					BepInEx.Logger.Log(LogLevel.Error, ex.ToString());
				}
			}

			IPAManagerObject.SetActive(true);
		}
	}
}