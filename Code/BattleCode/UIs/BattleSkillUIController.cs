using CIW.Code;
using CIW.Code.System.Events;
using DG.Tweening;
using PSB.Code.BattleCode.Events;
using PSW.Code.Battle;
using PSW.Code.EventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YIS.Code.Defines;
using YIS.Code.Skills;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.UIs
{
    public class BattleSkillUIController : MonoBehaviour
    {
        [SerializeField] private AnimationClip chainEffectClip;
        [SerializeField] private SpriteData panelImages;
        [SerializeField] private Entity playerEntity;

        private bool[] _isSkillChains = new bool[3]; 
        private bool[] _isNextSkillChainNotAttacks = new bool[3];
        private List<PlayerSkillIcon> _playerSkillIconList = new List<PlayerSkillIcon>();

        private void OnEnable()
        {
            Bus<OnSkillExecutionStepEndEvent>.OnEvent += HandleSkillStepEnd;
            Bus<OnSetupSkillUIEvent>.OnEvent += HandleSetupIcons;
            Bus<OnSkillExecutionEndEvent>.OnEvent += HandleExecutionEnd;
            Bus<ChainingEvent>.OnEvent += OnChaining;
            Bus<AllChainingEvent>.OnEvent += AllChaining;
            Bus<OnAttackEvent>.OnEvent += OnAttack;
        }
        
        private void Start()
        {
            _playerSkillIconList = GetComponentsInChildren<PlayerSkillIcon>().ToList();
            for (int i = 0; i < _playerSkillIconList.Count; i++)
            {
                if (_playerSkillIconList[i] != null)
                    _playerSkillIconList[i].transform.gameObject.SetActive(false);
            }
        }
        
        private void OnDisable()
        {
            Bus<OnSkillExecutionStepEndEvent>.OnEvent -= HandleSkillStepEnd;
            Bus<OnSetupSkillUIEvent>.OnEvent -= HandleSetupIcons;
            Bus<OnSkillExecutionEndEvent>.OnEvent -= HandleExecutionEnd;
            Bus<ChainingEvent>.OnEvent -= OnChaining;
            Bus<AllChainingEvent>.OnEvent -= AllChaining;
            Bus<OnAttackEvent>.OnEvent -= OnAttack;
        }
        
        private void OnChaining(ChainingEvent evt)
        {
            if (_playerSkillIconList[evt.myIndex] != null)
            {
                _isSkillChains[evt.myIndex] = true;
            }
        }
        
        private void AllChaining(AllChainingEvent evt)
        {
            for(int i = 0; i < _playerSkillIconList.Count; i++)
            {
                if (_playerSkillIconList[i] != null)
                    _isSkillChains[i] = true;
            }
        }

        private void HandleSetupIcons(OnSetupSkillUIEvent evt)
        {
            Array.Clear(_isNextSkillChainNotAttacks, 0, _isNextSkillChainNotAttacks.Length);
            
            var skills = evt.SelectedSkills;
            for (int i = 0; i < _playerSkillIconList.Count; i++)
            {
                if (i < skills.Length && skills[i] != null)
                {
                    _playerSkillIconList[i].SetIconSprite(skills[i].visualData.icon);
                    _playerSkillIconList[i].SetIconAllKillTween();

                    _playerSkillIconList[i].transform.gameObject.SetActive(true);

                    _playerSkillIconList[i].IconlocalScale(Vector3.zero);
                    _playerSkillIconList[i].IconNotColorA();

                    _playerSkillIconList[i].IconScale(Vector3.one, 0.3f,Ease.OutBack);
                    _playerSkillIconList[i].IconFade(1f, 0.3f);
                    _playerSkillIconList[i].SetChainImage(1f, 0.3f);
                    _playerSkillIconList[i].SetLightOffsets(0, 0, 0, 0);
                    _playerSkillIconList[i].PlayChainEffect("Re" + chainEffectClip.name);

                    if (i + 1 < skills.Length && i < _playerSkillIconList.Count - 1 && skills[i] != null && skills[i+1] != null)
                    {
                        BaseSkill currentSkill = skills[i].skillPrefab.GetComponent<BaseSkill>();
                        BaseSkill nextSkill = skills[i + 1].skillPrefab.GetComponent<BaseSkill>();

                        if (currentSkill != null && nextSkill != null)
                        {
                            currentSkill.SetData(skills[i]);
                            currentSkill.Initialize();

                            nextSkill.SetData(skills[i + 1]);
                            nextSkill.Initialize();

                            BaseSkill afterNextSkill = null;
                            if (i + 2 < skills.Length && skills[i + 2] != null)
                            {
                                afterNextSkill = skills[i + 2].skillPrefab.GetComponent<BaseSkill>();
                                if (afterNextSkill != null)
                                {
                                    afterNextSkill.SetData(skills[i + 2]);
                                    afterNextSkill.Initialize();
                                }
                            }

                            _isNextSkillChainNotAttacks[i] = GetNextSkillIsChainNotAttack(currentSkill, nextSkill, afterNextSkill);
                        }
                        else
                        {
                            _isNextSkillChainNotAttacks[i] = false;
                        }
                    }
                }
                else
                {
                    _playerSkillIconList[i].transform.gameObject.SetActive(false);
                }
            }
        }
        
        private void OnAttack(OnAttackEvent evt)
        {
            PlayChainEffect(0);
        }

        private void HandleSkillStepEnd(OnSkillExecutionStepEndEvent evt)
        {
            if (evt.SlotIndex >= 0 && evt.SlotIndex < _playerSkillIconList.Count)
            {
                if (_playerSkillIconList[evt.SlotIndex] != null)
                {
                    _playerSkillIconList[evt.SlotIndex].SetIconAllKillTween();
                    _playerSkillIconList[evt.SlotIndex].IconFade(0.3f, 0.2f);
                    _playerSkillIconList[evt.SlotIndex].SetChainImage(0.3f, 0.2f);
                    _playerSkillIconList[evt.SlotIndex].IconPunchScale(new Vector3(-0.15f, -0.15f, 0f), 0.2f, 1, 0.5f, (() =>
                    {
                        PopDownSlot(evt.SlotIndex);
                    }));

                    if (evt.SlotIndex >= 2)
                        return;

                    PlayChainEffect(evt.SlotIndex + 1);
                }
            }
        }

        private void HandleExecutionEnd(OnSkillExecutionEndEvent evt)
        {
            for (int i = 0; i < _playerSkillIconList.Count; i++)
            {
                if (_playerSkillIconList[i] != null && _playerSkillIconList[i].gameObject.activeSelf)
                {
                    PlayerSkillIcon playerSkillIcon = _playerSkillIconList[i];

                    playerSkillIcon.SetIconAllKillTween();

                    playerSkillIcon.IconFade(0.3f, 0.2f);
                    playerSkillIcon.SetChainImage(0.3f, 0.2f);
                    int index = i;
                    playerSkillIcon.IconScale(Vector3.zero, 0.2f, Ease.InBack, (() =>
                    {
                        playerSkillIcon.SetOutLineSprite(panelImages.GetSprite(false));
                        playerSkillIcon.PlayChainEffect("Re" + chainEffectClip.name);
                        playerSkillIcon.transform.gameObject.SetActive(false);
                        _isSkillChains[index] = false;
                    }));
                }
            }
        }

        private async void PopDownSlot(int index)
        {
            if(index == 0)
                await Awaitable.WaitForSecondsAsync(0.2f);
            
            _playerSkillIconList[index].SetLightOffsets(0, 0, 0, 0);
        }

        private bool GetNextSkillIsChainNotAttack(BaseSkill currentSkill, BaseSkill nextSkill, BaseSkill afterNextSkill)
        {
            if (currentSkill == null || nextSkill == null || nextSkill.SkillData == null)
                return false;

            bool nextNeedsPrev = nextSkill.SkillData.checkSkillType.HasFlag(CheckType.Previous);
            bool nextNeedsNext = nextSkill.SkillData.checkSkillType.HasFlag(CheckType.Next);

            if (nextNeedsPrev == false)
                return false;

            if (nextSkill.CanChainPrev(currentSkill) == false)
                return false;

            if (nextNeedsNext)
            {
                if (afterNextSkill == null)
                    return false;

                if (nextSkill.CanChainNext(afterNextSkill) == false)
                    return false;
            }

            IReadOnlyList<Entity> tempEntities = new List<Entity>();

            IReadOnlyList<ISkillAction> actionList = nextSkill.SimulateSkill(true, playerEntity, tempEntities);
            
            if (actionList == null)
            {
                return false;
            }
            bool nextAttacksWhenChained = actionList.Any(action => action is DamageSkillAction);

            return nextAttacksWhenChained == false;
        }

        private async void PlayChainEffect(int index)
        {
            await Awaitable.WaitForSecondsAsync(0.2f);

            if (_isSkillChains[index])
            {
                _playerSkillIconList[index].PlayChainEffect(chainEffectClip.name);
                _playerSkillIconList[index].SetOutLineSprite(panelImages.GetSprite(true));
                _playerSkillIconList[index].SetLightOffsets(-75, -75, -75, -75);
            }

            if (_isNextSkillChainNotAttacks[index])
            {
                _playerSkillIconList[index + 1].PlayChainEffect(chainEffectClip.name);
                _playerSkillIconList[index + 1].SetOutLineSprite(panelImages.GetSprite(true));
                _playerSkillIconList[index + 1].SetLightOffsets(-75, -75, -75, -75);
                _isSkillChains[index + 1] = false;
            }
        }
        
    }
}
