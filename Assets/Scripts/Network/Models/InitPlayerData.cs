using System.Collections.Generic;

namespace RepGamebackModels
{
    [System.Serializable]
    public class InitPlayerData
    {
        public List<CardModel> Cards;
        public int Health;
    }
}