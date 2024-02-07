using BepInEx.IL2CPP;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using XUnity.Common.Constants;
using XUnity.Common.Harmony;
using Stream = Il2CppSystem.IO.Stream;
using Type   = Il2CppSystem.Type;
using Object = UnityEngine.Object;

namespace XUnity.ResourceRedirector.Hooks;

internal static class ResourceRedirectorIl2CppFix
{
    private enum HarmonyParameter
    {
        __instance,
        __result,
        __state,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr RuntimeInvokeDetourDelegate(IntPtr obj);

    private static MethodInfo LoadFromFileAsyncMethod;
    private static MethodInfo LoadFromFileMethod;
    private static MethodInfo LoadFromMemoryAsyncMethod;
    private static MethodInfo LoadFromMemoryMethod;
    private static MethodInfo LoadFromStreamAsyncMethod;
    private static MethodInfo LoadFromStreamMethod;
    private static MethodInfo LoadAssetMethod;
    private static MethodInfo LoadAssetAsyncMethod;
    private static MethodInfo LoadAssetWithSubAssetsMethod;
    private static MethodInfo LoadAssetWithSubAssetsAsyncMethod;

    private static MethodInfo AssetBundleRequest_asset_Hook_Prefix;
    private static MethodInfo AssetBundleRequest_asset_Hook_Postfix;
    private static HarmonyParameter[] AssetBundleRequest_asset_Hook_Prefix_ParameterTypes;
    private static HarmonyParameter[] AssetBundleRequest_asset_Hook_Postfix_ParameterTypes;
    private static RuntimeInvokeDetourDelegate NativeDelegate_get_asset_Public_Object_0;

