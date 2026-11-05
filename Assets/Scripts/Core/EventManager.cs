using System;
using System.Collections.Generic;

namespace RepGame.Core
{
    public static class EventManager
    {
        private static Dictionary<string, Action> eventTable = new Dictionary<string, Action>();
        private static Dictionary<string, Delegate> eventTableWithArgs = new Dictionary<string, Delegate>();

        // Subscribe to events without arguments
        public static void Subscribe(string eventName, Action listener)
        {
            if (!eventTable.ContainsKey(eventName))
            {
                eventTable[eventName] = listener;
            }
            else
            {
                eventTable[eventName] += listener;
            }
        }

        // Unsubscribe from events without arguments
        public static void Unsubscribe(string eventName, Action listener)
        {
            if (eventTable.ContainsKey(eventName))
            {
                eventTable[eventName] -= listener;
                if (eventTable[eventName] == null)
                {
                    eventTable.Remove(eventName);
                }
            }
        }

        // Trigger events without arguments
        public static void TriggerEvent(string eventName)
        {
            if (eventTable.ContainsKey(eventName))
            {
                eventTable[eventName]?.Invoke();
            }
        }

        // Subscribe to events with arguments
        // 同一事件名只允许一种参数类型；类型冲突时记录错误并忽略本次订阅，
        // 避免 Delegate.Combine 抛异常导致调用方后续的订阅全部丢失。
        public static void Subscribe<T>(string eventName, Action<T> listener)
        {
            if (!eventTableWithArgs.TryGetValue(eventName, out var existing) || existing == null)
            {
                eventTableWithArgs[eventName] = listener;
                return;
            }
            if (!(existing is Action<T>))
            {
                UnityEngine.Debug.LogError($"事件 '{eventName}' 参数类型冲突：已有订阅类型 {existing.GetType()}，忽略 {typeof(Action<T>)} 的订阅");
                return;
            }
            eventTableWithArgs[eventName] = Delegate.Combine(existing, listener);
        }

        // Unsubscribe from events with arguments
        public static void Unsubscribe<T>(string eventName, Action<T> listener)
        {
            if (eventTableWithArgs.TryGetValue(eventName, out var existing) && existing is Action<T>)
            {
                var remaining = Delegate.Remove(existing, listener);
                if (remaining == null)
                {
                    eventTableWithArgs.Remove(eventName);
                }
                else
                {
                    eventTableWithArgs[eventName] = remaining;
                }
            }
        }

        // Trigger events with arguments
        public static void TriggerEvent<T>(string eventName, T arg)
        {
            if (!eventTableWithArgs.TryGetValue(eventName, out var existing))
            {
                return; // 没有订阅者
            }
            if (existing is Action<T> callback)
            {
                callback.Invoke(arg);
            }
            else
            {
                UnityEngine.Debug.LogError($"事件 '{eventName}' 触发参数类型 {typeof(T)} 与订阅类型 {existing.GetType()} 不一致，已忽略");
            }
        }
    }
}
