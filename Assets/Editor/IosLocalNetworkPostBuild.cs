#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace RepGame.EditorTools
{
    /// <summary>
    /// iOS 14+ 访问局域网（服务器在局域网 Mac mini 上）需要「本地网络」权限：
    /// 打包时往 Info.plist 写入 NSLocalNetworkUsageDescription，系统才会弹出带说明的授权框。
    /// 玩家拒绝后所有局域网连接都会超时，需要到 设置 → 隐私与安全性 → 本地网络 里打开。
    /// </summary>
    public static class IosLocalNetworkPostBuild
    {
        private const string Reason = "连接局域网内的游戏服务器进行对局";

        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("NSLocalNetworkUsageDescription", Reason);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
