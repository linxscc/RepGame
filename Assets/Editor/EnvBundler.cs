using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RepGame.EditorTools
{
    /// <summary>
    /// 每次打包（菜单 Build、命令行、流水线都一样）自动把服务器配置 .env 打进包里，
    /// 不用再手动往 Xcode 工程里复制：
    ///   来源：环境变量 ENV_FILE 指向的文件（流水线用），否则项目根目录的 .env；
    ///   目标：StreamingAssets/server.env（运行时由 EnvConfig 读取；Unity 不打包以点开头的文件，所以换名）。
    /// 打包结束后删除这份临时文件（已在 .gitignore 里，不会提交）。
    /// </summary>
    public sealed class EnvBundler : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private static string Target => Path.Combine(Application.streamingAssetsPath, "server.env");

        private static string Source()
        {
            var fromEnv = Environment.GetEnvironmentVariable("ENV_FILE");
            if (!string.IsNullOrEmpty(fromEnv)) return fromEnv;
            return Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", ".env");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            var source = Source();
            if (!File.Exists(source))
            {
                Debug.LogWarning($"[EnvBundler] 没找到 {source}，包里不含服务器配置，运行时会用默认地址 127.0.0.1");
                return;
            }
            Directory.CreateDirectory(Application.streamingAssetsPath);
            File.Copy(source, Target, true);
            AssetDatabase.Refresh();
            Debug.Log($"[EnvBundler] 已把 {source} 打进包（StreamingAssets/server.env）");
        }

        public void OnPostprocessBuild(BuildReport report) => Cleanup();

        [InitializeOnLoadMethod]
        private static void CleanupLeftover() => EditorApplication.delayCall += Cleanup; // 上次打包失败时残留的临时文件

        private static void Cleanup()
        {
            if (File.Exists(Target)) File.Delete(Target);
            if (File.Exists(Target + ".meta")) File.Delete(Target + ".meta");
        }
    }
}
