using System.Collections.Generic;
using PSB.Code.BattleCode.Enums;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies
{
    public static class EnemyArrangementStrategy
    {
        public static void SortEnemiesList(List<BattleEnemy> enemies)
        {
            if (enemies == null)
                return;

            Dictionary<BattleEnemy, int> originIndex = new Dictionary<BattleEnemy, int>();
            for (int i = 0; i < enemies.Count; i++)
                originIndex[enemies[i]] = i;

            enemies.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;

                int priA = GetGradePriority(a);
                int priB = GetGradePriority(b);

                int compare = priB.CompareTo(priA);
                if (compare != 0) return compare;

                return originIndex[a].CompareTo(originIndex[b]);
            });
        }

        private static int GetGradePriority(BattleEnemy enemy)
        {
            if (enemy == null || enemy.enemySO == null)
                return -1;

            EnemyGrade grade = enemy.enemySO.grade;
            return grade switch
            {
                EnemyGrade.Common => 0,
                EnemyGrade.Elite => 1,
                EnemyGrade.MiniBoss => 2,
                EnemyGrade.Boss => 3,
                _ => 0
            };
        }

        public static List<Vector3> ComputePositions(List<BattleEnemy> enemies, int maxActiveEnemyCount, Vector2 startPosition, Vector2 cellSize, float backLineExtraX, BatchSize batchSize)
        {
            if (enemies.Count <= 0) return null;

            List<Vector3> positions = new List<Vector3>(enemies.Count);
            int maxSlotIndex = Mathf.Max(0, Mathf.Min(maxActiveEnemyCount, 3) - 1);
            for (int i = 0; i < enemies.Count; i++)
            {
                int slotIndex = Mathf.Clamp(i, 0, maxSlotIndex);
                positions.Add(GetThreeEnemySlotPosition(slotIndex, startPosition, cellSize, backLineExtraX));
            }

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (Vector3 p in positions)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }

            float width = maxX - minX;
            float height = maxY - minY;

            float targetMinX = batchSize.startSize.x;
            float targetMinY = batchSize.startSize.y;
            float targetMaxX = batchSize.endSize.x;
            float targetMaxY = batchSize.endSize.y;

            float targetWidth = targetMaxX - targetMinX;
            float targetHeight = targetMaxY - targetMinY;

            float scaleX = targetWidth / Mathf.Max(width, 0.01f);
            float scaleY = targetHeight / Mathf.Max(height, 0.01f);
            float scale = Mathf.Min(scaleX, scaleY, 1f);

            List<Vector3> result = new List<Vector3>(enemies.Count);
            for (int i = 0; i < enemies.Count; i++)
            {
                Vector3 p = positions[i];
                Vector3 scaled = startPosition + (Vector2)(p - (Vector3)startPosition) * scale;

                scaled.x = Mathf.Clamp(scaled.x, targetMinX, targetMaxX);
                scaled.y = Mathf.Clamp(scaled.y, targetMinY, targetMaxY);

                result.Add(scaled);
            }

            return result;
        }

        private static Vector3 GetThreeEnemySlotPosition(int index, Vector2 startPosition, Vector2 cellSize, float backLineExtraX)
        {
            float backLineX = cellSize.x + backLineExtraX;
            Vector2 offset = index switch
            {
                0 => Vector2.zero,
                1 => new Vector2(backLineX, cellSize.y),
                2 => new Vector2(backLineX, -cellSize.y),
                _ => new Vector2(backLineX, -cellSize.y)
            };

            Vector2 pos = startPosition + offset;
            return new Vector3(pos.x, pos.y, 0f);
        }

        public static void ApplySortingOrders(List<BattleEnemy> enemies)
        {
            int[] ordersBySlot = { 20, 10, 30 };

            for (int i = 0; i < enemies.Count; i++)
            {
                BattleEnemy enemy = enemies[i];
                if (enemy == null) continue;

                int order = i < ordersBySlot.Length ? ordersBySlot[i] : 20 + i;

                SpriteRenderer[] renderers = enemy.GetComponentsInChildren<SpriteRenderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                {
                    renderers[r].sortingOrder = order;
                }
            }
        }
        
    }
}
