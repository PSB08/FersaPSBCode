using System.Collections.Generic;
using PSB.Code.BattleCode.BattleSystems.BattlePhases;
using UnityEngine;
using Work.PSB.Code.FieldCode;

namespace Work.PSB.Code.RunSystem
{
    [CreateAssetMenu(fileName = "RunMap", menuName = "SO/Run/RunMap")]
    public class RunMapSO : ScriptableObject
    {
        public List<RunNodesSO> nodes;
        public EncounterPoolSO encounterPool;
        public RunEventSO[] eventPool;
        public SkillRewardPoolSO skillRewardPool;
        public BattlePresentationSO battlePresentation;
        public string battleSceneName = "BattleScene";
        public string runSceneName = "RunScene";
        public string titleSceneName = "SW_Title";
        
        public int MaxChapterCount => nodes != null ? nodes.Count : 0;
        
        public RunNodesSO GetChapterNodes(int chapterIndex)
        {
            if (nodes == null || chapterIndex < 0 || chapterIndex >= nodes.Count)
                return null;
            
            return nodes[chapterIndex];
        }
        
        public int GetStageCount(int chapterIndex)
        {
            RunNodesSO chapterNodes = GetChapterNodes(chapterIndex);
            return chapterNodes != null ? chapterNodes.Count : 0;
        }
        
        public int GetEnemyProgressionStage(int chapterIndex)
        {
            RunNodesSO chapterNodes = GetChapterNodes(chapterIndex);
            return chapterNodes != null ? chapterNodes.enemyProgressionStage : 0;
        }
        
        public bool HasChapter(int chapterIndex)
        {
            return GetStageCount(chapterIndex) > 0;
        }
        
        public RunNodeType GetNodeType(int chapterIndex, int nodeIndex)
        {
            RunNodesSO chapterNodes = GetChapterNodes(chapterIndex);
            return chapterNodes != null ? chapterNodes.GetNodeType(nodeIndex) : RunNodeType.Battle;
        }
        
        public RunNodeData GetNode(int chapterIndex, int nodeIndex)
        {
            RunNodesSO chapterNodes = GetChapterNodes(chapterIndex);
            return chapterNodes != null ? chapterNodes.GetNode(nodeIndex) : null;
        }
        
        public string GetChapterName(int chapterIndex)
        {
            RunNodesSO chapterNodes = GetChapterNodes(chapterIndex);
            if (chapterNodes != null && !string.IsNullOrWhiteSpace(chapterNodes.chapterName))
                return chapterNodes.chapterName;

            return $"Chapter {chapterIndex + 1}";
        }
        
        public BattleEncounterSO PickEncounter(int chapterIndex, int nodeIndex)
        {
            RunNodeData node = GetNode(chapterIndex, nodeIndex);
            BattleEncounterSO nodeEncounter = node != null ? node.PickEncounter() : null;
            if (nodeEncounter != null)
                return nodeEncounter;
            
            if (encounterPool == null)
                return null;
            
            RunNodeType nodeType = GetNodeType(chapterIndex, nodeIndex);
            if (nodeType == RunNodeType.Boss)
                return encounterPool.PickBoss();
            
            return encounterPool.PickBattle(nodeIndex, GetStageCount(chapterIndex) - 1);
        }
        
        public BattlePresentationSO PickPresentation(int chapterIndex, int nodeIndex)
        {
            RunNodeData node = GetNode(chapterIndex, nodeIndex);
            return node != null ? node.PickPresentation(battlePresentation) : battlePresentation;
        }
        
        public SkillRewardPoolSO GetSkillRewardPool(int chapterIndex, int nodeIndex)
        {
            RunNodeData node = GetNode(chapterIndex, nodeIndex);
            return node != null ? node.ResolveRewardPool(skillRewardPool) : skillRewardPool;
        }
        
        public RunEventSO PickEvent(int chapterIndex, int nodeIndex)
        {
            RunNodeData node = GetNode(chapterIndex, nodeIndex);
            RunEventSO nodeEvent = node != null ? node.PickEvent() : null;
            return nodeEvent != null ? nodeEvent : PickEvent();
        }
        
        public RunEventSO PickEvent()
        {
            if (eventPool == null || eventPool.Length == 0)
                return null;
            
            return eventPool[Random.Range(0, eventPool.Length)];
        }
        
    }
}
