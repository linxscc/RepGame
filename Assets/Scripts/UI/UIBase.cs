using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

namespace RepGame.UI
{
    /// <summary>
    /// UI基类，提供自动查找和缓存组件的功能
    /// </summary>
    public class UIBase : MonoBehaviour
    {
        // 组件缓存
        protected Dictionary<string, Component> _componentCache = new Dictionary<string, Component>();

        /// <summary>
        /// 查找组件并缓存
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="path">相对路径</param>
        /// <returns>找到的组件</returns>
        protected T FindComponent<T>(string path) where T : Component
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
                Debug.LogWarning($"[{GetType().Name}] Path not found: {path}");
                return null;
            }

            T component = foundTransform.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Component {typeof(T).Name} not found at path: {path}");
                return null;
            }

            // 缓存查找结果
            _componentCache[cacheKey] = component;
            return component;
        }

        /// <summary>
        /// 查找按钮并添加点击事件
        /// </summary>
        /// <param name="path">按钮路径</param>
        /// <param name="onClick">点击回调</param>
        /// <returns>找到的按钮</returns>
        protected Button FindButton(string path, Action onClick)
        {
            Button button = FindComponent<Button>(path);
            if (button != null && onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            return button;
        }

        /// <summary>
        /// 查找文本组件
        /// </summary>
        /// <param name="path">文本路径</param>
        /// <returns>找到的文本组件</returns>
        protected TextMeshProUGUI FindText(string path)
        {
            return FindComponent<TextMeshProUGUI>(path);
        }

        protected Text FindTextByNormal(string path)
        {
            return FindComponent<Text>(path);
        }

        /// <summary>
        /// 查找图像组件
        /// </summary>
        /// <param name="path">图像路径</param>
        /// <returns>找到的图像组件</returns>
        protected Image FindImage(string path)
        {
            return FindComponent<Image>(path);
        }

        /// <summary>
        /// 查找游戏对象
        /// </summary>
        /// <param name="path">游戏对象路径</param>
        /// <returns>找到的游戏对象</returns>
        protected GameObject FindGameObject(string path)
        {
            Transform foundTransform = transform.Find(path);
            return foundTransform != null ? foundTransform.gameObject : null;
        }

        /// <summary>
        /// 清除组件缓存
        /// </summary>
        protected void ClearComponentCache()
        {
            _componentCache.Clear();
        }
    }
}
