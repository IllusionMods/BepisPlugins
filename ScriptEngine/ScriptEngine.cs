using BepInEx;
using BepInEx.Common;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ScriptEngine
{
    public class ScriptEngine : BaseUnityPlugin
    {
        public override string ID => "com.bepis.bepinex.scriptengine";

        public override string Name => "Script Engine";

        public override Version Version => new Version("1.0");

        public string ScriptDirectory => Path.Combine(Utility.PluginsDirectory, "scripts");

        private GameObject scriptManager = new GameObject();

        void Awake()
        {
            ReloadPlugins();
        }

        void Update()
        {
            if (Event.current.control && Event.current.keyCode == KeyCode.Delete)
            {
                ReloadPlugins();
            }
        }

        void ReloadPlugins()
        {
            Destroy(scriptManager);

            scriptManager = new GameObject();

            foreach (string path in Directory.GetFiles(ScriptDirectory, "*.dll"))
            {
                LoadDLL(path, scriptManager);
            }
        }

        private void LoadDLL(string path, GameObject obj)
        {
            var defaultResolver = new DefaultAssemblyResolver();
            defaultResolver.AddSearchDirectory(ScriptDirectory);
            defaultResolver.AddSearchDirectory(Path.Combine(Utility.ExecutingDirectory, @"KoikatuTrial_Data\Managed"));
            
            AssemblyDefinition dll = AssemblyDefinition.ReadAssembly(path, new ReaderParameters
            {
                AssemblyResolver = defaultResolver
            });

            dll.Name.Name = $"{dll.Name.Name}-{DateTime.Now.Ticks.ToString()}";

            using (var ms = new MemoryStream())
            {
                dll.Write(ms);
                var assembly = Assembly.Load(ms.ToArray());

                foreach (Type t in assembly.GetTypes())
                {
                    if (t.BaseType == typeof(BaseScript))
                    {
                        obj.AddComponent(t);
                    }
                }
            }
        }
    }
}
