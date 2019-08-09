using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ScriptEngine
{
    [BepInPlugin(GUID: GUID, Name: "Script Engine", Version: Version)]
    public class ScriptEngine : BaseUnityPlugin
    {
        public const string GUID = "com.bepis.bepinex.scriptengine";
        public const string Version = BepisPlugins.Metadata.PluginsVersion;

        public string ScriptDirectory => Path.Combine(Paths.PluginPath, "scripts");

        private GameObject scriptManager = new GameObject();

        private void Awake() => ReloadPlugins(false);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Delete) && Event.current.control)
            {
                ReloadPlugins();
            }
        }

        private void ReloadPlugins(bool message = false)
        {
            Destroy(scriptManager);

            scriptManager = new GameObject();

            DontDestroyOnLoad(scriptManager);

            if (Directory.Exists(ScriptDirectory))
                foreach (string path in Directory.GetFiles(ScriptDirectory, "*.dll"))
                {
                    LoadDLL(path, scriptManager);
                }

            if (message)
                Logger.Log(LogLevel.Message, "Reloaded script plugins!");
        }

        private void LoadDLL(string path, GameObject obj)
        {
            var defaultResolver = new DefaultAssemblyResolver();
            if (Directory.Exists(ScriptDirectory))
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
