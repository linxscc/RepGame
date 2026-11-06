using TMPro;
using UnityEngine;

namespace RepGame.Chess.UI
{
    /// <summary>
    /// 文字流光：一道亮带从左到右反复扫过文字（逐顶点改色，不需要额外材质）。
    /// 透明度跟随 TextMeshPro 自身的 alpha，所以可以照常用 DOFade 淡入淡出。
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class ShimmerText : MonoBehaviour
    {
        public Color baseColor = new Color(1f, 0.82f, 0.48f);   // 金色
        public Color shineColor = new Color(1f, 0.98f, 0.88f);  // 亮带颜色
        public float period = 1.1f;   // 扫过一次的秒数
        public float band = 0.22f;    // 亮带宽度（占文字宽度的比例）

        private TextMeshProUGUI tmp;

        private void Awake() => tmp = GetComponent<TextMeshProUGUI>();

        private void LateUpdate()
        {
            if (tmp == null || string.IsNullOrEmpty(tmp.text)) return;
            tmp.ForceMeshUpdate();
            var info = tmp.textInfo;
            if (info.characterCount == 0) return;
            var b = tmp.textBounds;
            float minX = b.min.x, width = Mathf.Max(1f, b.size.x);
            // 亮带位置从 -band 走到 1+band，循环
            float pos = Mathf.Repeat(Time.unscaledTime / period, 1f) * (1 + 2 * band) - band;
            float alpha = tmp.color.a;
            for (int i = 0; i < info.characterCount; i++)
            {
                var ch = info.characterInfo[i];
                if (!ch.isVisible) continue;
                var colors = info.meshInfo[ch.materialReferenceIndex].colors32;
                var verts = info.meshInfo[ch.materialReferenceIndex].vertices;
                for (int v = 0; v < 4; v++)
                {
                    int idx = ch.vertexIndex + v;
                    float x = (verts[idx].x - minX) / width;
                    float k = Mathf.Clamp01(1f - Mathf.Abs(x - pos) / band);
                    var c = Color.Lerp(baseColor, shineColor, k * k);
                    c.a = alpha;
                    colors[idx] = c;
                }
            }
            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
