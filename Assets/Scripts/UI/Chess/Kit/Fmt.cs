using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Data;

namespace RepGame.UI.Chess
{
    public static class Fmt
    {
        /// <summary>得分显示：万 / 亿 缩写。</summary>
        public static string Score(double v)
        {
            if (v >= 1e12) return (v / 1e12).ToString("0.##") + "兆";
            if (v >= 1e8) return (v / 1e8).ToString("0.##") + "亿";
            if (v >= 1e5) return (v / 1e4).ToString("0.#") + "万";
            return v.ToString("#,0");
        }

        public static string Num(double v) => v == System.Math.Floor(v) ? v.ToString("0") : v.ToString("0.##");

        public static string T(string key, string fallback, params object[] args)
        {
            var s = ConfigTables.Text(key, args);
            return s == key ? (args.Length > 0 ? string.Format(fallback, args) : fallback) : s;
        }
    }
}
