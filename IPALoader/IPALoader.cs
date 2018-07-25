using BepInEx;
using BepInEx.Common;
using System;
using System.IO;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using IllusionPlugin;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace IPALoader
{
	[BepInPlugin(GUID: "com.bepis.bepinex.ipapluginloader", Name: "IPA Plugin Loader", Version: "1.2")]
    [System.ComponentModel.Browsable(false)]
	public class IPALoader : BaseUnityPlugin
	{
		public static GameObject IPAManagerObject { get; private set; }

		internal static IPlugin pluginToLoad;

		public string IPAPluginDir => Path.Combine(Paths.PluginPath, "IPA");

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
		    if (File.Exists(Path.Combine(Paths.ManagedPath, "IllusionPlugin.dll")))
		        Logger.Log(LogLevel.Error | LogLevel.Message, "IPA has been detected to be installed! IPALoader may not function correctly!");
			
			if (!Directory.Exists(IPAPluginDir))
			{
				Logger.Log(LogLevel.Message, "No IPA plugin directory, skipping load");
				return;
			}

			if (IPAManagerObject != null)
				Destroy(IPAManagerObject);

			IPAManagerObject = new GameObject("IPA_Manager");

			DontDestroyOnLoad(IPAManagerObject);
			IPAManagerObject.SetActive(false);

			Logger.Log(LogLevel.Info, "Loading IPA plugins");

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

							Logger.Log(LogLevel.Info, $"Loaded IPA plugin [{pluginToLoad.Name}]");

							pluginToLoad = null;
						}
					}
				}
				catch (Exception ex)
				{
					Logger.Log(LogLevel.Error | LogLevel.Message, $"Error loading IPA plugin {Path.GetFileName(path)}");
					Logger.Log(LogLevel.Error, ex.ToString());
				}
			}

			IPAManagerObject.SetActive(true);
		}
	}
}