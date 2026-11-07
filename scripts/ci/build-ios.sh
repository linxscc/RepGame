#!/usr/bin/env bash
# Mac mini 上的 iOS 打包 + 安装到配对 iPhone。
# 流水线（.github/workflows/ios-device.yml）调用它，也可以在 Mac mini 上手动运行：
#   UNITY_PATH=... IOS_BUNDLE_ID=... IOS_TEAM_ID=... IOS_DEVICE_ID=... ENV_FILE=... scripts/ci/build-ios.sh
#
# 环境变量：
#   UNITY_PATH      Unity 可执行文件，默认 /Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity
#   IOS_BUNDLE_ID   包名（全局唯一）
#   IOS_TEAM_ID     开发者团队 ID（免费 Apple ID 的 Personal Team 也可以）
#   IOS_DEVICE_ID   目标设备标识（UDID 或 xcrun devicectl list devices 里的 Identifier），多台用逗号分隔；为空则只打包不安装
#   ENV_FILE        打进包里的服务器配置（.env），默认使用项目根目录的 .env；都没有时用 SERVER_HOST/SERVER_PORT/SERVER_CERT_SHA256 生成
#   BUILD_NUMBER    构建号，默认当前时间
set -euo pipefail

cd "$(dirname "$0")/../.."
PROJECT_DIR="$(pwd)"

UNITY_PATH="${UNITY_PATH:-/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity}"
ENV_FILE="${ENV_FILE:-$PROJECT_DIR/.env}"
BUILD_NUMBER="${BUILD_NUMBER:-$(date +%Y%m%d%H%M)}"
BUILD_OUTPUT="$PROJECT_DIR/Builds/iOS"
DERIVED="$PROJECT_DIR/Builds/DerivedData"
LOG_DIR="$PROJECT_DIR/Builds/logs"
mkdir -p "$LOG_DIR"

: "${IOS_BUNDLE_ID:?需要设置 IOS_BUNDLE_ID}"
: "${IOS_TEAM_ID:?需要设置 IOS_TEAM_ID}"
test -x "$UNITY_PATH" || { echo "找不到 Unity：$UNITY_PATH（用 UNITY_PATH 指定）"; exit 1; }
# 服务器配置：优先用 ENV_FILE 指向的文件；没有时用仓库变量 SERVER_HOST / SERVER_PORT / SERVER_CERT_SHA256 生成一份
if [ ! -f "$ENV_FILE" ] && [ -n "${SERVER_HOST:-}" ]; then
  ENV_FILE="$PROJECT_DIR/Builds/server.env"
  mkdir -p "$(dirname "$ENV_FILE")"
  {
    echo "SERVER_HOST=$SERVER_HOST"
    echo "SERVER_PORT=${SERVER_PORT:-19060}"
    [ -n "${SERVER_CERT_SHA256:-}" ] && echo "SERVER_CERT_SHA256=$SERVER_CERT_SHA256"
  } > "$ENV_FILE"
  echo "已用仓库变量生成服务器配置 $ENV_FILE（$SERVER_HOST:${SERVER_PORT:-19060}）"
fi
test -f "$ENV_FILE" || { echo "找不到服务器配置：设置仓库变量 SERVER_HOST（及 SERVER_PORT、SERVER_CERT_SHA256），或用 REPGAME_CLIENT_ENV_FILE 指向一个 .env 文件"; exit 1; }

echo "== 1/3 Unity 导出 Xcode 工程（含 Addressables）"
export IOS_BUNDLE_ID IOS_TEAM_ID ENV_FILE BUILD_NUMBER BUILD_OUTPUT
"$UNITY_PATH" -batchmode -quit -nographics \
  -projectPath "$PROJECT_DIR" \
  -buildTarget iOS \
  -executeMethod RepGame.EditorTools.CiBuild.BuildIOS \
  -logFile "$LOG_DIR/unity.log" || { tail -n 80 "$LOG_DIR/unity.log"; exit 1; }

echo "== 2/3 Xcode 编译并签名"
# xcode-select 指向命令行工具时 xcodebuild 不可用：直接用 Xcode.app 里的开发者目录（不需要 sudo xcode-select -s）
if ! xcodebuild -version >/dev/null 2>&1; then
  XCODE_APP="${XCODE_APP:-$(ls -d /Applications/Xcode*.app 2>/dev/null | head -n 1)}"
  test -n "$XCODE_APP" || { echo "没找到 Xcode.app（可用 XCODE_APP 指定）"; exit 1; }
  export DEVELOPER_DIR="$XCODE_APP/Contents/Developer"
  echo "使用 $DEVELOPER_DIR"
fi
xcodebuild -version
xcodebuild \
  -project "$BUILD_OUTPUT/Unity-iPhone.xcodeproj" \
  -scheme Unity-iPhone \
  -configuration Release \
  -destination "generic/platform=iOS" \
  -derivedDataPath "$DERIVED" \
  -allowProvisioningUpdates \
  DEVELOPMENT_TEAM="$IOS_TEAM_ID" \
  CODE_SIGN_STYLE=Automatic \
  PRODUCT_BUNDLE_IDENTIFIER="$IOS_BUNDLE_ID" \
  build > "$LOG_DIR/xcodebuild.log" 2>&1 || { tail -n 80 "$LOG_DIR/xcodebuild.log"; exit 1; }

APP_PATH="$(find "$DERIVED/Build/Products/Release-iphoneos" -maxdepth 1 -name '*.app' | head -n 1)"
test -n "$APP_PATH" || { echo "没找到编译出的 .app"; exit 1; }
echo "已生成：$APP_PATH"
# 打包好的 .app 压缩一份，流水线作为附件上传
( cd "$(dirname "$APP_PATH")" && ditto -c -k --keepParent "$(basename "$APP_PATH")" "$PROJECT_DIR/Builds/RepGame-app.zip" )

if [ -z "${IOS_DEVICE_ID:-}" ]; then
  echo "未设置 IOS_DEVICE_ID，跳过安装"
  exit 0
fi

echo "== 3/3 安装到设备：$IOS_DEVICE_ID"
ok=0; fail=0
IFS=',' read -r -a DEVICES <<< "$IOS_DEVICE_ID"
for dev in "${DEVICES[@]}"; do
  dev="$(echo "$dev" | xargs)"
  [ -z "$dev" ] && continue
  echo "-- 安装到 $dev"
  if xcrun devicectl device install app --device "$dev" "$APP_PATH"; then
    ok=$((ok+1))
    xcrun devicectl device process launch --device "$dev" "$IOS_BUNDLE_ID" || echo "已安装；自动启动失败（设备可能锁屏），请手动打开"
  else
    fail=$((fail+1))
    echo "!! $dev 安装失败（设备不在线/未配对/锁屏？），继续下一台"
  fi
done
echo "安装完成：成功 $ok 台，失败 $fail 台"
if [ "$ok" -eq 0 ]; then
  # 设备都不在同一局域网/没连线时不算失败：包已经打好（作为附件上传），回到局域网后重新运行即可安装
  echo "::warning title=未安装到设备::包已构建成功，但没有设备可连接（不在同一局域网或未连线）。回到局域网后在 Actions 里重新运行即可安装。"
fi
