using System;
using UnityEngine;

namespace Work.PSB.Code.RunSystem
{
    public static class RunStateStore
    {
        public static event Action Changed;
        public static event Action<RunStageInfo> StageInfoChanged;
        
        public static RunPhase Phase { get; private set; } = RunPhase.None;
        public static RunMapSO Map { get; private set; }
        public static int ChapterIndex { get; private set; }
        public static int NodeIndex { get; private set; }
        public static int RestVisitCount { get; private set; }
        public static RunEventSO CurrentEvent { get; private set; }
        public static RunEventPhase EventPhase { get; private set; } = RunEventPhase.None;
        public static RunEventBattleResult EventBattleResult { get; private set; } = RunEventBattleResult.None;
        
        public static bool HasMap => Map != null && Map.MaxChapterCount > 0 && Map.HasChapter(ChapterIndex);
        public static bool IsReadyToStart => Phase == RunPhase.ReadyToStart && HasMap;
        public static bool IsRunning => Phase == RunPhase.Running && HasMap;
        public static bool IsWaitingNext => Phase == RunPhase.WaitingNext && HasMap;
        public static RunNodeType CurrentType => HasMap ? Map.GetNodeType(ChapterIndex, NodeIndex) : RunNodeType.Battle;
        public static int ChapterNumber => HasMap ? ChapterIndex + 1 : 0;
        public static int StageNumber => HasMap ? Mathf.Clamp(NodeIndex + 1, 1, CurrentStageCount) : 0;
        public static int MaxChapterNumber => Map != null ? Map.MaxChapterCount : 0;
        public static int MaxStageNumber => HasMap ? CurrentStageCount : 0;
        public static string StageLabel => HasMap ? $"{ChapterNumber} - {StageNumber}" : string.Empty;
        public static int CurrentStageCount => Map != null ? Map.GetStageCount(ChapterIndex) : 0;
        
        public static RunStageInfo CurrentInfo
        {
            get
            {
                if (!HasMap)
                    return RunStageInfo.Empty;
                
                return new RunStageInfo(true, Phase, CurrentType,
                    Map.GetChapterName(ChapterIndex), ChapterIndex, NodeIndex, ChapterNumber,
                    StageNumber, MaxChapterNumber, MaxStageNumber);
            }
        }
        
        public static void PrepareRun(RunMapSO map)
        {
            Map = map;
            ChapterIndex = 0;
            NodeIndex = 0;
            RestVisitCount = 0;
            ResetEventState();
            Phase = HasMap ? RunPhase.ReadyToStart : RunPhase.None;
            NotifyChanged();
        }
        
        public static void StartCurrentNode()
        {
            if (!HasMap)
                return;
            
            Phase = RunPhase.Running;
            NotifyChanged();
        }
        
        public static void BeginEvent(RunEventSO data)
        {
            CurrentEvent = data;
            EventPhase = data != null && data.HasBattle ? RunEventPhase.BeforeBattle : RunEventPhase.None;
            EventBattleResult = RunEventBattleResult.None;
            RunEventActionRuntimeStore.ClearAll();
            
            NotifyChanged();
        }
        
        public static void EnterEventBattle()
        {
            if (CurrentEvent == null || !CurrentEvent.HasBattle)
                return;
            
            EventPhase = RunEventPhase.InBattle;
            NotifyChanged();
        }
        
        public static void CompleteEventBattle(RunEventBattleResult result = RunEventBattleResult.Success)
        {
            if (CurrentEvent == null || !CurrentEvent.HasBattle)
                return;
            
            EventBattleResult = result == RunEventBattleResult.None
                ? RunEventBattleResult.Success : result;
            EventPhase = RunEventPhase.AfterVictory;
            Phase = RunPhase.Running;
            NotifyChanged();
        }
        
        public static void SetEventBattleResult(RunEventBattleResult result)
        {
            if (CurrentEvent == null || result == RunEventBattleResult.None)
                return;
            
            EventBattleResult = result;
            NotifyChanged();
        }
        
        public static void CompleteEvent()
        {
            ResetEventState();
            WaitNext();
        }
        
        public static void WaitNext()
        {
            if (!HasMap)
                return;
            
            Phase = RunPhase.WaitingNext;
            NotifyChanged();
        }
        
        public static RunAdvanceResult AdvanceToNextNode(bool notify = true)
        {
            if (!HasMap)
                return RunAdvanceResult.None;
            
            int currentStageCount = CurrentStageCount;
            
            if (CurrentType == RunNodeType.Rest)
                RestVisitCount++;
            
            ResetEventState();
            NodeIndex++;
            
            if (NodeIndex < currentStageCount)
            {
                Phase = RunPhase.Running;
                if (notify)
                    NotifyChanged();
                
                return RunAdvanceResult.NextNode;
            }
            
            int nextChapterIndex = ChapterIndex + 1;
            while (Map != null && nextChapterIndex < MaxChapterNumber && !Map.HasChapter(nextChapterIndex))
                nextChapterIndex++;
            
            if (nextChapterIndex < MaxChapterNumber)
            {
                ChapterIndex = nextChapterIndex;
                NodeIndex = 0;
                Phase = RunPhase.ReadyToStart;
                if (notify)
                    NotifyChanged();
                
                return RunAdvanceResult.NextChapter;
            }
            
            NodeIndex = Mathf.Max(0, currentStageCount - 1);
            Phase = RunPhase.Finished;
            if (notify)
                NotifyChanged();
            
            return RunAdvanceResult.Finished;
        }
        
        public static string GetStageLabel(string separator)
        {
            return HasMap ? $"{ChapterNumber}{separator}{StageNumber}" : string.Empty;
        }
        
        public static float CurrentRestHealPercent()
            => 0.2f + RestVisitCount * 0.05f;
        
        public static void Clear()
        {
            Phase = RunPhase.None;
            Map = null;
            ChapterIndex = 0;
            NodeIndex = 0;
            RestVisitCount = 0;
            ResetEventState();
            NotifyChanged();
        }
        
        private static void ResetEventState()
        {
            CurrentEvent = null;
            EventPhase = RunEventPhase.None;
            EventBattleResult = RunEventBattleResult.None;
            RunEventActionRuntimeStore.ClearAll();
        }
        
        private static void NotifyChanged()
        {
            RunStageInfo currentInfo = CurrentInfo;
            Changed?.Invoke();
            StageInfoChanged?.Invoke(currentInfo);
        }
        
    }
    
}
