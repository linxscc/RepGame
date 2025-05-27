using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using RepGamebackModels;

namespace GameLogic
{
    public class DamageCalculator
    {
        private BondConfig bondConfig;

        public DamageCalculator()
        {
            LoadBondConfig();
        }

        private void LoadBondConfig()
        {
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Config", "BondConfig.json");
                if (File.Exists(path))
                {
                    string jsonContent = File.ReadAllText(path);
                    bondConfig = JsonUtility.FromJson<BondConfig>(jsonContent);
                    return;
                }
                bondConfig = new BondConfig { Bonds = new List<BondModel>() };
            }
            catch (System.Exception)
            {
                bondConfig = new BondConfig { Bonds = new List<BondModel>() };
            }
        }
        private List<BondModel> FindActiveBonds(List<CardModel> cards)
        {
            if (bondConfig?.Bonds == null || cards == null)
                return new List<BondModel>();

            var availableCards = new HashSet<string>(cards.Select(c => c.Type.ToString()));
            var activeBonds = new List<BondModel>();

            // 按伤害从高到低排序羁绊
            var orderedBonds = bondConfig.Bonds
                .Where(b => b.Cards != null && b.Cards.Count > 0)
                .OrderByDescending(b => b.Damage);

            foreach (var bond in orderedBonds)
            {
                // 检查是否所有需要的卡牌都可用
                if (bond.Cards.All(requiredCard => availableCards.Contains(requiredCard)))
                {
                    activeBonds.Add(bond);
                    // 从可用卡牌中移除已使用的卡牌
                    foreach (var usedCard in bond.Cards)
                    {
                        availableCards.Remove(usedCard);
                    }
                }
            }

            return activeBonds;
        }

        public DamageResult CalculateDamage(List<CardModel> cards, DamageType damageType = DamageType.Attacker)
        {
            if (cards == null || cards.Count == 0)
                return new DamageResult
                {
                    TotalDamage = 0,
                    ProcessedCards = new List<CardModel>(),
                    Type = damageType,
                    bonds = new List<BondModel>()
                };

            // 查找激活的羁绊（已按伤害从高到低排序）
            var activeBonds = FindActiveBonds(cards);

            // 创建已使用卡牌的HashSet，用于跟踪哪些卡牌已经在羁绊中使用
            var usedCards = new HashSet<string>();
            float totalDamage = 0;

            // 先计算羁绊伤害
            foreach (var bond in activeBonds)
            {
                totalDamage += bond.Damage;
                foreach (var cardType in bond.Cards)
                {
                    usedCards.Add(cardType);
                }
            }
            // 计算未被用于羁绊的卡牌的伤害
            var unusedCards = cards.Where(card => !usedCards.Contains(card.Type.ToString())).ToList();

            // 调试输出未使用卡牌信息
            Debug.Log($"未使用于羁绊的卡牌数量: {unusedCards.Count}");
            foreach (var card in unusedCards)
            {
                // 确保卡牌至少有基础伤害
                float cardDamage = card.Damage;
                totalDamage += cardDamage;
            }

            return new DamageResult
            {
                TotalDamage = totalDamage,
                ProcessedCards = cards,
                Type = damageType,
                bonds = activeBonds
            };
        }
    }
}
