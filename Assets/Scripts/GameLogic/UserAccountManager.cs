using UnityEngine;

namespace RepGame.GameLogic
{
    /// <summary>
    /// 用户账户管理器，单例模式管理用户相关信息
    /// </summary>
    public class UserAccountManager
    {
        private static UserAccountManager _instance;
        private static readonly object _lock = new object();

        private string _username;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static UserAccountManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new UserAccountManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 用户名属性
        /// </summary>
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// </summary>
        private UserAccountManager()
        {
            _username = string.Empty;
        }

        /// <summary>
        /// 获取用户名
        /// </summary>
        /// <returns>用户名</returns>
        public string GetUsername()
        {
            return _username;
        }

        /// <summary>
        /// 设置用户名
        /// </summary>
        /// <param name="username">要设置的用户名</param>
        public void SetUsername(string username)
        {
            _username = username ?? string.Empty;
        }

        /// <summary>
        /// 检查用户名是否有效
        /// </summary>
        /// <returns>用户名是否有效</returns>
        public bool IsUsernameValid()
        {
            return !string.IsNullOrEmpty(_username) && !string.IsNullOrWhiteSpace(_username);
        }

        /// <summary>
        /// 清空用户名
        /// </summary>
        public void ClearUsername()
        {
            _username = string.Empty;
        }        /// <summary>
                 /// 重写ToString方法
                 /// </summary>
                 /// <returns>用户账户的字符串表示</returns>
        public override string ToString()
        {
            return $"UserAccountManager: {_username}";
        }

        /// <summary>
        /// 重置单例实例（主要用于测试或重新初始化）
        /// </summary>
        public static void ResetInstance()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }
    }

    public class UserInfo
    {
        public string username;
    }

}