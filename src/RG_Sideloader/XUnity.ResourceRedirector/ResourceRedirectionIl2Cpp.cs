using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using XUnity.Common.Logging;
using XUnity.Common.Utilities;
using static XUnity.ResourceRedirector.ResourceRedirection;

namespace XUnity.ResourceRedirector
{
    /// <summary>
    /// Entrypoint to the resource redirection API.
    /// </summary>
    public static class ResourceRedirectionIl2Cpp
    {
        private static readonly PropertyInfo PropertiesResourcesEmptyProperty = typeof(AssetBundleHelper).Assembly.GetType($"{nameof(XUnity)}.{nameof(ResourceRedirector)}.{nameof(Properties)}.Resources").GetProperty("empty", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo _redirectionMissingAssetBundlesToEmptyField = typeof(ResourceRedirection).GetField("_redirectionMissingAssetBundlesToEmpty", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo _redirectionMissingAssetBundlesToEmptyAsyncField = typeof(ResourceRedirection).GetField("_redirectionMissingAssetBundlesToEmptyAsync", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        private static byte[] Properties_Resources_empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PropertiesResourcesEmptyProperty.GetValue(null) as byte[];
        }

        private static Action<AssetBundleLoadingContext> _redirectionMissingAssetBundlesToEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _redirectionMissingAssetBundlesToEmptyField.GetValue(null) as Action<AssetBundleLoadingContext>;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _redirectionMissingAssetBundlesToEmptyField.SetValue(null, value);
        }

        private static Action<AsyncAssetBundleLoadingContext> _redirectionMissingAssetBundlesToEmptyAsync
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _redirectionMissingAssetBundlesToEmptyAsyncField.GetValue(null) as Action<AsyncAssetBundleLoadingContext>;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _redirectionMissingAssetBundlesToEmptyAsyncField.SetValue(null, value);
        }

        /// <summary>
        /// Creates an asset bundle hook that redirects asset bundles loads to an empty
        /// asset bundle if the file that is being loaded does not exist.
        /// </summary>
        /// <param name="priority">Priority of the hook.</param>
        public static void EnableRedirectMissingAssetBundlesToEmptyAssetBundle(int priority)
        {
            if (_redirectionMissingAssetBundlesToEmpty == null && _redirectionMissingAssetBundlesToEmptyAsync == null)
            {
                _redirectionMissingAssetBundlesToEmpty = ctx => HandleMissingBundle(ctx, SetBundle);
                _redirectionMissingAssetBundlesToEmptyAsync = ctx => HandleMissingBundle(ctx, SetRequest);

                RegisterAssetBundleLoadingHook(priority, _redirectionMissingAssetBundlesToEmpty);
                RegisterAsyncAssetBundleLoadingHook(priority, _redirectionMissingAssetBundlesToEmptyAsync);

                // define base callback
                void HandleMissingBundle<TContext>(TContext context, Action<TContext, byte[]> changeBundle)
                   where TContext : IAssetBundleLoadingContext
                {
                    if (context.Parameters.LoadType == AssetBundleLoadType.LoadFromFile
                       && !File.Exists(context.Parameters.Path))
                    {
                        var buffer = Properties_Resources_empty;
                        CabHelper.RandomizeCab(buffer);

                        changeBundle(context, buffer);

                        context.Complete(
                           skipRemainingPrefixes: true,
                           skipOriginalCall: true);

                        XuaLogger.ResourceRedirector.Warn("Tried to load non-existing asset bundle: " + context.Parameters.Path);
                    }
                }

                // synchronous specific code
                void SetBundle(AssetBundleLoadingContext context, byte[] assetBundleData)
                {
                    var bundle = AssetBundle.LoadFromMemory(assetBundleData);
                    context.Bundle = bundle;
                }

                // asynchronous specific code
                void SetRequest(AsyncAssetBundleLoadingContext context, byte[] assetBundleData)
                {
                    var request = AssetBundle.LoadFromMemoryAsync(assetBundleData);
                    context.Request = request;
                }
            }
        }
    }
}
