using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using RepGameModels;
using RepGame.Core;

namespace RepGame.UI
{
    public class CardItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public string CardID { get; set; }
        public CardType Type { get; set; }
        
        private Vector3 originalPosition;
        private Image cardImage;
        private bool isSelected = false;
        private Material originalMaterial;
        private Material glowMaterial;
        
        private const float MOVE_DISTANCE_RATIO = 0.5f; // 移动自身长度的一半
        
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
        
        public void OnPointerDown(PointerEventData eventData)
        {
            isSelected = true;
            
            // 发光效果
            ApplyGlowEffect(true);
            
            // 向前移动
            MoveForward();
            
            // 发送选中卡牌的消息
            EventManager.TriggerEvent("CardSelected", new CardSelectionData { CardID = CardID, Type = Type });
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            isSelected = false;
            
            // 取消发光效果
            ApplyGlowEffect(false);
            
            // 恢复位置
            transform.position = originalPosition;
            
            // 发送取消选中卡牌的消息
            EventManager.TriggerEvent("CardDeselected", new CardSelectionData { CardID = CardID, Type = Type });
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
                // 如果没有设置发光材质，可以通过改变颜色来模拟选中效果
                Image image = GetComponent<Image>();
                if (image != null)
                {
                    image.color = glow ? new Color(1.2f, 1.2f, 1.2f) : Color.white;
                }
            }
        }
        
        private void MoveForward()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 计算向前移动的距离（卡牌高度的一半）
                float moveDistance = rectTransform.rect.height * MOVE_DISTANCE_RATIO;
                
                // 向上移动（在UI坐标系中，向上为正Y方向）
                transform.position = originalPosition + new Vector3(0, moveDistance, 0);
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
