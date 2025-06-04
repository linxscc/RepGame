using System.Collections.Generic;

namespace RepGameModels
{
    [System.Serializable]
    public class BondModel
    {
        public string Name;
        public List<Card> Cards;
        public float Damage;
        public string Description;
    }
}
