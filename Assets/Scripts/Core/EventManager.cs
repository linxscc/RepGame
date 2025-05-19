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
        public static void Subscribe<T>(string eventName, Action<T> listener)
        {
            if (!eventTableWithArgs.ContainsKey(eventName))
            {
                eventTableWithArgs[eventName] = listener;
            }
            else
            {
                eventTableWithArgs[eventName] = Delegate.Combine(eventTableWithArgs[eventName], listener);
            }
        }

        // Unsubscribe from events with arguments
        public static void Unsubscribe<T>(string eventName, Action<T> listener)
        {
            if (eventTableWithArgs.ContainsKey(eventName))
            {
                eventTableWithArgs[eventName] = Delegate.Remove(eventTableWithArgs[eventName], listener);
                if (eventTableWithArgs[eventName] == null)
                {
                    eventTableWithArgs.Remove(eventName);
                }
            }
        }

        // Trigger events with arguments
        public static void TriggerEvent<T>(string eventName, T arg)
        {
            if (eventTableWithArgs.ContainsKey(eventName))
            {
                if (eventTableWithArgs[eventName] is Action<T> callback)
                {
                    callback.Invoke(arg);
                }
            }
        }
    }
}
