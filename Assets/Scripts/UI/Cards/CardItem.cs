using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using RepGameModels;
using RepGame.Core;
using DG.Tweening;

namespace RepGame.UI
{
    public class CardItem : MonoBehaviour, IPointerClickHandler
    {
        public string CardID { get; set; }
        public CardType Type { get; set; }

        public string CardName { get; set; }

        private Vector3 originalPosition;
        private Image cardImage;
        private Material originalMaterial;
        private Material glowMaterial;

        private const float MOVE_DISTANCE_RATIO = 0.5f; // 移动自身长度的一半

        private int originalSiblingIndex; // 记录卡牌的原始索引位置
        private Transform originalParent; // 记录卡牌的原始父对象

        void Awake()
        {
            cardImage = GetComponent<Image>();
            if (cardImage != null)
            {
                originalMaterial = cardImage.material;
                // 在实际项目中，应该从资源加载发光材质
                // glowMaterial = Resources.Load<Material>("Materials/CardGlow");
            }

            originalPosition = transform.position;
        }

        void Start()
        {
            // 订阅清除所有卡牌选择的事件
            EventManager.Subscribe("ClearAllCardSelection", OnClearAllCardSelection);
        }

        void OnDestroy()
        {
            // 取消订阅事件
            EventManager.Unsubscribe("ClearAllCardSelection", OnClearAllCardSelection);
        }

        /// <summary>
        /// 处理清除所有卡牌选择的事件
        /// </summary>
        private void OnClearAllCardSelection()
        {
            if (IsSelected)
            {
                // 直接重置选中状态，不发送取消选中的消息
                IsSelected = false;

                // 取消发光效果
                ApplyGlowEffect(false);

                // 恢复到原始父对象
                if (originalParent != null)
                {
                    transform.SetParent(originalParent);
                }
            }
        }

        public void Init(string cardID, string cardName)
        {
            CardID = cardID;
            CardName = cardName;

            // 根据卡牌名称设置额外的视觉效果或属性
            // 例如可以在卡牌上显示名称
            SetCardAppearance();
        }

        private void SetCardAppearance()
        {
            // 根据卡牌类型设置卡牌外观
            // 例如可以修改卡牌颜色、图标等
            Text nameText = transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = Type.ToString();
            }

            // 可以根据卡牌类型设置不同的图标或颜色
            // Image iconImage = transform.Find("IconImage")?.GetComponent<Image>();
            // if (iconImage != null)
            // {
            //     iconImage.sprite = Resources.Load<Sprite>($"CardIcons/{Type}");
            // }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 如果卡牌已锁定，则不响应点击事件
            if (IsLocked)
            {
                // 可以添加一个提示效果，如晃动动画，提示用户此卡已锁定
                ShakeCard();
                return;
            }

            SetSelectionState(!IsSelected);
        }

        // 晃动卡牌提示已锁定
        private void ShakeCard()
        {
            // 使用DOTween实现晃动效果
            transform.DOShakePosition(0.5f, new Vector3(10, 0, 0), 10, 90, false, true);
        }

        private void SetSelectionState(bool selected)
        {
            IsSelected = selected;

            if (selected)
            {
                // 记录原始父对象和索引
                originalParent = transform.parent;
                originalSiblingIndex = transform.GetSiblingIndex();

                // 发光效果
                ApplyGlowEffect(true);

                // 将卡牌移动到PlayContainer中
                MoveForward();

                // 发送选中卡牌的消息
                EventManager.TriggerEvent("CardSelected", CardID);
            }
            else
            {
                // 取消发光效果
                ApplyGlowEffect(false);

                // 恢复到原始父对象
                if (originalParent != null)
                {
                    transform.SetParent(originalParent);
                    // 不再需要设置索引，卡牌会被添加到末尾
                }

                // 发送取消选中卡牌的消息
                EventManager.TriggerEvent("CardDeselected", CardID);
            }
        }

        public bool IsSelected { get; private set; } // 用于获取卡牌是否被选中
        public bool IsLocked { get; private set; } // 用于获取卡牌是否被锁定

        public void Deselect()
        {
            // 取消选中状态
            SetSelectionState(false);
        }

        // 设置卡牌锁定状态
        public void SetLockState(bool locked)
        {
            IsLocked = locked;

            // 更新视觉效果，锁定时添加灰色蒙版效果
            UpdateLockVisual();
        }

        // 更新锁定状态的视觉效果
        private void UpdateLockVisual()
        {
            Image image = GetComponent<Image>();
            if (image != null)
            {
                // 如果锁定，添加灰色蒙版效果
                if (IsLocked)
                {
                    image.color = new Color(0.7f, 0.7f, 0.7f, 1.0f); // 灰色
                }
                else if (IsSelected) // 如果解锁但仍被选中，恢复选中效果
                {
                    image.color = new Color(0.8f, 0.6f, 1.0f); // 浅紫色
                }
                else // 如果解锁且未选中，恢复正常效果
                {
                    image.color = Color.white;
                }
            }
        }

        private void ApplyGlowEffect(bool glow)
        {
            if (IsLocked) // 如果卡牌已锁定，不应用发光效果
                return;

            if (cardImage != null)
            {
                if (glow && glowMaterial != null)
                {
                    cardImage.material = glowMaterial;
                }
                else
                {
                    cardImage.material = originalMaterial;
                }
            }
            else
            {
                // 如果没有设置发光材质，通过改变颜色来模拟选中效果
                Image image = GetComponent<Image>();
                if (image != null)
                {
                    image.color = glow ? new Color(0.8f, 0.6f, 1.0f) : Color.white; // 浅紫色
                }
            }
        }

        private void MoveForward()
        {
            // 查找名为PlayContainer的容器
            Transform playContainer = originalParent.parent.Find("PlayContainer");

            if (playContainer == null)
            {
                Debug.LogWarning("找不到PlayContainer，将继续在原父对象的父级中显示卡牌");
                // 如果找不到PlayContainer，就使用原始父对象的父级作为替代
                transform.SetParent(originalParent.parent);
            }
            else
            {
                // 将卡牌移动到PlayContainer中
                transform.SetParent(playContainer);
            }

            // 重置本地位置，避免位置偏移
            transform.localPosition = new Vector3(0, 0, 0);

            // 保持原始缩放，不再放大
            transform.localScale = Vector3.one;
        }
    }

    // 用于传递卡牌选择数据的类
    [Serializable]
    public class CardSelectionData
    {
        public string CardID;
        public CardType Type;
        public string CardName;

    }
}
