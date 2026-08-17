using PSB.Code.BattleCode.Enemies.AttackCode;

namespace Work.PSB.Code.RunSystem
{
    public static class RunEnemyProgressionSync
    {
        public static void ApplyCurrentChapter()
        {
            RunMapSO map = RunStateStore.Map;
            if (map == null) return;

            int stage = map.GetEnemyProgressionStage(RunStateStore.ChapterIndex);
            EnemyProgressionManager.SetStage(stage);
        }
    }
}