// This is based on HC.BepInEx.ConfigurationManager.Il2Cpp.CoreCLR-18.0_beta2_20230821
// Please let me know if you're the author of this and want to be properly credited.
#if DEBUG
using BepInEx.Logging;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Preloader.Core.Patching;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    [PatcherPluginInfo(GUID, Name, Version)]
    public class Patcher : BasePatcher
    {
        public const string GUID = "com.bepis.bepinex.imguimodule.Il2Cpp.CoreCLR.Patcher";
        public const string Name = "IMGUIModule.Il2Cpp.CoreCLR.Patcher";
        public const string Version = "1.0";


        [TargetType("UnityEngine.IMGUIModule.dll", "UnityEngine.GUILayout")]
        public void PatchAssembly(TypeDefinition type)
        {
            Console.WriteLine("asd");
            var m = type.Methods.Single(x=>x.Name == "DoWindow");
            Console.WriteLine("asd " + m);
            var ilp = m.Body.GetILProcessor();
            ilp.Clear();
            ilp.Append(Instruction.Create(OpCodes.Ldarg_1));
            ilp.Append(Instruction.Create(OpCodes.Ret));
        }
    }
}
