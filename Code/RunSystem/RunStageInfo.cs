using System;

namespace Work.PSB.Code.RunSystem
{
    [Serializable]
    public readonly struct RunStageInfo
    {
        public readonly bool HasStage;
        public readonly RunPhase Phase;
        public readonly string PhaseLabel;
        public readonly RunNodeType NodeType;
        public readonly string NodeTypeLabel;
        public readonly string ChapterName;
        
        public readonly int ChapterIndex;
        public readonly int ChapterNumber;
        public readonly int MaxChapterNumber;
        public readonly int NodeIndex;
        public readonly int StageNumber;
        public readonly int MaxStageNumber;
        
        public readonly string StageLabel;
        public readonly string FullLabel;
        
        public readonly bool IsReadyToStart;
        public readonly bool IsRunning;
        public readonly bool IsWaitingNext;
        public readonly bool IsFinished;
        public readonly bool IsBattleNode;
        
        public static RunStageInfo Empty => new RunStageInfo(false, RunPhase.None,
            RunNodeType.Battle, string.Empty, -1, -1, 0, 0, 0, 0);
        
        public RunStageInfo(bool hasStage, RunPhase phase, RunNodeType nodeType,
            string chapterName, int chapterIndex, int nodeIndex, int chapterNumber,
            int stageNumber, int maxChapterNumber, int maxStageNumber)
        {
            HasStage = hasStage;
            Phase = phase;
            PhaseLabel = GetPhaseLabel(phase);
            NodeType = nodeType;
            NodeTypeLabel = hasStage ? GetNodeTypeLabel(nodeType) : string.Empty;
            ChapterName = hasStage ? chapterName ?? string.Empty : string.Empty;
            
            ChapterIndex = chapterIndex;
            ChapterNumber = chapterNumber;
            MaxChapterNumber = maxChapterNumber;
            NodeIndex = nodeIndex;
            StageNumber = stageNumber;
            MaxStageNumber = maxStageNumber;
            
            StageLabel = hasStage ? $"{chapterNumber} - {stageNumber}" : string.Empty;
            FullLabel = hasStage ? BuildFullLabel(ChapterName, StageLabel, NodeTypeLabel) : string.Empty;
            
            IsReadyToStart = hasStage && phase == RunPhase.ReadyToStart;
            IsRunning = hasStage && phase == RunPhase.Running;
            IsWaitingNext = hasStage && phase == RunPhase.WaitingNext;
            IsFinished = hasStage && phase == RunPhase.Finished;
            IsBattleNode = hasStage && (nodeType == RunNodeType.Battle || nodeType == RunNodeType.Boss);
        }

        private static string BuildFullLabel(string chapterName, string stageLabel, string nodeTypeLabel)
        {
            if (string.IsNullOrWhiteSpace(chapterName))
                return $"{stageLabel} {nodeTypeLabel}";

            return $"{chapterName} {stageLabel} {nodeTypeLabel}";
        }
        
        public static string GetNodeTypeLabel(RunNodeType nodeType)
        {
            switch (nodeType)
            {
                case RunNodeType.Battle:
                    return "\uC804\uD22C";
                case RunNodeType.Event:
                    return "\uC0AC\uAC74";
                case RunNodeType.Rest:
                    return "\uD734\uC2DD";
                case RunNodeType.Boss:
                    return "\uBCF4\uC2A4";
                default:
                    return string.Empty;
            }
        }
        
        public static string GetPhaseLabel(RunPhase phase)
        {
            switch (phase)
            {
                case RunPhase.ReadyToStart:
                    return "\uC2DC\uC791 \uB300\uAE30";
                case RunPhase.Running:
                    return "\uC9C4\uD589 \uC911";
                case RunPhase.WaitingNext:
                    return "\uB2E4\uC74C \uB300\uAE30";
                case RunPhase.Finished:
                    return "\uC644\uB8CC";
                default:
                    return string.Empty;
            }
        }
        
    }
}
