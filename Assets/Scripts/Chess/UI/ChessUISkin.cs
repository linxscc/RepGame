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
