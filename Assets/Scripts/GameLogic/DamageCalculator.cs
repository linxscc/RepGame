using System.Collections.Generic;
using RepGamebackModels;

namespace GameLogic
{
    public class DamageCalculator
    {
        public static DamageResult CalculateDamage(List<CardModel> cards)
        {
            float totalDamage = 0f;

            // 简单的伤害累加计算
            foreach (var card in cards)
            {
                totalDamage += card.Damage;
            }

            return new DamageResult
            {
                TotalDamage = totalDamage,
                ProcessedCards = cards
            };
        }
    }
}
