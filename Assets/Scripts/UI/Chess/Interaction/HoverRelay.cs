using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace RepGame.UI.Chess
{
    public class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action Enter;
        public Action Exit;
        public Action Click;

        public void OnPointerEnter(PointerEventData e) => Enter?.Invoke();
        public void OnPointerExit(PointerEventData e) => Exit?.Invoke();
        public void OnPointerClick(PointerEventData e) => Click?.Invoke();
        private void OnDisable() => Exit?.Invoke();
    }
}
