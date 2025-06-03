using System;

namespace RepGameModels
{
    [Serializable]
    public class Card
    {
        public string UID;
        public string Name;
        public float Damage;
        public string TargetName;
        public int Level;
    }
}
