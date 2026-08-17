using System;
using System.Collections.Generic;
using PSB.Code.BattleCode.BattleSystems.BattlePhases;
using UnityEngine;
using Work.PSB.Code.FieldCode;
using Random = UnityEngine.Random;

namespace Work.PSB.Code.RunSystem
{
    [Serializable]
    public class RunNodeData
    {
        public RunNodeType nodeType = RunNodeType.Battle;
        
        [Header("Battle")]
        public BattleEncounterSO[] encounterCandidates;
        public BattlePresentationSO[] presentationCandidates;
        public SkillRewardPoolSO rewardPoolOverride;
        
        [Header("Event")]
        public RunEventSO[] eventCandidates;
        
        public BattleEncounterSO PickEncounter()
        {
            return Pick(encounterCandidates);
        }
        
        public BattlePresentationSO PickPresentation(BattlePresentationSO fallback)
        {
            BattlePresentationSO picked = Pick(presentationCandidates);
            return picked != null ? picked : fallback;
        }
        
        public RunEventSO PickEvent()
        {
            return Pick(eventCandidates);
        }
        
        public SkillRewardPoolSO ResolveRewardPool(SkillRewardPoolSO fallback)
        {
            return rewardPoolOverride != null ? rewardPoolOverride : fallback;
        }
        
        private static T Pick<T>(T[] candidates) where T : UnityEngine.Object
        {
            if (candidates == null || candidates.Length == 0)
                return null;
            
            int validCount = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null)
                    validCount++;
            }
            
            if (validCount <= 0)
                return null;
            
            int targetIndex = Random.Range(0, validCount);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] == null) 
                    continue;
                
                if (targetIndex == 0) 
                    return candidates[i];
                
                targetIndex--;
            }
            
            return null;
        }
    }
    
    [CreateAssetMenu(fileName = "RunChapter", menuName = "SO/Run/Chapter", order = 0)]
    public class RunNodesSO : ScriptableObject
    {
        public string chapterName;
        public List<RunNodeData> nodes = CreateDefaultNodes();
        public int enemyProgressionStage;
        
        public int Count => nodes != null ? nodes.Count : 0;
        
        public RunNodeData GetNode(int nodeIndex)
        {
            if (nodes == null || nodeIndex < 0 || nodeIndex >= nodes.Count)
                return null;
            
            return nodes[nodeIndex];
        }
        
        public RunNodeType GetNodeType(int nodeIndex)
        {
            RunNodeData node = GetNode(nodeIndex);
            return node != null ? node.nodeType : RunNodeType.Battle;
        }
        
        [ContextMenu("Apply Default Route")]
        private void ApplyDefaultRoute()
        {
            nodes = CreateDefaultNodes();
        }
        
        private void Reset()
        {
            ApplyDefaultRoute();
        }
        
        private void OnValidate()
        {
            if (nodes == null)
                nodes = CreateDefaultNodes();
            
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                    nodes[i] = new RunNodeData();
            }
        }
        
        private static List<RunNodeData> CreateDefaultNodes()
        {
            return new List<RunNodeData>
            {
                new RunNodeData { nodeType = RunNodeType.Battle },
                new RunNodeData { nodeType = RunNodeType.Battle },
                new RunNodeData { nodeType = RunNodeType.Event },
                new RunNodeData { nodeType = RunNodeType.Battle },
                new RunNodeData { nodeType = RunNodeType.Battle },
                new RunNodeData { nodeType = RunNodeType.Battle },
                new RunNodeData { nodeType = RunNodeType.Event },
                new RunNodeData { nodeType = RunNodeType.Battle },
                new RunNodeData { nodeType = RunNodeType.Rest },
                new RunNodeData { nodeType = RunNodeType.Boss },
            };
        }
        
    }
}
