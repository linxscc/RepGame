using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RepGameModels;

namespace RepGame.GameLogic
{
    /// <summary>
    /// 羁绊管理器，单例模式管理羁绊数据
    /// </summary>
    public class BondManager
    {
        private static BondManager _instance;
        private static readonly object _lock = new object();

        private List<BondModel> _bondList;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static BondManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new BondManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// </summary>
        private BondManager()
        {
            _bondList = new List<BondModel>();
        }

        /// <summary>
        /// 设置羁绊数据列表
        /// </summary>
        /// <param name="bonds">羁绊数据列表</param>
        public void SetBonds(List<BondModel> bonds)
        {
            if (bonds == null)
            {
                Debug.LogWarning("BondManager: 尝试设置空的羁绊列表");
                return;
            }

            _bondList = new List<BondModel>(bonds);
        }

        /// <summary>
        /// 获取所有羁绊数据
        /// </summary>
        /// <returns>羁绊数据列表</returns>
        public List<BondModel> GetAllBonds()
        {
            return new List<BondModel>(_bondList);
        }

        /// <summary>
        /// 根据卡牌名字查询该卡牌可以构成的羁绊
        /// </summary>
        /// <param name="cardName">卡牌名字</param>
        /// <returns>该卡牌可以构成的羁绊列表</returns>
        public List<BondModel> GetBondsByCardName(string cardName)
        {
            if (string.IsNullOrEmpty(cardName))
            {
                Debug.LogWarning("BondManager: 卡牌名字不能为空");
                return new List<BondModel>();
            }

            var result = _bondList.Where(bond =>
                bond.CardNames != null &&
                bond.CardNames.Contains(cardName)
            ).ToList();

            Debug.Log($"BondManager: 卡牌 '{cardName}' 可以构成 {result.Count} 个羁绊");
            return result;
        }

        /// <summary>
        /// 根据卡牌列表查询是否有被激活的羁绊
        /// 若同一张卡牌可激活多种羁绊，则优先选择伤害高的羁绊返回
        /// </summary>
        /// <param name="cardNames">卡牌名字列表</param>
        /// <returns>激活的羁绊列表，已按伤害从高到低排序</returns>
        public List<BondModel> GetActiveBonds(List<string> cardNames)
        {
            if (cardNames == null || cardNames.Count == 0)
            {
                Debug.LogWarning("BondManager: 卡牌列表不能为空");
                return new List<BondModel>();
            }

            var availableCards = new HashSet<string>(cardNames);
            var activeBonds = new List<BondModel>();

            // 按伤害从高到低排序羁绊，确保优先选择高伤害羁绊
            var orderedBonds = _bondList
                .Where(bond => bond.CardNames != null && bond.CardNames.Count > 0)
                .OrderByDescending(bond => bond.Damage);

            foreach (var bond in orderedBonds)
            {
                // 检查是否所有需要的卡牌都可用
                if (bond.CardNames.All(requiredCard => availableCards.Contains(requiredCard)))
                {
                    activeBonds.Add(bond);

                    // 从可用卡牌中移除已使用的卡牌，防止重复使用
                    foreach (var usedCard in bond.CardNames)
                    {
                        availableCards.Remove(usedCard);
                    }
                }
            }

            Debug.Log($"BondManager: 从 {cardNames.Count} 张卡牌中激活了 {activeBonds.Count} 个羁绊");
            return activeBonds;
        }

        /// <summary>
        /// 根据卡牌列表查询最优的单个羁绊（伤害最高的）
        /// </summary>
        /// <param name="cardNames">卡牌名字列表</param>
        /// <returns>伤害最高的可激活羁绊，如果没有则返回null</returns>
        public BondModel GetBestBond(List<string> cardNames)
        {
            var activeBonds = GetActiveBonds(cardNames);
            return activeBonds.FirstOrDefault(); // 由于已经按伤害排序，第一个就是最优的
        }

        /// <summary>
        /// 检查指定羁绊是否可以被激活
        /// </summary>
        /// <param name="bondId">羁绊ID</param>
        /// <param name="cardNames">卡牌名字列表</param>
        /// <returns>是否可以激活</returns>
        public bool CanActivateBond(int bondId, List<string> cardNames)
        {
            var bond = _bondList.FirstOrDefault(b => b.ID == bondId);
            if (bond == null || bond.CardNames == null)
            {
                return false;
            }

            return bond.CardNames.All(requiredCard => cardNames.Contains(requiredCard));
        }

        /// <summary>
        /// 获取羁绊的详细信息
        /// </summary>
        /// <param name="bondId">羁绊ID</param>
        /// <returns>羁绊信息，如果不存在则返回null</returns>
        public BondModel GetBondById(int bondId)
        {
            return _bondList.FirstOrDefault(bond => bond.ID == bondId);
        }

        /// <summary>
        /// 清空所有羁绊数据
        /// </summary>
        public void ClearBonds()
        {
            _bondList.Clear();
            Debug.Log("BondManager: 已清空所有羁绊数据");
        }

        /// <summary>
        /// 获取当前管理的羁绊数量
        /// </summary>
        /// <returns>羁绊数量</returns>
        public int GetBondCount()
        {
            return _bondList.Count;
        }
    }
}
