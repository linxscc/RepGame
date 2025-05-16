using UnityEngine;

public class AnimationStateManager : MonoBehaviour
{
    private Animator animator;
    private AnimationConfig animationConfig;

    public void Initialize(Animator animator, AnimationConfig animationConfig)
    {
        this.animator = animator;
        this.animationConfig = animationConfig;
    }

    public void SetIdle()
    {
        animator.SetFloat("isIdle", animationConfig.isIdle);
        animator.SetFloat("isWalking", 0);
        animator.SetFloat("isRunning", 0);
    }

    public void SetWalking()
    {
        animator.SetFloat("isIdle", 0);
        animator.SetFloat("isWalking", animationConfig.isWalking);
        animator.SetFloat("isRunning", 0);
    }

    public void SetRunning()
    {
        animator.SetFloat("isIdle", 0);
        animator.SetFloat("isWalking", 0);
        animator.SetFloat("isRunning", animationConfig.isRuning);
    }
}