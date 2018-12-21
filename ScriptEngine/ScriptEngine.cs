using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace ScriptEngine
{
    [BepInPlugin(GUID: "com.bepis.bepinex.scriptengine", Name: "Script Engine", Version: "2.0")]
    public class ScriptEngine : BaseUnityPlugin
    {
        public string ScriptDirectory => Path.Combine(Paths.PluginPath, "scripts");

        private GameObject scriptManager = new GameObject();

        void Awake()
        {
            ReloadPlugins();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Delete) && Event.current.control)
            {
                ReloadPlugins();
            }
        }

        void ReloadPlugins()
        {
            Destroy(scriptManager);

            scriptManager = new GameObject();

            DontDestroyOnLoad(scriptManager);

            foreach (string path in Directory.GetFiles(ScriptDirectory, "*.dll"))
            {
                LoadDLL(path, scriptManager);
            }

	        Logger.Log(LogLevel.Message, "Reloaded script plugins!");
        }

        private void LoadDLL(string path, GameObject obj)
        {
            var defaultResolver = new DefaultAssemblyResolver();
            defaultResolver.AddSearchDirectory(ScriptDirectory);
	        defaultResolver.AddSearchDirectory(Paths.ManagedPath);
            
            AssemblyDefinition dll = AssemblyDefinition.ReadAssembly(path, new ReaderParameters
            {
                AssemblyResolver = defaultResolver
            });

            dll.Name.Name = $"{dll.Name.Name}-{DateTime.Now.Ticks}";

            using (var ms = new MemoryStream())
            {
                dll.Write(ms);
                var assembly = Assembly.Load(ms.ToArray());

                foreach (Type t in assembly.GetTypes())
                {
                    if (typeof(BaseUnityPlugin).IsAssignableFrom(t))
                    {
                        obj.AddComponent(t);
                    }
                }
            }
        }
    }
}
