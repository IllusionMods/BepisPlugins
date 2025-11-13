// This is based on HC.BepInEx.ConfigurationManager.Il2Cpp.CoreCLR-18.0_beta2_20230821
// Please let me know if you're the author of this and want to be properly credited.
#if DEBUG
using BepInEx.Logging;
#endif
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepisPlugins;
using IMGUIModule.Il2Cpp.CoreCLR.Replacements;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    [BepInPlugin(GUID, Name, Version)]
    public class Patcher : BasePlugin
    {
        public const string GUID = "com.bepis.bepinex.imguimodule.Il2Cpp.CoreCLR.Plugin";
        public const string Name = "IMGUIModule.Il2Cpp.CoreCLR.Plugin";
        public const string Version = Metadata.PluginsVersion;

#if DEBUG
        public static new ManualLogSource Log;
#endif
        public static List<IDetour> Detours = new List<IDetour>();

        public override void Load()
        {
#if DEBUG
            Log = base.Log;
#endif
            PatchAll();
        }

        public static void PatchAll()
        {
            // UnityEngine.GUI.nextScrollStepTime
            // UnityEngine.GUI.scrollTroughSide
            // UnityEngine.GUI.CleanupRoots
            // UnityEngine.GUI.DoButtonGrid
            // UnityEngine.GUI.CalcMouseRects
            // UnityEngine.GUI.Slider
            // UnityEngine.GUI.ScrollerRepeatButton
            // UnityEngine.GUI.Scroller
            // UnityEngine.GUI.BeginScrollView
            // UnityEngine.GUI.EndScrollView
            // UnityEngine.GUI.ScrollTo
            // UnityEngine.GUI.ScrollTowards
            foreach (MethodInfo to in typeof(GUI).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                MethodInfo from = to.Name != nameof(GUI.DoButtonGrid)
                    ? typeof(UnityEngine.GUI).GetMethod(to.Name, to.GetParameters().Select(x => x.ParameterType).ToArray())
                    : typeof(UnityEngine.GUI).GetMethod(nameof(UnityEngine.GUI.DoButtonGrid));
                if (from != null)
                    Detours.Add(new Detour(from, to));
            }

            // --- Not Implemented ---
            // (All of these are avoided by replacing UnityEngine.GUILayout.SelectionGrid instead)
            // UnityEngine.GUIAspectSizer::.ctor(float, Il2CppReferenceArray<GUILayoutOption>)
            // UnityEngine.GUIAspectSizer.CalcHeight()
            // UnityEngine.GUIGridSizer.rows { get; }

            // UnityEngine.GUILayout.FlexibleSpace
            // UnityEngine.GUILayout.Width
            // UnityEngine.GUILayout.Height
            // UnityEngine.GUILayout.MinWidth
            // UnityEngine.GUILayout.MaxWidth
            // UnityEngine.GUILayout.MinHeight
            // UnityEngine.GUILayout.MaxHeight
            foreach (MethodInfo to in typeof(GUILayout).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                MethodInfo from = typeof(UnityEngine.GUILayout).GetMethod(to.Name, to.GetParameters().Select(x => x.ParameterType).ToArray());
                if (from != null)
                    Detours.Add(new Detour(from, to));
            }

            // UnityEngine.GUILayoutGroup.PeekNext
            // UnityEngine.GUILayoutGroup.GetLast
            foreach (MethodInfo to in typeof(GUILayoutGroup).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                MethodInfo from = typeof(UnityEngine.GUILayoutGroup).GetMethod(to.Name, to.GetParameters().Select(x => x.ParameterType).ToArray());
                if (from != null)
                    Detours.Add(new Detour(from, to));
            }

            // UnityEngine.GUIStateObjects.QueryStateObject
            foreach (MethodInfo to in typeof(GUIStateObjects).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                MethodInfo from = typeof(UnityEngine.GUIStateObjects).GetMethod(to.Name, to.GetParameters().Select(x => x.ParameterType).ToArray());
                if (from != null)
                    Detours.Add(new Detour(from, to));
            }

            // --- Not Implemented ---
            // UnityEngine.GUIUtility.CleanupRoots()
            // UnityEngine.ObjectGUIState.Destroy()
            // UnityEngine.ScrollViewState.ScrollTowards(Rect, float)
            // UnityEngine.ScrollViewState.ScrollNeeded(Rect)

            LayoutedWindowFix.ApplyIfNeeded(Detours);
        }

        public static void Free(bool undo = false)
        {
            foreach (IDetour detour in Detours)
            {
                if (undo)
                    detour.Undo();
                detour.Free();
            }
            Detours.Clear();
        }
    }
}
