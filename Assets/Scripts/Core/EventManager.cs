using System;
using System.Collections.Generic;

namespace RepGame.Core
{
    public static class EventManager
    {
        private static Dictionary<string, Action> eventTable = new Dictionary<string, Action>();

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

        public static void TriggerEvent(string eventName)
        {
            if (eventTable.ContainsKey(eventName))
            {
                eventTable[eventName]?.Invoke();
            }
        }
    }
}
