using System.Collections.Generic;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.Players;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.UIs
{
    public class BuffIconPresenter : MonoBehaviour
    {
        [SerializeField] private ModuleOwner myTarget;
        [SerializeField] private Transform iconRoot;
        [SerializeField] private BuffIconView iconPrefab;

        private readonly List<BuffIconView> _spawnedIcons = new();

        private void Awake()
        {
            if (iconRoot == null)
                iconRoot = transform;
        }

        private void OnEnable()  => Bus<BuffUiEvent>.OnEvent += OnBuffUiEvent;
        private void OnDisable() => Bus<BuffUiEvent>.OnEvent -= OnBuffUiEvent;

        private void OnBuffUiEvent(BuffUiEvent evt)
        {
            if (evt.Target == null) return;
            if (!ReferenceEquals(evt.Target, myTarget)) return;

            SyncBuffs(evt.Target);
        }

        private void SyncBuffs(ModuleOwner target)
        {
            ClearAll();

            BuffModule buffModule = target.GetModule<BuffModule>();
            if (buffModule == null) return;

            bool isLeft = target is BattlePlayer;

            var activeBuffs = buffModule.GetActiveBuffs();
            
            Dictionary<BuffVisualSO, int> groupedBuffs = new Dictionary<BuffVisualSO, int>();

            foreach (var buff in activeBuffs)
            {
                if (buff.so == null) continue;

                if (groupedBuffs.TryGetValue(buff.so, out int currentMaxTurn))
                {
                    groupedBuffs[buff.so] = Mathf.Max(currentMaxTurn, buff.remainingTurn);
                }
                else
                {
                    groupedBuffs[buff.so] = buff.remainingTurn;
                }
            }

            foreach (var kvp in groupedBuffs)
            {
                var so = kvp.Key;
                var maxTurn = kvp.Value;

                var view = Instantiate(iconPrefab, iconRoot);
                view.Set(so, maxTurn, target, isLeft);
                
                _spawnedIcons.Add(view);
            }
        }

        public void ClearAll()
        {
            foreach (var view in _spawnedIcons)
            {
                if (view != null) 
                    Destroy(view.gameObject);
            }
            
            _spawnedIcons.Clear();
        }
        
    }
}