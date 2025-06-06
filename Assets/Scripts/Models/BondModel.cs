using System.Collections.Generic;

namespace RepGameModels
{
    [System.Serializable]
    public class BondModel
    {
        public int ID;
        public string Name;
        public int Level;
        public List<string> CardNames;
        public float Damage;
        public string Description;
        public string Skill;
    }
}
