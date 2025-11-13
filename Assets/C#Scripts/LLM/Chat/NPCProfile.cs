using UnityEngine;

/// <summary>
/// NPC配置文件，存储角色的重要信息
/// </summary>
[CreateAssetMenu(fileName = "NPCProfile", menuName = "ScriptableObjects/NPCProfile", order = 2)]
public class NPCProfile : ScriptableObject
{
    [Header("角色基本信息")]
    [Tooltip("NPC的唯一标识符，用于记忆管理")]
    public string npcId = "";

    [Tooltip("角色名称")]
    public string characterName = "NPC";

    [Header("角色背景")]
    [TextArea(5, 15)]
    [Tooltip("角色的背景故事、经历、身份等")]
    public string background = "";

    [Header("性格特征")]
    [TextArea(3, 10)]
    [Tooltip("角色的性格描述")]
    public string personality = "";

    [Header("对话风格")]
    [TextArea(3, 10)]
    [Tooltip("角色的说话方式、语气、用词习惯等")]
    public string speakingStyle = "";

    [Header("角色目标")]
    [TextArea(3, 8)]
    [Tooltip("角色的目标、动机、追求等")]
    public string goals = "";

    [Header("其他信息")]
    [TextArea(3, 8)]
    [Tooltip("其他需要AI知道的角色信息")]
    public string additionalInfo = "";

    [Header("LLM配置")]
    [Tooltip("该角色使用的LLM配置文件")]
    public LLMProfile llmProfile;
}

