using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private Dictionary<int, GameObject> players = new Dictionary<int, GameObject>(); // 存储玩家对象
    [SerializeField] private GameObject playerPrefab; // 玩家预制体

    public void UpdatePlayer(int clientId, Vector3 position)
    {
        if (!players.ContainsKey(clientId))
        {
            // 实例化新的玩家对象
            GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
            players[clientId] = player;
        }
        else
        {
            // 更新已有玩家的位置
            players[clientId].transform.position = position;
        }
    }

    public void RemovePlayer(int clientId)
    {
        if (players.ContainsKey(clientId))
        {
            Destroy(players[clientId]);
            players.Remove(clientId);
        }
    }
}