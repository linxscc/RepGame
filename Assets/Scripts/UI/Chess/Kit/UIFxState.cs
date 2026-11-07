using DG.Tweening;
using System;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>记录对象的“目标状态”，隐藏动画进行中也能正确判断。</summary>
    public sealed class UIFxState : MonoBehaviour
    {
        public bool Shown = true;
        public Vector2 RestPos;
        public bool HasRestPos;
    }
}
