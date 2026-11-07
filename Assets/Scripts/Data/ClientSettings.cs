using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

namespace RepGame.Data
{
    // ============================================================
    // 客户端配置表
    // ------------------------------------------------------------
    // 配置文件位于 Assets/StreamingAssets/Config（打包后随游戏发布，可直接修改）：
    //   client_settings.json  数值配置（网络、账号限制、界面时长）
    //   ui_text.json          界面文本（键 -> 文本，支持 {0} 占位符）
    //   error_messages.json   服务器错误码 -> 玩家提示
    // 文件缺失或格式错误时使用代码中的默认值，并在控制台报错。
    // ============================================================

    /// <summary>client_settings.json 的结构；字段默认值即内置默认配置</summary>
    [Serializable]
    public class ClientSettings
    {
        public NetworkSettings network = new NetworkSettings();
        public AccountSettings account = new AccountSettings();
        public GameplaySettings gameplay = new GameplaySettings();
        public UiSettings ui = new UiSettings();

        [Serializable]
        public class NetworkSettings
        {
            public float heartbeatIntervalSeconds = 15f; // 心跳间隔
            public float connectTimeoutSeconds = 8f;     // 连接/握手超时
            public float reconnectMaxDelaySeconds = 30f; // 重连最大间隔
            public int maxFrameBytes = 1 << 20;          // 单条消息最大字节数
            public float requestTimeoutSeconds = 10f;    // 出牌/合成等待服务器结果的最长时间
            public float receiveTimeoutSeconds = 40f;    // 超过该时间收不到服务器任何消息（含心跳回复）即判定断线
        }

        [Serializable]
        public class AccountSettings
        {
            public int usernameMinLength = 3;
            public int usernameMaxLength = 20;
            public int passwordMinLength = 6;
            public int passwordMaxLength = 24;
        }

        [Serializable]
        public class GameplaySettings
        {
            public float fallbackMaxHealth = 100f; // 收到服务器数据前的血量上限
        }

        [Serializable]
        public class UiSettings
        {
            public float messageDuration = 0.5f;
            public float damageMessageDuration = 0.8f;
            public float damageMessageInterval = 0.5f;
            public float enemyComposeMessageDelay = 0.5f;
            public float gameOverMessageDelay = 0.5f;
            public float gameOverMessageDuration = 3f;
            public float gameOverPanelDelay = 0.5f;
            public float loginToStartPanelDelay = 0.1f;
            public float registerToLoginPanelDelay = 0.5f;
            public float matchFoundPanelDelay = 0.2f;
            public float rematchPanelDelay = 0.5f;
            public float defaultPanelDelay = 0.1f;
            public float bondTooltipDelay = 0.5f;
            public float bondTooltipOffset = 150f;
            public float cardShakeDuration = 0.5f;
            public int cardPoolSize = 1;
        }
    }
}
