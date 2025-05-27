using System.Collections.Generic;
using System;

namespace RepGamebackModels
{
    [Serializable]
    public class BondModel
    {
        public string Id;
        public string Name;
        public List<string> Cards;
        public int Level;
        public float Damage;
        public string Description;
    }

    [Serializable]
    public class BondConfig
    {
        public List<BondModel> Bonds;
    }
}
