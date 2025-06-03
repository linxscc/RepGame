using System.Collections.Generic;

namespace RepGameModels
{
    [System.Serializable]
    public class BondModel
    {
        public string Name;
        public List<string> Cards;
        public float Damage;
        public string Description;
    }
}
