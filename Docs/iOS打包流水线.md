# iOS 打包流水线（Mac mini → iPhone）

推送到 `main` 后，GitHub Actions 在 Mac mini（自托管 runner）上自动：

1. 拉取代码（含 Git LFS 图片），保留 `Library` 缓存；
2. Unity 命令行导出 Xcode 工程（Addressables 随包构建，服务器配置 `.env` 打进包里）；
3. `xcodebuild` 自动签名编译；
4. `xcrun devicectl` 安装到配对的 iPhone 并启动。

相关文件：`.github/workflows/ios-device.yml`、`scripts/ci/build-ios.sh`、`Assets/Editor/CiBuild.cs`、`Assets/Editor/EnvBundler.cs`。

**服务器配置 `.env` 自动打包**：任何方式打包（Unity 菜单 Build、命令行、流水线）都会在打包前自动把 `.env` 打进包里（流水线用 `REPGAME_CLIENT_ENV_FILE` 指定的文件，手动打包用项目根目录的 `.env`），不需要再手动复制到 Xcode 工程。
日志：每次运行的 Unity / Xcode 日志在 Actions 页面的 `ios-build-logs` 附件里，Mac mini 上在 `Builds/logs/`。

## 一次性准备（都在 Mac mini 上做）

1. **给 RepGame 仓库注册 runner**（服务器仓库的 runner 只服务 RepGameServer，个人账号下每个仓库要单独注册）
   - GitHub → linxscc/RepGame → Settings → Actions → Runners → New self-hosted runner → macOS，按页面命令下载、`./config.sh`（标签保留默认，包含 `self-hosted, macOS`）。
   - 放在和服务器 runner 不同的目录。
   - 用 `./svc.sh install && ./svc.sh start` 装成**登录用户的后台服务**（需要用户已登录的会话，Unity 和 Xcode 签名要用登录钥匙串）。
2. **Unity**：Unity Hub 安装 `6000.3.26f1` 并勾选 **iOS Build Support**；在 Hub 里登录并激活 Personal 许可证（命令行打包需要）。
   安装位置不是默认的 `/Applications/Unity/Hub/Editor/6000.3.26f1/` 时，设置仓库变量 `UNITY_PATH`。
3. **Xcode**：Xcode → Settings → Accounts 登录你的 Apple ID，会出现 “(Personal Team)”，记下它的 Team ID（10 位字母数字）。
4. **iPhone 配对**（一次）：
   - 用数据线连 Mac mini，手机上点“信任”；手机 设置 → 隐私与安全性 → **开发者模式** 打开（会重启）。
   - Xcode → Window → Devices and Simulators 选中手机，勾选 **Connect via network**，之后同一 Wi-Fi 下无需数据线。
   - 终端运行 `xcrun devicectl list devices`，记下手机那一行的 **Identifier**。
5. **服务器配置**：在 Mac mini 上放一份客户端用的 `.env`（内容同项目根目录的 `.env`：`SERVER_HOST`、`SERVER_PORT`、`SERVER_CERT_SHA256`），记下完整路径。
6. **仓库变量**：GitHub → linxscc/RepGame → Settings → Secrets and variables → Actions → **Variables**：

   | 变量 | 值 |
   |---|---|
   | `IOS_BUNDLE_ID` | 全局唯一的包名，例如 `com.linxscc.repgame`（现在工程里的模板包名不能用） |
   | `IOS_TEAM_ID` | 第 3 步的 Team ID |
   | `IOS_DEVICE_ID` | 第 4 步的设备 UDID/Identifier，多台设备（如 iPhone 和 iPad）用逗号分隔；某台不在线时跳过；全部连不上时运行仍算成功并给出警告，打好的包作为 `ios-app` 附件保留 7 天，回到局域网后重新运行即可安装 |
   | `SERVER_HOST` / `SERVER_PORT` / `SERVER_CERT_SHA256` | 服务器地址、端口、证书指纹（流水线用它们生成打进包的配置；也可以改用下一行的文件） |
| `REPGAME_CLIENT_ENV_FILE` | （可选）Mac mini 上一份 `.env` 的完整路径，设置后优先使用 |
   | `UNITY_PATH` | （可选）Unity 可执行文件路径 |

7. **签名用的钥匙串**：runner 以后台服务运行时，登录钥匙串可能是锁着的，签名会报 `errSecInternalComponent`。在仓库 Settings → Secrets and variables → Actions → **Secrets** 新建 `MAC_KEYCHAIN_PASSWORD`，值为 Mac mini 登录密码（加密保存，日志里不会显示），流水线签名前会自动解锁。
8. Mac mini 上安装 Git LFS：`brew install git-lfs && git lfs install`。

## 免费 Apple ID 的限制

- 签名 **7 天后过期**，过期后 App 打不开；推送一次代码（或在 Actions 页面手动 Run workflow）重新安装即可。
- 同一时间最多 3 个自签 App；不能用 TestFlight。
- 第一次安装后，手机 设置 → 通用 → VPN 与设备管理 → 信任你的开发者证书。

## 手动运行

Actions 页面选 “iOS build to iPhone” → Run workflow；或在 Mac mini 项目目录：

```bash
IOS_BUNDLE_ID=... IOS_TEAM_ID=... IOS_DEVICE_ID=... ENV_FILE=/path/to/.env scripts/ci/build-ios.sh
```
