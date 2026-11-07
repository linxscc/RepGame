using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>拖拽的内容：一张卡（uid）或一个法宝栏位。</summary>
    public sealed class DragPayload
    {
        public string CardUid;     // 拖的是卡
        public bool FromBoard;
        public int RelicSlot = -1; // 拖的是法宝
    }
}
