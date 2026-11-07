using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RepGame.Service
{
    /// <summary>
    /// 资源加载入口：美术/界面资源都走 Addressables（AssetBundle），不使用 Resources 文件夹（JSON 配置仍在 StreamingAssets）。
    /// 启动时由 UIBootstrap 调用 <see cref="Preload"/> 异步初始化并预加载常驻资源（如 ChessUISkin），
    /// 之后界面代码可以用 <see cref="Get{T}"/> 同步取到已加载的资源。
    /// 目前 bundle 随包（Local 路径）；以后改为 CDN 只需在 Addressables Profile 里切换 Remote 路径。
    /// </summary>
    public static class AssetLoader
    {
        /// <summary>启动时预加载的常驻资源地址（Addressables address）。</summary>
        private static readonly string[] PreloadAddresses = { "ChessUISkin" };

        private static readonly Dictionary<string, AsyncOperationHandle> handles = new Dictionary<string, AsyncOperationHandle>();
        private static bool initialized;
        private static bool quitHooked;

        /// <summary>预加载已完成（成功或失败都算完成）。</summary>
        public static bool Ready { get; private set; }

        /// <summary>初始化 Addressables 并预加载常驻资源；在协程里 yield 它。</summary>
        public static IEnumerator Preload()
        {
            HookQuit();
            if (!initialized)
            {
                var init = Addressables.InitializeAsync(false);
                yield return init;
                if (init.Status != AsyncOperationStatus.Succeeded)
                    Debug.LogError($"AssetLoader: Addressables 初始化失败 {init.OperationException}");
                init.Release();
                initialized = true;
            }

            foreach (var address in PreloadAddresses)
            {
                if (handles.ContainsKey(address)) continue;
                var handle = Addressables.LoadAssetAsync<Object>(address);
                yield return handle;
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    handles[address] = handle;
                }
                else
                {
                    Debug.LogError($"AssetLoader: 预加载 {address} 失败 {handle.OperationException}");
                    handle.Release();
                }
            }
            Ready = true;
        }

        /// <summary>
        /// 同步取资源：优先返回已预加载的；没预加载时阻塞加载（WaitForCompletion）并警告。
        /// 失败返回 null。
        /// </summary>
        public static T Get<T>(string address) where T : Object
        {
            if (handles.TryGetValue(address, out var cached) && cached.IsValid())
            {
                var asset = cached.Result as T;
                if (asset != null) return asset;
            }

            HookQuit();
            Debug.LogWarning($"AssetLoader: {address} 未预加载就被访问，改为同步加载（会卡一下）");
            var handle = Addressables.LoadAssetAsync<T>(address);
            var result = handle.WaitForCompletion();
            if (handle.Status != AsyncOperationStatus.Succeeded || result == null)
            {
                Debug.LogError($"AssetLoader: 加载 {address} 失败 {handle.OperationException}");
                handle.Release();
                return null;
            }
            handles[address] = handle;
            return result;
        }

        /// <summary>释放所有已加载的资源句柄。</summary>
        public static void ReleaseAll()
        {
            foreach (var h in handles.Values)
            {
                if (h.IsValid()) Addressables.Release(h);
            }
            handles.Clear();
            Ready = false;
        }

        private static void HookQuit()
        {
            if (quitHooked) return;
            quitHooked = true;
            Application.quitting += ReleaseAll;
        }

        // 关闭 Domain Reload 进入 Play Mode 时清掉上一次的静态状态
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            handles.Clear();
            initialized = false;
            Ready = false;
            if (quitHooked) Application.quitting -= ReleaseAll;
            quitHooked = false;
        }
    }
}
