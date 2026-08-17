using System;
using System.Collections;
using System.Collections.Generic;
using CIW.Code.Player.Combat;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Skills;
using YIS.Code.Skills.Modules;

namespace Work.PSB.Code.RunSystem
{
    public class RunBattleRewardUI : MonoBehaviour
    {
        [SerializeField] private GameObject rewardChoicesRoot;
        [SerializeField] private SkillRewardSlotUI[] slots;
        [SerializeField] private SkillContainer skillContainer;
        [SerializeField] private MonoBehaviour skillRewardReceiverBehaviour;
        
        private Action _onComplete;
        private SkillDataSO _selected;
        private ISkillRewardReceiver _skillRewardReceiver;
        private bool _isComplete;
        
        private void Awake()
        {
            _isComplete = true;
            CloseImmediate();
        }
        
        public IEnumerator ShowCoroutine(SkillRewardPoolSO rewardPool, Action onComplete)
        {
            yield return new WaitForSeconds(0.5f);
            
            _onComplete = onComplete;
            _selected = null;
            _isComplete = false;
            
            ResolveRewardTarget();
            gameObject.SetActive(true);
            
            if (rewardChoicesRoot != null)
                rewardChoicesRoot.SetActive(true);
            
            List<SkillDataSO> rewards = rewardPool != null
                ? rewardPool.Pick(3)
                : new List<SkillDataSO>();
            
            if (rewards.Count == 0 || slots == null || slots.Length == 0)
            {
                CompleteReward();
                yield break;
            }
            
            for (int i = 0; i < slots.Length; i++)
            {
                if (i < rewards.Count)
                    slots[i].Bind(rewards[i], SelectSkill);
                else if (slots[i] != null)
                    slots[i].gameObject.SetActive(false);
            }
        }
        
        //스킬을 선택하지 않고 현재 보상 단계를 완료
        public void SkipSkillReward()
        {
            CompleteReward();
        }
        
        private void SelectSkill(SkillDataSO skill)
        {
            if (_isComplete || _selected != null || skill == null)
                return;
            
            _selected = skill;
            
            bool received;
            if (_skillRewardReceiver != null)
            {
                received = _skillRewardReceiver.TryReceiveSkill(skill);
            }
            else
            {
                Bus<GiveSkillEvent>.Raise(new GiveSkillEvent(skill));
                ResolveRewardTarget();
                received = skillContainer != null && skillContainer.HasSkill(skill);
            }
            
            if (!received)
            {
                Debug.LogWarning($"Skill reward was not added: {skill.skillName}", this);
                _selected = null;
                return;
            }
            
            CompleteReward();
        }
        
        //스킬 선택, 스킵, 빈 보상 목록이 공통으로 사용하는 완료 처리
        private void CompleteReward()
        {
            if (_isComplete)
                return;
            
            _isComplete = true;
            LockRewardSlots();
            CloseImmediate();
            
            Action onComplete = _onComplete;
            _onComplete = null;
            onComplete?.Invoke();
        }
        
        private void LockRewardSlots()
        {
            if (slots == null)
                return;
            
            foreach (SkillRewardSlotUI slot in slots)
            {
                if (slot != null)
                    slot.Lock();
            }
        }
        
        private void CloseImmediate()
        {
            gameObject.SetActive(false);
            
            if (rewardChoicesRoot != null)
                rewardChoicesRoot.SetActive(false);
        }
        
        private void ResolveRewardTarget()
        {
            if (skillContainer == null)
                skillContainer = FindAnyObjectByType<SkillContainer>(FindObjectsInactive.Include);
            
            if (skillRewardReceiverBehaviour == null)
                return;
            
            _skillRewardReceiver = skillRewardReceiverBehaviour as ISkillRewardReceiver;
        }
        
    }
}