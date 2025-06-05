using System.Collections.Generic;

namespace RepGameModels
{
    [System.Serializable]
    public class BondModel
    {
        public string Name;
        public List<string> CardNames;
        public float Damage;
        public string Description;
    }
}
