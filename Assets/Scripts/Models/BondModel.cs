using System.Collections.Generic;

namespace RepGameModels
{
    [System.Serializable]
    public class BondModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Cards { get; set; }
        public int Level { get; set; }
        public float Damage { get; set; }
        public string Description { get; set; }
    }

    [System.Serializable]
    public class BondConfig
    {
        public BondModel[] Bonds;
    }
}
