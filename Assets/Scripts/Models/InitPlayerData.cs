using System.Collections.Generic;
using System;

namespace RepGameModels
{
    [Serializable]
    public class InitPlayerData
    {
        public List<CardModel> Cards;
        public int Health;
    }
}