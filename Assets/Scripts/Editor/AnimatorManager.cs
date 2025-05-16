using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimatorManager : MonoBehaviour
{
    [System.Serializable]
    public class TransitionCondition
    {
        public string parameterName; // 参数名称
        public string conditionMode; // 条件模式 ("Greater", "Less", "Equals", "NotEquals")
        public float threshold; // 阈值
        public string targetStateName; // 目标状态名称
    }

    [System.Serializable]
    public class StateConfig
    {
        public string stateName; // 状态名称
        public string animationClipPath; // 动画剪辑路径
        public bool hasExitTime; // 是否有退出时间
        public float exitTime; // 退出时间
        public List<TransitionCondition> transitions; // 切换条件
    }

    [System.Serializable]
    public class AnimatorConfig
    {
        public string animatorControllerPath; // 动画控制器保存路径
        public List<StateConfig> states; // 状态列表
    }

    [MenuItem("Tools/Animator Manager/Generate Animator From JSON")]
    public static void GenerateAnimatorFromJsonMenu()
    {
        // 打开文件选择器
        string jsonPath = EditorUtility.OpenFilePanel("Select JSON Config File", Application.dataPath, "json");
        if (!string.IsNullOrEmpty(jsonPath))
        {
            GenerateAnimatorFromJson(jsonPath);
        }
    }

    public static void GenerateAnimatorFromJson(string jsonPath)
    {
        // 读取 JSON 文件
        string jsonContent = File.ReadAllText(jsonPath);
        AnimatorConfig config = JsonUtility.FromJson<AnimatorConfig>(jsonContent);

        // 创建 Animator Controller
        AnimatorController animatorController = AnimatorController.CreateAnimatorControllerAtPath(config.animatorControllerPath);

        // 添加参数
        HashSet<string> addedParameters = new HashSet<string>();
        foreach (var stateConfig in config.states)
        {
            foreach (var transitionConfig in stateConfig.transitions)
            {
                if (!addedParameters.Contains(transitionConfig.parameterName))
                {
                    // 添加参数到动画控制器
                    animatorController.AddParameter(transitionConfig.parameterName, AnimatorControllerParameterType.Float);
                    addedParameters.Add(transitionConfig.parameterName);
                }
            }
        }

        // 添加状态和过渡
        Dictionary<string, AnimatorState> stateMap = new Dictionary<string, AnimatorState>();
        foreach (var stateConfig in config.states)
        {
            // 加载动画剪辑
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(stateConfig.animationClipPath);
            if (clip == null)
            {
                Debug.LogError($"Animation clip not found at path: {stateConfig.animationClipPath}");
                continue;
            }

            // 创建状态
            AnimatorState state = animatorController.layers[0].stateMachine.AddState(stateConfig.stateName);
            state.motion = clip;

            // 保存状态到字典中
            stateMap[stateConfig.stateName] = state;
        }

        // 添加状态转换
        foreach (var stateConfig in config.states)
        {
            if (!stateMap.TryGetValue(stateConfig.stateName, out var sourceState))
            {
                Debug.LogError($"State '{stateConfig.stateName}' not found in state map.");
                continue;
            }

            foreach (var transitionConfig in stateConfig.transitions)
            {
                if (!stateMap.TryGetValue(transitionConfig.targetStateName, out var targetState))
                {
                    Debug.LogError($"Target state '{transitionConfig.targetStateName}' not found for transition from '{stateConfig.stateName}'.");
                    continue;
                }

                // 创建状态转换
                AnimatorStateTransition transition = sourceState.AddTransition(targetState);
                transition.hasExitTime = stateConfig.hasExitTime;
                transition.exitTime = stateConfig.exitTime;

                // 添加条件
                transition.AddCondition(
                    ParseConditionMode(transitionConfig.conditionMode), // 条件模式
                    transitionConfig.threshold,                        // 阈值
                    transitionConfig.parameterName                     // 参数名称
                );
            }
        }

        Debug.Log($"Animator Controller generated at: {config.animatorControllerPath}");
    }

    private static AnimatorConditionMode ParseConditionMode(string conditionMode)
    {
        return conditionMode switch
        {
            "Greater" => AnimatorConditionMode.Greater,
            "Less" => AnimatorConditionMode.Less,
            "Equals" => AnimatorConditionMode.Equals,
            "NotEquals" => AnimatorConditionMode.NotEqual,
            _ => throw new System.ArgumentException($"Invalid condition mode: {conditionMode}")
        };
    }
}