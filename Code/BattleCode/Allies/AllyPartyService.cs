using System.Collections.Generic;
using UnityEngine;

namespace PSB.Code.BattleCode.Allies
{
    public sealed class AllyPartyService
    {
        private readonly AllyPartyRepository _repository;
        
        public AllyPartyService(AllyPartyRepository repository)
        {
            _repository = repository;
        }
        
        public IReadOnlyList<string> OwnedAllyIds => _repository != null ? _repository.OwnedAllyIds : EmptyIds;
        public IReadOnlyList<string> EquippedAllyIds => _repository != null ? _repository.EquippedAllyIds : EmptyIds;
        
        private static readonly IReadOnlyList<string> EmptyIds = new string[0];
        
        public bool Acquire(AllySO ally, bool autoEquip = true, bool reviveIfDead = true)
        {
            if (ally == null)
                return false;
            
            return Acquire(ally.AllyId, autoEquip, reviveIfDead);
        }
        
        public bool Acquire(string allyId, bool autoEquip = true, bool reviveIfDead = true)
        {
            if (_repository == null)
                return false;
            
            allyId = _repository.NormalizeAllyId(allyId);
            if (string.IsNullOrEmpty(allyId))
                return false;
            
            bool wasDead = _repository.IsDead(allyId);
            bool changed = _repository.AddOwnedAlly(allyId);
            
            if (reviveIfDead && (changed || wasDead))
                changed |= _repository.ClearHealth(allyId);
            
            if (autoEquip)
                changed |= TryAutoEquip(allyId);
            
            if (changed)
                _repository.RequestSave();
            
            return changed;
        }
        
        public AllyRewardResult ApplyAllyReward(AllySO ally, bool autoEquip = true, float duplicateHealPercent = 0.3f)
        {
            if (_repository == null || ally == null)
                return AllyRewardResult.None(ally);
            
            string allyId = _repository.NormalizeAllyId(ally.AllyId);
            if (string.IsNullOrEmpty(allyId))
                return AllyRewardResult.None(ally, allyId);
            
            duplicateHealPercent = Mathf.Clamp01(duplicateHealPercent);
            
            if (!_repository.IsOwned(allyId))
                return AcquireNewAllyReward(ally, allyId, autoEquip);
            
            bool autoEquipped = autoEquip && TryAutoEquip(allyId);
            bool hasHealth = _repository.TryGetHealth(allyId, out float currentHp, out float maxHp);
            
            if (hasHealth && currentHp <= 0f)
                return ReviveOwnedAllyReward(ally, allyId, maxHp, autoEquipped);
            
            if (hasHealth && currentHp < maxHp && duplicateHealPercent > 0f)
                return HealOwnedAllyReward(ally, allyId, currentHp, maxHp, duplicateHealPercent, autoEquipped);
            
            if (autoEquipped)
                _repository.RequestSave();
            
            return new AllyRewardResult(AllyRewardResultType.AlreadyFullHealth, ally, allyId,
                autoEquipped, autoEquipped, 0f, hasHealth ? currentHp : 0f, hasHealth ? maxHp : 0f);
        }
        
        public bool Equip(AllySO ally)
        {
            return ally != null && Equip(ally.AllyId);
        }
        
        public bool Equip(string allyId)
        {
            if (_repository == null)
                return false;
            
            allyId = _repository.NormalizeAllyId(allyId);
            if (!_repository.CanEquipAlly(allyId))
                return false;
            
            bool changed = _repository.AddEquippedAlly(allyId);
            if (changed)
                _repository.RequestSave();
            
            return changed;
        }
        
        public bool Unequip(AllySO ally)
        {
            return ally != null && Unequip(ally.AllyId);
        }
        
        public bool Unequip(string allyId)
        {
            if (_repository == null)
                return false;
            
            allyId = _repository.NormalizeAllyId(allyId);
            bool changed = _repository.RemoveEquippedAlly(allyId);
            if (changed)
                _repository.RequestSave();
            
            return changed;
        }
        
        public bool Dismiss(AllySO ally)
        {
            return ally != null && Dismiss(ally.AllyId);
        }
        
