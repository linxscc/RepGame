using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
        public Sprite rounded;
        public Sprite circle;
        public Sprite cardBack;
        public Sprite avatarMe;
        public Sprite avatarEnemy;
        public List<NamedSprite> cards = new List<NamedSprite>();
        public List<NamedSprite> relicIcons = new List<NamedSprite>();

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
