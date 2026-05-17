using DG.Tweening;
using PSB_Lib.Dependencies;
using PSB.Code.BattleCode.Items;
using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Defines;
using YIS.Code.Items;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Enemies
{
    public class ItemDropper : MonoBehaviour, IModule
    {
        [SerializeField] private DropTableSO dropTable;
        
        [SerializeField] private int maxVisualSpawnForItems = 5;
        [SerializeField] private int maxVisualSpawnForCurrency = 1;

        public void Initialize(ModuleOwner owner)
        {
        }
        
        public void SetDropTable(DropTableSO table)
        {
            dropTable = table;
        }
        
        public void DropItem()
        {
            if (dropTable == null || dropTable.entries == null || dropTable.entries.Length == 0)
                return;
            
            DropResultTable resultTable = new DropResultTable();

            foreach (var entry in dropTable.entries)
            {
                if (entry.item == null) 
                    continue;
                
                if (Random.value > entry.dropRate)
                    continue;
                
                int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);

                if (amount <= 0)
                    continue;

                resultTable.Add(entry.item, amount);
                
                SpawnDropVisual(entry.item, amount);
            }
            Bus<ItemDropped>.Raise(new ItemDropped().Init(resultTable));
        }

        private void SpawnDropVisual(ItemDataSO item, int amount)
        {
            if (item.itemPrefab == null) 
                return;

            bool isCurrency = IsCurrency(item.itemType);

            int maxSpawn = isCurrency ? maxVisualSpawnForCurrency : maxVisualSpawnForItems;
            int spawnCount = Mathf.Clamp(amount, 1, maxSpawn);

            for (int i = 0; i < spawnCount; i++)
                CreateDropObject(item.itemPrefab, transform.position);
        }
        
        public void DropItemAt(Vector3 worldPos, LootApplyMode applyMode)
        {
            if (dropTable == null || dropTable.entries == null || dropTable.entries.Length == 0)
                return;

            DropResultTable resultTable = new DropResultTable();

            foreach (var entry in dropTable.entries)
            {
                if (entry.item == null) continue;
                if (Random.value > entry.dropRate) continue;

                int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                if (amount <= 0) continue;

                resultTable.Add(entry.item, amount);
                SpawnDropVisualAt(entry.item, amount, worldPos);
            }

            Bus<ItemDropped>.Raise(new ItemDropped().Init(resultTable, applyMode));
        }

        private void SpawnDropVisualAt(ItemDataSO item, int amount, Vector3 worldPos)
        {
            if (item.itemPrefab == null) return;

            bool isCurrency = IsCurrency(item.itemType);
            int maxSpawn = isCurrency ? maxVisualSpawnForCurrency : maxVisualSpawnForItems;
            int spawnCount = Mathf.Clamp(amount, 1, maxSpawn);

            for (int i = 0; i < spawnCount; i++)
                CreateDropObject(item.itemPrefab, worldPos);
        }

        private void CreateDropObject(GameObject prefab, Vector3 worldPos)
        {
            Vector3 startPos = new Vector3(worldPos.x, worldPos.y - 1.5f, worldPos.z);
            GameObject obj = Instantiate(prefab, startPos, Quaternion.identity);

            if (Injector.Instance != null)
                Injector.Instance.InjectTo(obj);

            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.y < 0) dir.y *= -1f;
            float distance = Random.Range(0.6f, 1.5f);
            Vector3 endPos = startPos + new Vector3(dir.x, dir.y, 0f) * distance;

            //obj.transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            seq.Append(obj.transform.DOScale(obj.transform.localScale, 0.15f).SetEase(Ease.OutBack));
            seq.Append(obj.transform.DOJump(endPos, 1.0f, 1, 0.45f).SetEase(Ease.OutQuad));
            seq.Append(obj.transform.DOScale(obj.transform.localScale - new Vector3(0.1f, 0.1f, 0.1f), 0.08f));
            seq.Append(obj.transform.DOScale(obj.transform.localScale, 0.08f));
        }

        private static bool IsCurrency(ItemType type)
        {
            return type == ItemType.Coin
                   || type == ItemType.PP
                   || type == ItemType.BossCoin;
        }
        
    }
}