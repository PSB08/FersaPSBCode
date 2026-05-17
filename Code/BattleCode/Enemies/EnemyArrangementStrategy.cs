using System.Collections.Generic;
using PSB.Code.BattleCode.Enums;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies
{
    public static class EnemyArrangementStrategy
    {
        public static void SortEnemiesList(List<BattleEnemy> enemies)
        {
            Dictionary<BattleEnemy, int> originIndex = new Dictionary<BattleEnemy, int>();
            for (int i = 0; i < enemies.Count; i++)
                originIndex[enemies[i]] = i;

            enemies.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;

                int priA = GetPlacementPriority(a.enemySO.grade);
                int priB = GetPlacementPriority(b.enemySO.grade);

                int compare = priA.CompareTo(priB);
                if (compare != 0) return compare;

                return originIndex[a].CompareTo(originIndex[b]);
            });

            int bossIndex = enemies.FindIndex(e => e != null && e.enemySO.grade == EnemyGrade.Boss);
            if (bossIndex >= 0)
            {
                BattleEnemy boss = enemies[bossIndex];
                enemies.RemoveAt(bossIndex);

                int cnt = enemies.Count + 1;
                int insertCnt = cnt / 2;

                enemies.Insert(insertCnt, boss);
            }
        }

        private static int GetPlacementPriority(EnemyGrade grade)
        {
            return grade switch
            {
                EnemyGrade.Common => 0,
                EnemyGrade.MiniBoss => 1,
                EnemyGrade.Boss => 2,
                EnemyGrade.Elite => 3,
                _ => 1
            };
        }

        public static List<Vector3> ComputePositions(List<BattleEnemy> enemies, int columns, Vector2 startPosition, Vector2 cellSize, BatchSize batchSize)
        {
            if (enemies.Count <= 0) return null;

            List<Vector3> positions = new List<Vector3>(enemies.Count);
            for (int i = 0; i < enemies.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                float yOffset = (i == 1 || i == 3) ? cellSize.y : 0f;

                Vector3 pos = new Vector3(startPosition.x + col * cellSize.x, 
                    startPosition.y - row * cellSize.y + yOffset, 0f);

                positions.Add(pos);
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

        public static void ApplySortingOrders(List<BattleEnemy> enemies)
        {
            const int frontBase = 20;
            const int backBase = 10;

            for (int i = 0; i < enemies.Count; i++)
            {
                BattleEnemy enemy = enemies[i];
                if (enemy == null) continue;

                bool isFront = i % 2 == 0;
                int baseOrder = isFront ? frontBase : backBase;
                int order = baseOrder + i;

                SpriteRenderer[] renderers = enemy.GetComponentsInChildren<SpriteRenderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                {
                    renderers[r].sortingOrder = order;
                }
            }
        }
        
    }
}