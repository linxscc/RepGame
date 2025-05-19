using System;
using UnityEngine;
using System.Collections.Generic;

namespace RepGamebackModels
{
        [System.Serializable]
        public class CardDeckWrapper
        {
            public List<string> keys;
            public List<int> values;

            public Dictionary<string, int> ToDictionary()
            {
                Dictionary<string, int> dict = new Dictionary<string, int>();
                for (int i = 0; i < keys.Count; i++)
                {
                    dict[keys[i]] = values[i];
                }
                return dict;
            }
        }
}
