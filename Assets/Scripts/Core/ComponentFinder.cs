using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace RepGame.Core
{
    /// <summary>
    /// 通用组件查找工具，通过约定命名自动查找并缓存组件
    /// </summary>
    public class ComponentFinder : MonoBehaviour
    {
        // 组件缓存，避免重复查找
        private Dictionary<string, Component> _componentCache = new Dictionary<string, Component>();
        
        /// <summary>
        /// 按照路径查找组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="path">路径，相对于当前GameObject</param>
        /// <returns>找到的组件</returns>
        public T Find<T>(string path) where T : Component
        {
            string cacheKey = $"{typeof(T).Name}_{path}";
            
            // 尝试从缓存获取
            if (_componentCache.TryGetValue(cacheKey, out Component cachedComponent))
            {
                return cachedComponent as T;
            }
            
            // 查找组件
            Transform foundTransform = transform.Find(path);
            if (foundTransform == null)
            {
                Debug.LogWarning($"[ComponentFinder] Path not found: {path}");
                return null;
            }
            
            T component = foundTransform.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning($"[ComponentFinder] Component {typeof(T).Name} not found at path: {path}");
                return null;
            }
            
            // 缓存查找结果
            _componentCache[cacheKey] = component;
            return component;
        }
        
        /// <summary>
        /// 查找按钮组件并添加点击事件监听器
        /// </summary>
        /// <param name="path">按钮路径</param>
        /// <param name="onClick">点击回调</param>
        /// <returns>找到的按钮</returns>
        public Button FindButton(string path, Action onClick)
        {
            Button button = Find<Button>(path);
            if (button != null && onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            return button;
        }
        
        /// <summary>
        /// 查找文本组件
        /// </summary>
        /// <param name="path">文本组件路径</param>
        /// <returns>找到的文本组件</returns>
        public TextMeshProUGUI FindText(string path)
        {
            return Find<TextMeshProUGUI>(path);
        }
        
        /// <summary>
        /// 查找游戏对象
        /// </summary>
        /// <param name="path">游戏对象路径</param>
        /// <returns>找到的游戏对象</returns>
        public GameObject FindGameObject(string path)
        {
            Transform foundTransform = transform.Find(path);
            return foundTransform != null ? foundTransform.gameObject : null;
        }
        
        /// <summary>
        /// 清除组件缓存
        /// </summary>
        public void ClearCache()
        {
            _componentCache.Clear();
        }
    }
}
