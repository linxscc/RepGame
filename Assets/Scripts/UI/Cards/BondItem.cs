using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

namespace RepGame.UI
{
    public class BondItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Bond Data")]
        public bool IsActived { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                UpdateButtonText();
            }
        }
        public int Level { get; set; }
        public List<string> CardNames { get; set; }
        public float Damage { get; set; }
        public string Description { get; set; }
        public string Skill { get; set; }

        [Header("UI Components")]
        [SerializeField] private GameObject tooltipPrefab; // 文本预制体

        private Button button;
        private GameObject currentTooltip;
        private Coroutine hoverCoroutine;
        private bool isHovering = false;
        private void Start()
        {
            // 获取自身的Button组件
            button = GetComponent<Button>();
            if (button != null)
            {
                // 设置按钮不可点击
                button.interactable = false;
            }
            else
            {
                Debug.LogWarning($"BondItem {gameObject.name} 没有找到Button组件");
            }

            // 更新按钮文本
            UpdateButtonText();
        }
        private void UpdateButtonText()
        {
            // 如果button还没有初始化，先获取它
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null && !string.IsNullOrEmpty(_name))
            {
                // 查找Button下的TextMeshProUGUI组件
                var tmpText = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmpText != null)
                {
                    tmpText.text = _name;
                }
                else
                {
                    Debug.LogWarning($"BondItem {gameObject.name} 的Button下没有找到TextMeshProUGUI组件");
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            // 开始悬停计时
            hoverCoroutine = StartCoroutine(HoverTimer());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            // 停止悬停计时
            if (hoverCoroutine != null)
            {
                StopCoroutine(hoverCoroutine);
                hoverCoroutine = null;
            }

            // 隐藏提示文本
            HideTooltip();
        }

        private IEnumerator HoverTimer()
        {
            // 等待0.5秒
            yield return new WaitForSeconds(0.5f);

            // 如果仍在悬停状态，显示提示文本
            if (isHovering && tooltipPrefab != null)
            {
                ShowTooltip();
            }
        }
        private void ShowTooltip()
        {
            if (currentTooltip != null)
            {
                return; // 已经显示了提示
            }

            // 实例化提示文本预制体到自身的四层父级之下
            Transform targetParent = GetFourthParent();
            if (targetParent != null)
            {
                currentTooltip = Instantiate(tooltipPrefab, targetParent);

                // 设置提示文本位置在鼠标左侧
                SetTooltipPosition();

                // 设置提示文本内容
                SetTooltipContent();
            }
        }

        private Transform GetFourthParent()
        {
            Transform current = transform;
            for (int i = 0; i < 4; i++)
            {
                if (current.parent != null)
                {
                    current = current.parent;
                }
                else
                {
                    Debug.LogWarning($"BondItem {gameObject.name} 无法找到第{i + 1}层父级，使用当前找到的最高层级: {current.name}");
                    break;
                }
            }
            return current;
        }

        private void SetTooltipPosition()
        {
            if (currentTooltip == null) return;

            // 获取鼠标位置
            Vector3 mousePosition = Input.mousePosition;

            // 获取提示框的RectTransform
            RectTransform tooltipRect = currentTooltip.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                // 设置位置在鼠标左侧（偏移一定距离）
                Vector3 tooltipPosition = mousePosition;
                tooltipPosition.x -= 150f; // 向左偏移150像素

                // 确保提示框不会超出屏幕左边界
                if (tooltipPosition.x < 0)
                {
                    tooltipPosition.x = mousePosition.x + 20f; // 如果左侧超出，则显示在右侧
                }

                tooltipRect.position = tooltipPosition;
            }
        }

        private void SetTooltipContent()
        {
            if (currentTooltip == null) return;

            // 查找提示框中的Text组件并设置内容
            var textComponents = currentTooltip.GetComponentsInChildren<UnityEngine.UI.Text>();
            var tmpComponents = currentTooltip.GetComponentsInChildren<TMPro.TextMeshProUGUI>();

            string tooltipText = GenerateTooltipText();

            // 设置Text组件内容
            if (textComponents.Length > 0)
            {
                textComponents[0].text = tooltipText;
            }

            // 设置TextMeshPro组件内容
            if (tmpComponents.Length > 0)
            {
                tmpComponents[0].text = tooltipText;
            }
        }

        private string GenerateTooltipText()
        {
            string cardNamesText = CardNames != null && CardNames.Count > 0
                ? string.Join(", ", CardNames)
                : "无";

            return $"<b>{Name}</b>\n" +
                   $"等级: {Level}\n" +
                   $"伤害: {Damage}\n" +
                   $"卡牌: {cardNamesText}\n" +
                   $"技能: {Skill}\n" +
                   $"描述: {Description}\n" +
                   $"状态: {(IsActived ? "已激活" : "未激活")}";
        }

        private void HideTooltip()
        {
            if (currentTooltip != null)
            {
                Destroy(currentTooltip);
                currentTooltip = null;
            }
        }

        private void OnDestroy()
        {
            // 清理资源
            if (hoverCoroutine != null)
            {
                StopCoroutine(hoverCoroutine);
            }

            HideTooltip();
        }
    }
}