        public bool Dismiss(string allyId)
        {
            if (_repository == null)
                return false;
            
            allyId = _repository.NormalizeAllyId(allyId);
            bool changed = _repository.RemoveOwnedAlly(allyId);
            
            if (changed)
                _repository.RequestSave();
            
            return changed;
        }
        
        public bool IsOwned(AllySO ally)
        {
            return _repository != null && _repository.IsOwned(ally);
        }
        
        public bool IsOwned(string allyId)
        {
            return _repository != null && _repository.IsOwned(allyId);
        }
        
        public bool IsEquipped(AllySO ally)
        {
            return _repository != null && _repository.IsEquipped(ally);
        }
        
        public bool IsEquipped(string allyId)
        {
            return _repository != null && _repository.IsEquipped(allyId);
        }
        
        public bool IsDead(AllySO ally)
        {
            return _repository != null && _repository.IsDead(ally);
        }
        
        public bool IsDead(string allyId)
        {
            return _repository != null && _repository.IsDead(allyId);
        }
        
        public bool TryGetHealth(string allyId, out float currentHp, out float maxHp)
        {
            currentHp = 0f;
            maxHp = 0f;
            return _repository != null && _repository.TryGetHealth(allyId, out currentHp, out maxHp);
        }
        
        public void SaveHealth(string allyId, float currentHp, float maxHp)
        {
            if (_repository == null)
                return;
            
            bool changed = _repository.SetHealth(allyId, currentHp, maxHp);
            if (changed)
                _repository.RequestSave();
        }
        
        public void ClearHealth(string allyId)
        {
            if (_repository == null)
                return;
            
            bool changed = _repository.ClearHealth(allyId);
            if (changed)
                _repository.RequestSave();
        }
        
        public void ClearAllHealth()
        {
            if (_repository == null)
                return;
            
            bool changed = _repository.ClearAllHealth();
            if (changed)
                _repository.RequestSave();
        }
        
        private bool TryAutoEquip(string allyId)
        {
            if (!_repository.CanEquipAlly(allyId))
                return false;
            
            return _repository.AddEquippedAlly(allyId);
        }
        
        private AllyRewardResult AcquireNewAllyReward(AllySO ally, string allyId, bool autoEquip)
        {
            bool changed = _repository.AddOwnedAlly(allyId);
            changed |= _repository.ClearHealth(allyId);
            
            bool autoEquipped = autoEquip && TryAutoEquip(allyId);
            changed |= autoEquipped;
            
            if (changed)
                _repository.RequestSave();
            
            return new AllyRewardResult(AllyRewardResultType.Acquired, ally, allyId,
                changed, autoEquipped, 0f, 0f, 0f);
        }
        
        private AllyRewardResult ReviveOwnedAllyReward(AllySO ally, string allyId,
            float maxHp, bool autoEquipped)
        {
            float reviveHp = Mathf.Max(0f, maxHp);
            
            bool changed = autoEquipped;
            if (maxHp > 0f)
                changed |= _repository.SetHealth(allyId, reviveHp, maxHp);
            else
                changed |= _repository.ClearHealth(allyId);
            
            if (changed)
                _repository.RequestSave();
            
            return new AllyRewardResult(AllyRewardResultType.Revived, ally, allyId,
                changed, autoEquipped, reviveHp, reviveHp, maxHp);
        }
        
        private AllyRewardResult HealOwnedAllyReward(AllySO ally, string allyId, 
            float currentHp, float maxHp, float duplicateHealPercent, bool autoEquipped)
        {
            float beforeHp = Mathf.Clamp(currentHp, 0f, maxHp);
            float targetHp = Mathf.Clamp(beforeHp + maxHp * duplicateHealPercent, 0f, maxHp);
            float healAmount = Mathf.Max(0f, targetHp - beforeHp);
            
            bool changed = autoEquipped;
            if (healAmount > 0f)
                changed |= _repository.SetHealth(allyId, targetHp, maxHp);
            
            if (changed)
                _repository.RequestSave();
            
            return new AllyRewardResult(AllyRewardResultType.Healed, ally, allyId, 
                changed, autoEquipped, healAmount, targetHp, maxHp);
        }
        
    }
}