    public unsafe static void PatchAll()
    {
        var selfType = typeof(ResourceRedirectorIl2CppFix);

        var harmony = new Harmony($"{selfType.FullName}-{Guid.NewGuid()}");

        // Module:    UnityEngine.AssetBundleModule.dll
        // NameSpace: UnityEngine
        // Class:     AssetBundle

        // static AssetBundleCreateRequest LoadFromFileAsync(string path)
        var original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromFileAsync), new[] { typeof(string) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromFileAsync1)));

        // static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromFileAsync), new[] { typeof(string), typeof(uint) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromFileAsync2)));

        // static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc, ulong offset)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromFileAsync), new[] { typeof(string), typeof(uint), typeof(ulong) });
        LoadFromFileAsyncMethod = AccessTools.DeclaredMethod(typeof(AssetBundle), "LoadFromFileAsync_Internal", new[] { typeof(string), typeof(uint), typeof(ulong) });
        if (LoadFromFileAsyncMethod == null)
            LoadFromFileAsyncMethod = original;
        else if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromFileAsync3)));

        // static AssetBundle LoadFromFile(string path)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromFile), new[] { typeof(string) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromFile1)));

        // static AssetBundle LoadFromFile(string path, uint crc)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromFile), new[] { typeof(string), typeof(uint) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromFile2)));

        // static AssetBundle LoadFromFile(string path, uint crc, ulong offset)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromFile), new[] { typeof(string), typeof(uint), typeof(ulong) });
        LoadFromFileMethod = AccessTools.DeclaredMethod(typeof(AssetBundle), "LoadFromFile_Internal", new[] { typeof(string), typeof(uint), typeof(ulong) });
        if (LoadFromFileMethod == null)
            LoadFromFileMethod = original;
        else if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromFile3)));

        // static AssetBundleCreateRequest LoadFromMemoryAsync(Il2CppStructArray<byte> binary)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromMemoryAsync), new[] { typeof(Il2CppStructArray<byte>) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromMemoryAsync1)));

        // static AssetBundleCreateRequest LoadFromMemoryAsync(Il2CppStructArray<byte> binary, uint crc)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromMemoryAsync), new[] { typeof(Il2CppStructArray<byte>), typeof(uint) });
        LoadFromMemoryAsyncMethod = AccessTools.DeclaredMethod(typeof(AssetBundle), "LoadFromMemoryAsync_Internal", new[] { typeof(Il2CppStructArray<byte>), typeof(uint) });
        if (LoadFromMemoryAsyncMethod == null)
            LoadFromMemoryAsyncMethod = original;
        else if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromMemoryAsync2)));

        // static AssetBundle LoadFromMemory(Il2CppStructArray<byte> binary)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromMemory), new[] { typeof(Il2CppStructArray<byte>) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromMemory1)));

        // static AssetBundle LoadFromMemory(Il2CppStructArray<byte> binary, uint crc)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromMemory), new[] { typeof(Il2CppStructArray<byte>), typeof(uint) });
        LoadFromMemoryMethod = AccessTools.DeclaredMethod(typeof(AssetBundle), "LoadFromMemory_Internal", new[] { typeof(Il2CppStructArray<byte>), typeof(uint) });
        if (LoadFromMemoryMethod == null)
            LoadFromMemoryMethod = original;
        else if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromMemory2)));

        // static AssetBundleCreateRequest LoadFromStreamAsync(Il2CppSystem.IO.Stream stream)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromStreamAsync), new[] { typeof(Stream) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromStreamAsync1)));

        // static AssetBundleCreateRequest LoadFromStreamAsync(Il2CppSystem.IO.Stream stream, uint crc)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromStreamAsync), new[] { typeof(Stream), typeof(uint) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromStreamAsync2)));

        // static AssetBundleCreateRequest LoadFromStreamAsync(Il2CppSystem.IO.Stream stream, uint crc, uint managedReadBufferSize)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromStreamAsync), new[] { typeof(Stream), typeof(uint), typeof(uint) });
        LoadFromStreamAsyncMethod = AccessTools.DeclaredMethod(typeof(AssetBundle), "LoadFromStreamAsyncInternal", new[] { typeof(Stream), typeof(uint), typeof(uint) });
        if (LoadFromStreamAsyncMethod == null)
            LoadFromStreamAsyncMethod = original;
        else if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromStreamAsync3)));

        // static AssetBundle LoadFromStream(Il2CppSystem.IO.Stream stream)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromStream), new[] { typeof(Stream) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromStream1)));

        // static AssetBundle LoadFromStream(Il2CppSystem.IO.Stream stream, uint crc)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromStream), new[] { typeof(Stream), typeof(uint) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromStream2)));

        // static AssetBundle LoadFromStream(Il2CppSystem.IO.Stream stream, uint crc, uint managedReadBufferSize)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadFromStream), new[] { typeof(Stream), typeof(uint), typeof(uint) });
        LoadFromStreamMethod = AccessTools.DeclaredMethod(typeof(AssetBundle), "LoadFromStreamInternal", new[] { typeof(Stream), typeof(uint), typeof(uint) });
        if (LoadFromStreamMethod == null)
            LoadFromStreamMethod = original;
        else if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadFromStream3)));

        ParameterInfo[] parameters;

        var methods =
            AccessTools.GetDeclaredMethods(typeof(AssetBundle))
                .Where(method =>
                    method.Name == nameof(AssetBundle.Load) &&
                    (parameters = method.GetParameters()).Length == 1 &&
                    parameters[0].GetType() == typeof(string)).ToArray();

        // UnityEngine.Object Load(string name)
        original = methods.FirstOrDefault(method => !method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_Load1)));

        /*
        // UnityEngine.Object Load<T>(string name)
        original = methods.FirstOrDefault(method => method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_Load2)));
        */

        // Il2CppReferenceArray<UnityEngine.Object> LoadAll()
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadAll), new System.Type[0]);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAll)));

        methods =
            AccessTools.GetDeclaredMethods(typeof(AssetBundle))
                .Where(method =>
                    method.Name == nameof(AssetBundle.LoadAsset) &&
                    (parameters = method.GetParameters()).Length == 1 &&
                    parameters[0].GetType() == typeof(string)).ToArray();

        // UnityEngine.Object LoadAsset(string name)
        original = methods.FirstOrDefault(method => !method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAsset1)));

        /*
        // T LoadAsset<T>(string name) where T : UnityEngine.Object
        original = methods.FirstOrDefault(method => method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAsset2)));
        */

        // UnityEngine.Object LoadAsset(string name, Il2CppSystem.Type type)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), new[] { typeof(string), typeof(Type) });
        LoadAssetMethod = AccessTools.DeclaredMethod(typeof(AssetBundle), "LoadAsset_Internal", new[] { typeof(string), typeof(Type) });
        if (LoadAssetMethod == null)
            LoadAssetMethod = original;
        else if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAsset3)));

        methods =
            AccessTools.GetDeclaredMethods(typeof(AssetBundle))
                .Where(method =>
                    method.Name == nameof(AssetBundle.LoadAssetAsync) &&
                    (parameters = method.GetParameters()).Length == 1 &&
                    parameters[0].GetType() == typeof(string)).ToArray();

        // AssetBundleRequest LoadAssetAsync(string name)
        original = methods.FirstOrDefault(method => !method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAssetAsync1)));

        /*
        // AssetBundleRequest LoadAssetAsync<T>(string name)
        original = methods.FirstOrDefault(method => method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAssetAsync2)));
        */

        // AssetBundleRequest LoadAssetAsync(string name, Il2CppSystem.Type type)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadAssetAsync), new[] { typeof(string), typeof(Type) });
        LoadAssetAsyncMethod = AccessTools.DeclaredMethod(typeof(AssetBundle), "LoadAssetAsync_Internal", new[] { typeof(string), typeof(Type) });
        if (LoadAssetAsyncMethod == null)
            LoadAssetAsyncMethod = original;
        else if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAssetAsync3)));

        methods =
            AccessTools.GetDeclaredMethods(typeof(AssetBundle))
                .Where(method =>
                    method.Name == nameof(AssetBundle.LoadAssetWithSubAssets) &&
                    (parameters = method.GetParameters()).Length == 1 &&
                    parameters[0].GetType() == typeof(string)).ToArray();

        // Il2CppReferenceArray<UnityEngine.Object> LoadAssetWithSubAssets(string name)
        original = methods.FirstOrDefault(method => !method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAssetWithSubAssets1)));

        /*
        // Il2CppReferenceArray<T> LoadAssetWithSubAssets<T>(string name) where T : UnityEngine.Object
        original = methods.FirstOrDefault(method => method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAssetWithSubAssets2)));
        */

        // Il2CppReferenceArray<UnityEngine.Object> LoadAssetWithSubAssets(string name, Il2CppSystem.Type type)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadAssetWithSubAssets), new[] { typeof(string), typeof(Type) });
        LoadAssetWithSubAssetsMethod = AccessTools.DeclaredMethod(typeof(AssetBundle), "LoadAssetWithSubAssets_Internal", new[] { typeof(string), typeof(Type) });
        if (LoadAssetWithSubAssetsMethod == null)
            LoadAssetWithSubAssetsMethod = original;
        else if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAssetWithSubAssets3)));

        methods =
            AccessTools.GetDeclaredMethods(typeof(AssetBundle))
                .Where(method =>
                    method.Name == nameof(AssetBundle.LoadAssetWithSubAssetsAsync) &&
                    (parameters = method.GetParameters()).Length == 1 &&
                    parameters[0].GetType() == typeof(string)).ToArray();

        // AssetBundleRequest LoadAssetWithSubAssetsAsync(string name)
        original = methods.FirstOrDefault(method => !method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAssetWithSubAssetsAsync1)));

        /*
        // AssetBundleRequest LoadAssetWithSubAssetsAsync<T>(string name)
        original = methods.FirstOrDefault(method => method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAssetWithSubAssetsAsync2)));
        */

        // AssetBundleRequest LoadAssetWithSubAssetsAsync(string name, Il2CppSystem.Type type)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadAssetWithSubAssetsAsync), new[] { typeof(string), typeof(Type) });
        LoadAssetWithSubAssetsAsyncMethod = AccessTools.DeclaredMethod(typeof(AssetBundle), "LoadAssetWithSubAssetsAsync_Internal", new[] { typeof(string), typeof(Type) });
        if (LoadAssetWithSubAssetsAsyncMethod == null)
            LoadAssetWithSubAssetsAsyncMethod = original;
        else if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAssetWithSubAssetsAsync3)));

        methods =
            AccessTools.GetDeclaredMethods(typeof(AssetBundle))
                .Where(method =>
                    method.Name == nameof(AssetBundle.LoadAllAssets) &&
                    method.GetParameters().Length == 0).ToArray();

        // Il2CppReferenceArray<UnityEngine.Object> LoadAllAssets()
        original = methods.FirstOrDefault(method => !method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAllAssets1)));

        /*
        // Il2CppReferenceArray<T> LoadAllAssets<T>() where T : UnityEngine.Object
        original = methods.FirstOrDefault(method => method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAllAssets2)));
        */

        // Il2CppReferenceArray<UnityEngine.Object> LoadAllAssets(Il2CppSystem.Type type)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadAllAssets), new[] { typeof(Type) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAllAssets3)));

        // Failed it...
        /*
        methods =
            AccessTools.GetDeclaredMethods(typeof(AssetBundle))
                .Where(method =>
                    method.Name == nameof(AssetBundle.LoadAllAssetsAsync) &&
                    method.GetParameters().Length == 0).ToArray();

        // AssetBundleRequest LoadAllAssetsAsync()
        original = methods.FirstOrDefault(method => !method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAllAssetsAsync1)));

        // AssetBundleRequest LoadAllAssetsAsync<T>()
        original = methods.FirstOrDefault(method => method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAllAssetsAsync2)));
        */

        // AssetBundleRequest LoadAllAssetsAsync(Il2CppSystem.Type type)
        original = AccessTools.DeclaredMethod(typeof(AssetBundle), nameof(AssetBundle.LoadAllAssetsAsync), new[] { typeof(Type) });
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(AssetBundle_LoadAllAssetsAsync3)));

        // Module:    UnityEngine.CoreModule.dll
        // NameSpace: UnityEngine
        // Class:     Resources

        methods =
            AccessTools.GetDeclaredMethods(typeof(Resources))
                .Where(method =>
                    method.Name == nameof(Resources.Load) &&
                    (parameters = method.GetParameters()).Length == 1 &&
                    parameters[0].GetType() == typeof(string)).ToArray();

        // static UnityEngine.Object Load(string path)
        original = methods.FirstOrDefault(method => !method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(Resources_Load1)));

        /*
        // static T Load<T>(string path) where T : UnityEngine.Object
        original = methods.FirstOrDefault(method => method.IsGenericMethod);
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(Resources_Load2)));
        */

        // static T GetBuiltinResource<T>(string path) where T : UnityEngine.Object
        original = AccessTools.GetDeclaredMethods(typeof(Resources))
                .FirstOrDefault(method =>
                    method.IsGenericMethod &&
                    method.Name == nameof(Resources.GetBuiltinResource) &&
                    (parameters = method.GetParameters()).Length == 1 &&
                    parameters[0].GetType() == typeof(string));
        if (IsNativeMethod(original))
            harmony.Patch(original, new HarmonyMethod(selfType, nameof(Resources_GetBuiltinResource)));

        const string typeNamePrefix = $"{nameof(XUnity)}.{nameof(ResourceRedirector)}.{nameof(Hooks)}.";

        var assembly = typeof(ResourceLoadType).Assembly;
        System.Type type;

        do
        {
            // Module:    UnityEngine.AssetBundleModule.dll
            // NameSpace: UnityEngine
            // Class:     AssetBundleRequest

            // UnityEngine.Object asset { get; }
            original = AccessTools.DeclaredPropertyGetter(typeof(AssetBundleRequest), nameof(AssetBundleRequest.asset));
            if (original == null)
                break;
            var methodField = UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(original);
            if (methodField == null)
                break;

            // Module:    XUnity.ResourceRedirector.dll
            // NameSpace: XUnity.ResourceRedirector.Hooks
            // Class:     ResourceAndAssetHooks

            type = assembly.GetType(typeNamePrefix + "ResourceAndAssetHooks");
            if (type == null)
                break;

            // static readonly Type[] GeneralHooks
            var field = type.GetField("GeneralHooks");
            if (field == null)
                break;
            var generalHooks = field.GetValue(null) as System.Type[];
            if (generalHooks == null)
                break;
            field.SetValue(null, generalHooks.Where(x => x.Name != "AssetBundleRequest_asset_Hook").ToArray());

            // Module:    XUnity.ResourceRedirector.dll
            // NameSpace: XUnity.ResourceRedirector.Hooks
            // Class:     AssetBundleRequest_asset_Hook

            type = assembly.GetType(typeNamePrefix + "AssetBundleRequest_asset_Hook");
            if (type == null)
                break;
            AssetBundleRequest_asset_Hook_Prefix = AccessTools.DeclaredMethod(type, "Prefix");
            if (AssetBundleRequest_asset_Hook_Prefix == null)
                break;
            AssetBundleRequest_asset_Hook_Postfix = AccessTools.DeclaredMethod(type, "Postfix");
            if (AssetBundleRequest_asset_Hook_Postfix == null)
                break;
            var parameterNames = typeof(HarmonyParameter).GetEnumNames();
            AssetBundleRequest_asset_Hook_Prefix_ParameterTypes = AssetBundleRequest_asset_Hook_Prefix.GetParameters().Select(x =>
                (HarmonyParameter)Array.IndexOf(parameterNames, x.Name)).ToArray();
            AssetBundleRequest_asset_Hook_Postfix_ParameterTypes = AssetBundleRequest_asset_Hook_Postfix.GetParameters().Select(x =>
                (HarmonyParameter)Array.IndexOf(parameterNames, x.Name)).ToArray();

            // Module:    UnityEngine.AssetBundleModule.dll
            // NameSpace: UnityEngine
            // Class:     AssetBundleRequest

            // Get the native MethodInfo struct for the target method
            var nativeMethodInfo = UnhollowerBaseLib.Runtime.UnityVersionHandler.Wrap((UnhollowerBaseLib.Runtime.Il2CppMethodInfo*)(IntPtr)methodField.GetValue(null));

            // Create a trampoline from the original target method
            var methodPointer = nativeMethodInfo.MethodPointer;
            var trampolinePointer = DetourGenerator.CreateTrampolineFromFunction(methodPointer, out _, out _);
            NativeDelegate_get_asset_Public_Object_0 = Marshal.GetDelegateForFunctionPointer<RuntimeInvokeDetourDelegate>(trampolinePointer);

            var replacement = AccessTools.DeclaredMethod(selfType, nameof(AssetBundleRequest_asset_Hook));
            var detourPointer = Marshal.GetFunctionPointerForDelegate(replacement.CreateDelegate(typeof(RuntimeInvokeDetourDelegate)));
            new NativeDetour(methodPointer, detourPointer);
        } while (false);

        // Module:    XUnity.ResourceRedirector.dll
        // NameSpace: XUnity.ResourceRedirector.Hooks
        // Class:     Resources_GetBuiltinResource_Old_Hook

        type = assembly.GetType(typeNamePrefix + "Resources_GetBuiltinResource_Old_Hook");
        if (type != null)
        {
            // static MethodBase TargetMethod(object instance)
            original = AccessTools.DeclaredMethod(type, "TargetMethod", new[] { typeof(object) });
            if (original != null)
            {
                var prefix = new HarmonyMethod(selfType, nameof(Resources_GetBuiltinResource_Old_Hook_TargetMethod));
                harmony.Patch(original, prefix);
            }
        }

        // Module:    XUnity.ResourceRedirector.dll
        // NameSpace: XUnity.ResourceRedirector.Hooks
        // Class:     Resources_GetBuiltinResource_New_Hook

        type = assembly.GetType(typeNamePrefix + "Resources_GetBuiltinResource_New_Hook");
        if (type != null)
        {
            // static bool Prepare(object instance)
            original = AccessTools.DeclaredMethod(type, "Prepare", new[] { typeof(object) });
            if (original != null)
            {
                var prefix = new HarmonyMethod(selfType, nameof(Resources_GetBuiltinResource_New_Hook_Prepare));
                harmony.Patch(original, prefix);
            }
        }

        // Module:    XUnity.ResourceRedirector.dll
        // NameSpace: XUnity.ResourceRedirector.Hooks
        // Class:     AsyncOperation_Finalize_Hook

        type = assembly.GetType(typeNamePrefix + "AsyncOperation_Finalize_Hook");
        if (type != null)
        {
            // static MethodBase TargetMethod(object instance)
            original = AccessTools.DeclaredMethod(type, "TargetMethod", new[] { typeof(object) });
            if (original != null)
            {
                var prefix = new HarmonyMethod(selfType, nameof(AsyncOperation_Finalize_Hook_TargetMethod));
                harmony.Patch(original, prefix);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNativeMethod(MethodInfo method) =>
        method != null && UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method) != null;

    // Module:    UnityEngine.AssetBundleModule.dll
    // NameSpace: UnityEngine
    // Class:     AssetBundle

    // static AssetBundleCreateRequest LoadFromFileAsync(string path)
    private static bool AssetBundle_LoadFromFileAsync1(ref AssetBundleCreateRequest __result, string path)
    {
        __result = LoadFromFileAsyncMethod.Invoke(null, new object[] { path, 0U, 0UL }) as AssetBundleCreateRequest;
        return false;
    }

    // static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc)
    private static bool AssetBundle_LoadFromFileAsync2(ref AssetBundleCreateRequest __result, string path, uint crc)
    {
        __result = LoadFromFileAsyncMethod.Invoke(null, new object[] { path, crc, 0UL }) as AssetBundleCreateRequest;
        return false;
    }

    // static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc, ulong offset)
    private static bool AssetBundle_LoadFromFileAsync3(ref AssetBundleCreateRequest __result, string path, uint crc, ulong offset)
    {
        __result = LoadFromFileAsyncMethod.Invoke(null, new object[] { path, crc, offset }) as AssetBundleCreateRequest;
        return false;
    }

    // static AssetBundle LoadFromFile(string path)
    private static bool AssetBundle_LoadFromFile1(ref AssetBundle __result, string path)
    {
        __result = LoadFromFileMethod.Invoke(null, new object[] { path, 0U, 0UL }) as AssetBundle;
        return false;
    }

    // static AssetBundle LoadFromFile(string path, uint crc)
    private static bool AssetBundle_LoadFromFile2(ref AssetBundle __result, string path, uint crc)
    {
        __result = LoadFromFileMethod.Invoke(null, new object[] { path, crc, 0UL }) as AssetBundle;
        return false;
    }

    // static AssetBundle LoadFromFile(string path, uint crc, ulong offset)
    private static bool AssetBundle_LoadFromFile3(ref AssetBundle __result, string path, uint crc, ulong offset)
    {
        __result = LoadFromFileMethod.Invoke(null, new object[] { path, crc, offset }) as AssetBundle;
        return false;
    }

    // static AssetBundleCreateRequest LoadFromMemoryAsync(Il2CppStructArray<byte> binary)
    private static bool AssetBundle_LoadFromMemoryAsync1(ref AssetBundleCreateRequest __result, Il2CppStructArray<byte> binary)
    {
        __result = LoadFromMemoryAsyncMethod.Invoke(null, new object[] { binary, 0U }) as AssetBundleCreateRequest;
        return false;
    }

    // static AssetBundleCreateRequest LoadFromMemoryAsync(Il2CppStructArray<byte> binary, uint crc)
    private static bool AssetBundle_LoadFromMemoryAsync2(ref AssetBundleCreateRequest __result, Il2CppStructArray<byte> binary, uint crc)
    {
        __result = LoadFromMemoryAsyncMethod.Invoke(null, new object[] { binary, crc }) as AssetBundleCreateRequest;
        return false;
    }

    // static AssetBundle LoadFromMemory(Il2CppStructArray<byte> binary)
    private static bool AssetBundle_LoadFromMemory1(ref AssetBundle __result, Il2CppStructArray<byte> binary)
    {
        __result = LoadFromMemoryMethod.Invoke(null, new object[] { binary, 0U }) as AssetBundle;
        return false;
    }

    // static AssetBundle LoadFromMemory(Il2CppStructArray<byte> binary, uint crc)
    private static bool AssetBundle_LoadFromMemory2(ref AssetBundle __result, Il2CppStructArray<byte> binary, uint crc)
    {
        __result = LoadFromMemoryMethod.Invoke(null, new object[] { binary, crc }) as AssetBundle;
        return false;
    }

    // static AssetBundleCreateRequest LoadFromStreamAsync(Il2CppSystem.IO.Stream stream)
    private static bool AssetBundle_LoadFromStreamAsync1(ref AssetBundleCreateRequest __result, Stream stream)
    {
        AssetBundle.ValidateLoadFromStream(stream);
        __result = LoadFromStreamAsyncMethod.Invoke(null, new object[] { stream, 0U, 0U }) as AssetBundleCreateRequest;
        return false;
    }

    // static AssetBundleCreateRequest LoadFromStreamAsync(Il2CppSystem.IO.Stream stream, uint crc)
    private static bool AssetBundle_LoadFromStreamAsync2(ref AssetBundleCreateRequest __result, Stream stream, uint crc)
    {
        AssetBundle.ValidateLoadFromStream(stream);
        __result = LoadFromStreamAsyncMethod.Invoke(null, new object[] { stream, crc, 0U }) as AssetBundleCreateRequest;
        return false;
    }

    // static AssetBundleCreateRequest LoadFromStreamAsync(Il2CppSystem.IO.Stream stream, uint crc, uint managedReadBufferSize)
    private static bool AssetBundle_LoadFromStreamAsync3(ref AssetBundleCreateRequest __result, Stream stream, uint crc, uint managedReadBufferSize)
    {
        AssetBundle.ValidateLoadFromStream(stream);
        __result = LoadFromStreamAsyncMethod.Invoke(null, new object[] { stream, crc, managedReadBufferSize }) as AssetBundleCreateRequest;
        return false;
    }

    // static AssetBundle LoadFromStream(Il2CppSystem.IO.Stream stream)
    private static bool AssetBundle_LoadFromStream1(ref AssetBundle __result, Stream stream)
    {
        AssetBundle.ValidateLoadFromStream(stream);
        __result = LoadFromStreamMethod.Invoke(null, new object[] { stream, 0U, 0U }) as AssetBundle;
        return false;
    }

    // static AssetBundle LoadFromStream(Il2CppSystem.IO.Stream stream, uint crc)
    private static bool AssetBundle_LoadFromStream2(ref AssetBundle __result, Stream stream, uint crc)
    {
        AssetBundle.ValidateLoadFromStream(stream);
        __result = LoadFromStreamMethod.Invoke(null, new object[] { stream, crc, 0U }) as AssetBundle;
        return false;
    }

    // static AssetBundle LoadFromStream(Il2CppSystem.IO.Stream stream, uint crc, uint managedReadBufferSize)
    private static bool AssetBundle_LoadFromStream3(ref AssetBundle __result, Stream stream, uint crc, uint managedReadBufferSize)
    {
        AssetBundle.ValidateLoadFromStream(stream);
        __result = LoadFromStreamMethod.Invoke(null, new object[] { stream, crc, managedReadBufferSize }) as AssetBundle;
        return false;
    }

    // UnityEngine.Object Load(string name)
    private static bool AssetBundle_Load1(ref Object __result, AssetBundle __instance, string name)
    {
        __result = __instance.Load(name, null);
        return false;
    }

    /*
    // UnityEngine.Object Load<T>(string name)
    private static bool AssetBundle_Load2<T>(ref Object __result, AssetBundle __instance, string name)
    {
        __result = __instance.Load(name, Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<T>()));
        return false;
    }
    */

    // Il2CppReferenceArray<UnityEngine.Object> LoadAll()
    private static bool AssetBundle_LoadAll(ref Il2CppReferenceArray<Object> __result, AssetBundle __instance)
    {
        __result = __instance.LoadAll(null);
        return false;
    }

    // UnityEngine.Object LoadAsset(string name)
    private static bool AssetBundle_LoadAsset1(ref Object __result, AssetBundle __instance, string name)
    {
        __result = __instance.LoadAsset(name, Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<Object>()));
        return false;
    }

    /*
    // T LoadAsset<T>(string name) where T : UnityEngine.Object
    private static bool AssetBundle_LoadAsset2<T>(ref T __result, AssetBundle __instance, string name) where T : Object
    {
        __result = (T)(object)__instance.LoadAsset(name, Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<Object>()));
        return false;
    }
    */

    // UnityEngine.Object LoadAsset(string name, Il2CppSystem.Type type)
    private static bool AssetBundle_LoadAsset3(ref Object __result, AssetBundle __instance, string name, Type type)
    {
        ValidateLoadAsset(name, type);
        __result = LoadAssetMethod.Invoke(__instance, new object[] { name, type }) as Object;
        return false;
    }

    // AssetBundleRequest LoadAssetAsync(string name)
    private static bool AssetBundle_LoadAssetAsync1(ref AssetBundleRequest __result, AssetBundle __instance, string name)
    {
        __result = __instance.LoadAssetAsync(name, Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<Object>()));
        return false;
    }

    /*
    // AssetBundleRequest LoadAssetAsync<T>(string name)
    private static bool AssetBundle_LoadAssetAsync2<T>(ref AssetBundleRequest __result, AssetBundle __instance, string name)
    {
        __result = __instance.LoadAssetAsync(name, Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<T>()));
        return false;
    }
    */

    // AssetBundleRequest LoadAssetAsync(string name, Il2CppSystem.Type type)
    private static bool AssetBundle_LoadAssetAsync3(ref AssetBundleRequest __result, AssetBundle __instance, string name, Type type)
    {
        ValidateLoadAsset(name, type);
        __result = LoadAssetAsyncMethod.Invoke(__instance, new object[] { name, type }) as AssetBundleRequest;
        return false;
    }

    // Il2CppReferenceArray<UnityEngine.Object> LoadAssetWithSubAssets(string name)
    private static bool AssetBundle_LoadAssetWithSubAssets1(ref Il2CppReferenceArray<Object> __result, AssetBundle __instance, string name)
    {
        __result = __instance.LoadAssetWithSubAssets(name, Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<Object>()));
        return false;
    }

    /*
    // Il2CppReferenceArray<T> LoadAssetWithSubAssets<T>(string name) where T : UnityEngine.Object
    private static bool AssetBundle_LoadAssetWithSubAssets2<T>(ref Il2CppReferenceArray<T> __result, AssetBundle __instance, string name) where T : Object
    {
        __result = ConvertObjects<T>(__instance.LoadAssetWithSubAssets(name, Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<T>())));
        return false;
    }
    */

    // Il2CppReferenceArray<UnityEngine.Object> LoadAssetWithSubAssets(string name, Il2CppSystem.Type type)
    private static bool AssetBundle_LoadAssetWithSubAssets3(ref Il2CppReferenceArray<Object> __result, AssetBundle __instance, string name, Type type)
    {
        ValidateLoadAsset(name, type);
        __result = LoadAssetWithSubAssetsMethod.Invoke(__instance, new object[] { name, type }) as Il2CppReferenceArray<Object>;
        return false;
    }

    // AssetBundleRequest LoadAssetWithSubAssetsAsync(string name)
    private static bool AssetBundle_LoadAssetWithSubAssetsAsync1(ref AssetBundleRequest __result, AssetBundle __instance, string name)
    {
        __result = __instance.LoadAssetWithSubAssetsAsync(name, Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<Object>()));
        return false;
    }

    /*
    // AssetBundleRequest LoadAssetWithSubAssetsAsync<T>(string name)
    private static bool AssetBundle_LoadAssetWithSubAssetsAsync2<T>(ref AssetBundleRequest __result, AssetBundle __instance, string name)
    {
        __result = __instance.LoadAssetWithSubAssetsAsync(name, Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<T>()));
        return false;
    }
    */

    // AssetBundleRequest LoadAssetWithSubAssetsAsync(string name, Il2CppSystem.Type type)
    private static bool AssetBundle_LoadAssetWithSubAssetsAsync3(ref AssetBundleRequest __result, AssetBundle __instance, string name, Type type)
    {
        ValidateLoadAsset(name, type);
        __result = LoadAssetWithSubAssetsAsyncMethod.Invoke(__instance, new object[] { name, type }) as AssetBundleRequest;
        return false;
    }

    private static void ValidateLoadAsset(string name, Type type)
    {
        if (name == null)
            throw new NullReferenceException("The input asset name cannot be null.");
        if (name.Length == 0)
            throw new ArgumentException("The input asset name cannot be empty.");
        if (type == null)
            throw new NullReferenceException("The input type cannot be null.");
    }

    // Il2CppReferenceArray<UnityEngine.Object> LoadAllAssets()
    public static bool AssetBundle_LoadAllAssets1(ref Il2CppReferenceArray<Object> __result, AssetBundle __instance)
    {
        __result = __instance.LoadAllAssets(Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<Object>()));
        return false;
    }

    /*
    // Il2CppReferenceArray<T> LoadAllAssets<T>() where T : UnityEngine.Object
    public static bool AssetBundle_LoadAllAssets2<T>(ref Il2CppReferenceArray<T> __result, AssetBundle __instance) where T : Object
    {
        __result = ConvertObjects<T>(__instance.LoadAllAssets(Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<T>())));
        return false;
    }
    */

    // Il2CppReferenceArray<UnityEngine.Object> LoadAllAssets(Il2CppSystem.Type type)
    public static bool AssetBundle_LoadAllAssets3(ref Il2CppReferenceArray<Object> __result, AssetBundle __instance, Type type)
    {
        if (type == null)
            throw new NullReferenceException("The input type cannot be null.");
        __result = LoadAssetWithSubAssetsMethod.Invoke(__instance, new object[] { "", type }) as Il2CppReferenceArray<Object>;
        return false;
    }

    /*
    // AssetBundleRequest LoadAllAssetsAsync()
    public static bool AssetBundle_LoadAllAssetsAsync1(ref AssetBundleRequest __result, AssetBundle __instance)
    {
        __result = __instance.LoadAllAssetsAsync(Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<Object>()));
        return false;
    }

    // AssetBundleRequest LoadAllAssetsAsync<T>()
    public static bool AssetBundle_LoadAllAssetsAsync2<T>(ref AssetBundleRequest __result, AssetBundle __instance)
    {
        __result = __instance.LoadAllAssetsAsync(Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<T>()));
        return false;
    }
    */

    // AssetBundleRequest LoadAllAssetsAsync(Il2CppSystem.Type type)
    public static bool AssetBundle_LoadAllAssetsAsync3(ref AssetBundleRequest __result, AssetBundle __instance, Type type)
    {
        if (type == null)
            throw new NullReferenceException("The input type cannot be null.");
        __result = LoadAssetWithSubAssetsAsyncMethod.Invoke(__instance, new object[] { "", type }) as AssetBundleRequest;
        return false;
    }

    private static Il2CppReferenceArray<T> ConvertObjects<T>(Il2CppReferenceArray<Object> rawObjects) where T : Object
    {
        Il2CppReferenceArray<T> array = null;
        if (rawObjects != null)
        {
            array = new(rawObjects.Length);
            for (int i = 0; i < array.Length; i++)
                array[i] = (T)(object)rawObjects[i];
        }
        return array;
    }

    // Module:    UnityEngine.CoreModule.dll
    // NameSpace: UnityEngine
    // Class:     Resources

    // static UnityEngine.Object Load(string path)
    private static bool Resources_Load1(ref Object __result, string path)
    {
        __result = Resources.Load(path, Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<Object>()));
        return false;
    }

    /*
    // static T Load<T>(string path) where T : UnityEngine.Object
    private static bool Resources_Load2<T>(ref T __result, string path) where T : Object
    {
        __result = (T)(object)Resources.Load(path, Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<Object>()));
        return false;
    }
    */

    // static T GetBuiltinResource<T>(string path) where T : UnityEngine.Object
    private static bool Resources_GetBuiltinResource<T>(ref T __result, string path) where T : Object
    {
        __result = (T)(object)Resources.GetBuiltinResource(Type.GetTypeFromHandle(RuntimeReflectionHelper.GetRuntimeTypeHandle<T>()), path);
        return false;
    }

    // Module:    XUnity.ResourceRedirector.dll
    // NameSpace: XUnity.ResourceRedirector.Hooks
    // Class:     AssetBundleRequest_asset_Hook

    private static IntPtr AssetBundleRequest_asset_Hook(IntPtr pointer)
    {
        Object __result = null;
        if (pointer != default)
        {
            var __instance = new AssetBundleRequest(pointer);
            var parameters = MakePrefixParameters(__instance);
            if ((bool)AssetBundleRequest_asset_Hook_Prefix.Invoke(null, parameters))
            {
                object __state = GetStateFromPrefixParameters(parameters);
                __result = AssetBundleRequest_get_asset(__instance);
                parameters = MakePostfixParameters(__instance, __result, __state);
                AssetBundleRequest_asset_Hook_Postfix.Invoke(null, parameters);
                GetResultFromPostfixParameters(parameters, ref __result);
            }
            else
            {
                __result = GetResultFromPrefixParameters(parameters);
            }
        }
        return __result?.Pointer ?? default;

        static object[] MakePrefixParameters(object __instance)
        {
            var parameters = new object[AssetBundleRequest_asset_Hook_Prefix_ParameterTypes.Length];
            for (var i = 0; i < parameters.Length; i++)
                parameters[i] =
                    AssetBundleRequest_asset_Hook_Prefix_ParameterTypes[i] == HarmonyParameter.__instance
                        ? __instance
                        : null;
            return parameters;
        }

        static object GetStateFromPrefixParameters(object[] parameters)
        {
            var index = Array.IndexOf(AssetBundleRequest_asset_Hook_Prefix_ParameterTypes, HarmonyParameter.__state);
            return index != -1 ? parameters[index] : null;
        }

        static Object GetResultFromPrefixParameters(object[] parameters)
        {
            var index = Array.IndexOf(AssetBundleRequest_asset_Hook_Prefix_ParameterTypes, HarmonyParameter.__result);
            return index != -1 ? parameters[index] as Object : null;
        }

        static object[] MakePostfixParameters(object __instance, object __result, object __state)
        {
            var parameters = new object[AssetBundleRequest_asset_Hook_Postfix_ParameterTypes.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                switch (AssetBundleRequest_asset_Hook_Postfix_ParameterTypes[i])
                {
                    case HarmonyParameter.__instance:
                        parameters[i] = __instance;
                        break;
                    case HarmonyParameter.__result:
                        parameters[i] = __result;
                        break;
                    case HarmonyParameter.__state:
                        parameters[i] = __state;
                        break;
                }
            }
            return parameters;
        }

        static void GetResultFromPostfixParameters(object[] parameters, ref Object __result)
        {
            var index = Array.IndexOf(AssetBundleRequest_asset_Hook_Postfix_ParameterTypes, HarmonyParameter.__result);
            if (index != -1)
                __result = parameters[index] as Object;
        }
    }

    private static Object AssetBundleRequest_get_asset(AssetBundleRequest __instance)
    {
        try
        {
            var pointer = NativeDelegate_get_asset_Public_Object_0(IL2CPP.Il2CppObjectBaseToPtrNotNull(__instance));

            // validate the pointer
            var nativeClass = Marshal.ReadIntPtr(pointer);

            return new Object(pointer);
        }
        catch
        {
            return null;
        }
    }

    // Module:    XUnity.ResourceRedirector.dll
    // NameSpace: XUnity.ResourceRedirector.Hooks
    // Class:     Resources_GetBuiltinResource_Old_Hook

    // static MethodBase TargetMethod(object instance)
    private static bool Resources_GetBuiltinResource_Old_Hook_TargetMethod(ref MethodBase __result)
    {
        __result = AccessToolsShim.Method(UnityTypes.Resources?.ClrType, nameof(Resources.GetBuiltinResource), typeof(Type), typeof(string));
        return false;
    }

    // Module:    XUnity.ResourceRedirector.dll
    // NameSpace: XUnity.ResourceRedirector.Hooks
    // Class:     Resources_GetBuiltinResource_New_Hook

    // static bool Prepare(object instance)
    private static bool Resources_GetBuiltinResource_New_Hook_Prepare(ref bool __result)
    {
        __result = AccessToolsShim.Method(UnityTypes.Resources?.ClrType, nameof(Resources.GetBuiltinResource), typeof(Type), typeof(string)) == null;
        return false;
    }

    // Module:    XUnity.ResourceRedirector.dll
    // NameSpace: XUnity.ResourceRedirector.Hooks
    // Class:     AsyncOperation_Finalize_Hook

    // static MethodBase TargetMethod(object instance)
    private static bool AsyncOperation_Finalize_Hook_TargetMethod(ref MethodBase __result)
    {
        __result = UnityTypes.AsyncOperation?.ClrType.GetMethod(nameof(AsyncOperation.Finalize), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return false;
    }
}
