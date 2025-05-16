using UnityEngine;

[CreateAssetMenu(fileName = "AnimationConfig", menuName = "Configs/AnimationConfig", order = 1)]
public class AnimationConfig : ScriptableObject
{
    public float isIdle = 1;
    public float isWalking = 1;
    public float isRuning = 1;
}