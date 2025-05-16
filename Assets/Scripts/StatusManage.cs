using UnityEngine;

public class StatusManage
{
    // 游戏状态枚举
    public enum GameStateType
    {
        Initialized, // 初始化
        Started,     // 游戏开始
        Playing,     // 游戏进行中
        Ended        // 游戏结束
    }

    // 单例实例
    private static StatusManage _instance;

    // 公共访问点
    public static StatusManage Instance => _instance ??= new StatusManage();

    // 游戏状态变量
    public GameStateType GameState { get; private set; }

    // 游戏状态变化事件
    public event System.Action<GameStateType> OnGameStateChanged;

    // 私有构造函数，防止外部实例化
    private StatusManage()
    {
        GameState = GameStateType.Initialized; // 默认状态
    }

    public void SetGameState(GameStateType newState)
    {
        if (GameState != newState)
        {
            GameState = newState;
            OnGameStateChanged?.Invoke(GameState); // 触发事件
        }
    }
}
