using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RepGame.Chess.UI
{
    /// <summary>
    /// 对局界面用到的美术资源（Resources/ChessUISkin.asset）。
    /// 卡图按卡名查找；找不到时界面会显示带卡名的占位框。
    /// </summary>
    [CreateAssetMenu(menuName = "RepGame/Chess UI Skin")]
    public class ChessUISkin : ScriptableObject
    {
        [Serializable]
        public class NamedSprite
        {
            public string name;
            public Sprite sprite;
        }

        public TMP_FontAsset font;
        [Tooltip("中文字体源文件；运行时据此生成动态字库（多图集，不会因为图集写满而显示方块）")]
        public Font sourceFont;
        public Sprite rounded;
        public Sprite circle;
        public Sprite cardBack;
        public Sprite avatarMe;
        public Sprite avatarEnemy;
        public List<NamedSprite> cards = new List<NamedSprite>();
        public List<NamedSprite> relicIcons = new List<NamedSprite>();

        [Header("结算过场：贴图（留空时用代码画的简单形状占位）")]
        public Sprite voidBackground;   // 虚空背景（铺满全屏）
        public Sprite voidMist;         // 虚空中漂浮的雾气 / 墨云
        public Sprite fireball;         // 火球本体
        public Sprite fireTrail;        // 拖尾火星
        public Sprite impactBurst;      // 命中爆炸
        public Sprite verdictWin;       // “胜”字样
        public Sprite verdictLose;      // “负”字样
        public Sprite verdictDraw;      // “平”字样

        [Header("结算过场：节奏（秒）")]
        public float cutsceneIntro = 0.4f;     // 淡入
        public float stepMinSeconds = 0.12f;   // 每个计分步骤最短时长
        public float stepMaxSeconds = 0.6f;    // 每个计分步骤最长时长
        public float stepExtraSeconds = 0.6f;  // 每步额外放慢（看清楚每一步）
        public float verdictHold = 1.5f;       // 积分算完后停留多久再淡出（含胜负字样）
        public float cutsceneOutro = 0.6f;     // 淡出
        public float fireFlightSeconds = 0.6f; // 火球飞行时间
        public float fireArcHeight = 220f;     // 火球弧线高度（像素）
        public float trailInterval = 0.04f;    // 拖尾火星的间隔（占飞行进度的比例）
        public float impactScale = 2.4f;       // 命中爆炸放大倍数
        public float hpDrainSeconds = 0.7f;    // 血条下降时长

        [Header("结算过场：画面")]
        [Range(0, 1)] public float veilAlpha = 0.55f;   // 云雾遮罩的不透明度（1 = 完全挡住对局界面）
        [Range(0, 1)] public float mistAlpha = 0.7f;    // 飘入的云团不透明度
        public float scoreFontSize = 162f;              // 中间双方积分的字号
        public float scoreShakeAngle = 8f;              // 积分增长时的晃动角度
        public float scoreShakeOffset = 14f;            // 积分增长时的晃动位移（像素）

        [Header("界面过渡")]
        public float uiInSeconds = 0.22f;      // 弹窗/面板/按钮出现
        public float uiOutSeconds = 0.16f;     // 消失
        public float uiPopFrom = 0.92f;        // 出现时从多大放大到 1
        public float uiSlideDistance = 40f;    // 顶部浮条滑入距离（像素）

        private TMP_FontAsset runtimeFont;
        private Sprite smoothCircle;
        private Sprite smoothRounded;
        private Dictionary<string, Sprite> cardIndex;
        private Dictionary<string, Sprite> relicIndex;

        private static ChessUISkin instance;

        public static ChessUISkin Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ChessUISkin>("ChessUISkin");
                    if (instance == null)
                    {
                        Debug.LogError("缺少 Resources/ChessUISkin.asset");
                        instance = CreateInstance<ChessUISkin>();
                    }
                }
                return instance;
            }
        }

        /// <summary>界面用的字体：优先用运行时生成的动态多图集字库。</summary>
        public TMP_FontAsset UIFont
        {
            get
            {
                if (runtimeFont == null && sourceFont != null)
                {
                    runtimeFont = TMP_FontAsset.CreateFontAsset(sourceFont, 64, 8, GlyphRenderMode.SDFAA, 2048, 2048,
                        AtlasPopulationMode.Dynamic, true);
                    if (runtimeFont != null) runtimeFont.name = sourceFont.name + " (Runtime)";
                }
                return runtimeFont != null ? runtimeFont : font;
            }
        }

        /// <summary>抗锯齿圆形（256px），替代内置的小圆点图，放大后不糊。</summary>
        public Sprite Circle => smoothCircle != null ? smoothCircle : (smoothCircle = MakeRounded(256, 128, false));

        /// <summary>抗锯齿圆角矩形（九宫格，圆角 12px）。</summary>
        public Sprite Rounded => smoothRounded != null ? smoothRounded : (smoothRounded = MakeRounded(64, 12, true));

        private static Sprite MakeRounded(int size, float radius, bool sliced)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = sliced ? "UIRounded" : "UICircle",
            };
            var px = new Color32[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 到圆角矩形边界的有向距离，1px 过渡做抗锯齿
                    float qx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
                    byte a = (byte)(Mathf.Clamp01(0.5f - outside) * 255);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        public Sprite Card(string cardName) => Find(ref cardIndex, cards, cardName);

        public Sprite RelicIcon(string relicKey) => Find(ref relicIndex, relicIcons, relicKey);

        private static Sprite Find(ref Dictionary<string, Sprite> index, List<NamedSprite> list, string key)
        {
            if (index == null)
            {
                index = new Dictionary<string, Sprite>();
                foreach (var e in list)
                {
                    if (e != null && !string.IsNullOrEmpty(e.name) && e.sprite != null) index[e.name] = e.sprite;
                }
            }
            return key != null && index.TryGetValue(key, out var s) ? s : null;
        }
    }
}
