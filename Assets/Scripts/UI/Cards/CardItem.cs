using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using RepGameModels;
using RepGame.Core;

namespace RepGame.UI
{
    public class CardItem : MonoBehaviour, IPointerClickHandler
    {
        public string CardID { get; set; }
        public CardType Type { get; set; }
        
        private Vector3 originalPosition;
        private Image cardImage;
        private bool isSelected = false;
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
        
        public void Init(string cardID, CardType type)
        {
            CardID = cardID;
            Type = type;
            
            // 根据卡牌类型设置额外的视觉效果或属性
            // 例如不同类型的卡牌可能有不同的背景色
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
            SetSelectionState(!IsSelected);
        }
        
        private void SetSelectionState(bool selected)
        {
            IsSelected = selected;

            if (selected)
            {
                // 记录原始父对象和索引
                originalParent = transform.parent;
                originalSiblingIndex = transform.GetSiblingIndex();

                // 将卡牌从 GridLayoutGroup 中移除
                transform.SetParent(originalParent.parent);

                // 发光效果
                ApplyGlowEffect(true);

                // 向前移动
                MoveForward();

                // 发送选中卡牌的消息
                EventManager.TriggerEvent("CardSelected", new CardSelectionData { CardID = CardID, Type = Type });
            }
            else
            {
                // 取消发光效果
                ApplyGlowEffect(false);

                // 恢复到原始父对象和索引
                if (originalParent != null)
                {
                    transform.SetParent(originalParent);
                    transform.SetSiblingIndex(originalSiblingIndex);
                }

                // 发送取消选中卡牌的消息
                EventManager.TriggerEvent("CardDeselected", new CardSelectionData { CardID = CardID, Type = Type });
            }
        }

        public bool IsSelected { get; private set; } // 用于获取卡牌是否被选中

        public void Deselect()
        {
            SetSelectionState(false);
        }

        private void ApplyGlowEffect(bool glow)
        {
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
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 计算向前移动的距离（卡牌高度）
                float moveDistance = rectTransform.rect.height*1.2f;

                // 向上移动（在本地坐标系中，向上为正Y方向）
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + moveDistance, transform.localPosition.z);
            }
        }
    }
    
    // 用于传递卡牌选择数据的类
    [Serializable]
    public class CardSelectionData
    {
        public string CardID;
        public CardType Type;
    }
}
