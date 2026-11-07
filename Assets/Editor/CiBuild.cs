using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RepGame.EditorTools
{
    /// <summary>
    /// 命令行打包入口（Mac mini 流水线用）：
    /// Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod RepGame.EditorTools.CiBuild.BuildIOS
    /// 读取的环境变量：
    ///   IOS_BUNDLE_ID   包名（免费 Apple ID 也必须是全局唯一的，例如 com.yourname.repgame）
    ///   IOS_TEAM_ID     苹果开发者团队 ID（免费账号的 Personal Team 也有，在 Xcode → Settings → Accounts 里看）
    ///   BUILD_NUMBER    构建号（流水线传 run 编号）
    ///   ENV_FILE        要打进包里的服务器配置（.env）；由 EnvBundler 处理，不设置时用项目根目录的 .env
    ///   BUILD_OUTPUT    Xcode 工程输出目录，默认 Builds/iOS
    /// Addressables 已设置为随 Player 一起构建，不需要单独步骤。
    /// </summary>
    public static class CiBuild
    {
        public static void BuildIOS()
        {
            int code = 1;
            try
            {
                var bundleId = Env("IOS_BUNDLE_ID");
                var teamId = Env("IOS_TEAM_ID");
                if (string.IsNullOrEmpty(bundleId) || string.IsNullOrEmpty(teamId))
                    throw new Exception("缺少环境变量 IOS_BUNDLE_ID 或 IOS_TEAM_ID");

                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, bundleId);
                PlayerSettings.iOS.appleDeveloperTeamID = teamId;
                PlayerSettings.iOS.appleEnableAutomaticSigning = true;
                var buildNumber = Env("BUILD_NUMBER");
                if (!string.IsNullOrEmpty(buildNumber)) PlayerSettings.iOS.buildNumber = buildNumber;

                // 服务器配置 .env 由 EnvBundler 在打包前自动打进包（读 ENV_FILE，否则项目根目录 .env）

                var output = Env("BUILD_OUTPUT");
                if (string.IsNullOrEmpty(output)) output = "Builds/iOS";
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.iOS,
                    options = BuildOptions.None,
                });
                Debug.Log($"[CiBuild] iOS 构建结果：{report.summary.result}，错误 {report.summary.totalErrors} 个，输出 {output}");
                code = report.summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CiBuild] 构建失败：{e}");
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        private static string Env(string name) => Environment.GetEnvironmentVariable(name);
    }
}
