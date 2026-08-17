using PSB_Lib.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using PSB.Code.BattleCode.Allies;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Players;
using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSW.Code.Deck;
using PSW.Code.EventBus;
using UnityEngine;
using Work.CSH.Scripts.PlayerComponents;
using Work.CSH.Scripts.Relics;
using YIS.Code.Defines;
using YIS.Code.Events;
using YIS.Code.Items;
using YIS.Code.Skills;
using YIS.Code.Skills.Modules;

namespace Work.PSB.Code.RunSystem
{
    public class RunEventActionRunner : MonoBehaviour
    {
        [SerializeField] private RunEventSelectionPanel selectionPanel;
        [SerializeField] private DecksListSO decksList;
        [SerializeField] private AllyDatabaseSO allyDatabase;
        
        private InventoryCode _inventory;
        private SkillContainer _skillContainer;
        private RelicContainer _relicContainer;
        private EntityHealth _playerHealth;
        private bool _isRunning;
        
        public bool IsRunning => _isRunning;
        
        [Inject] private AllyPartyRepository _allyRepository;
        [Inject] private PlayerManager _playerManager;
        
        public static RunEventActionRunner FindRunner()
        {
            return FindAnyObjectByType<RunEventActionRunner>(FindObjectsInactive.Include);
        }
        
        public bool CanBegin(RunEventActionSO action)
        {
            if (action == null || _isRunning)
                return false;
            
            ResolveDependencies();
            
            if (!CanPayCosts(action.costs))
                return false;
            
            if (action is RunEventTransactionSO transaction)
                return CanBeginTransaction(transaction);
            
            if (action is RunEventDoubleOrNothingSO doubleOrNothing)
                return CanBeginDoubleOrNothing(doubleOrNothing);
            
            return action is RunEventChanceSO;
        }
        
        public bool Execute(RunEventActionSO action, Action<RunEventActionResult> onCompleted)
        {
            if (!CanBegin(action))
                return false;
            
            _isRunning = true;
            
            if (action is RunEventTransactionSO transaction)
            {
                ExecuteTransaction(transaction, onCompleted);
                return true;
            }
            
            RunEventActionResult result = action is RunEventChanceSO chance
                ? ExecuteChance(chance) : ExecuteDoubleOrNothing(action as RunEventDoubleOrNothingSO);
            
            _isRunning = false;
            onCompleted?.Invoke(result);
            return true;
        }
        
        private bool CanBeginTransaction(RunEventTransactionSO transaction)
        {
            RunEventSelectionSettings settings = transaction.selection;
            if (settings == null || settings.source == RunEventSelectionSource.None)
                return true;
            
            if (selectionPanel == null || !selectionPanel.IsConfigured)
                return false;
            
            List<RunEventSelectionCandidate> candidates = BuildCandidates(settings);
            if (candidates.Count < settings.MinimumCount)
                return false;
            
            return CanPotentiallySatisfySelection(settings, candidates);
        }
        
        private bool CanBeginDoubleOrNothing(RunEventDoubleOrNothingSO action)
        {
            if (action == null)
                return false;
            
            bool hasPot = RunEventActionRuntimeStore.TryGetInt(action.StateKey, out int pot) && pot > 0;
            
            if (action.operation == RunEventDoubleOrNothingOperation.CashOut)
                return hasPot;
            
            return hasPot || CurrencyContainer.Has(action.currencyType, Mathf.Max(1, action.startingAmount));
        }
        
        private void ExecuteTransaction(RunEventTransactionSO transaction,
            Action<RunEventActionResult> onCompleted)
        {
            RunEventSelectionSettings settings = transaction.selection;
            if (settings == null || settings.source == RunEventSelectionSource.None)
            {
                RunEventActionResult immediateResult = CompleteTransaction(transaction,
                    Array.Empty<RunEventSelectionCandidate>());
                _isRunning = false;
                onCompleted?.Invoke(immediateResult);
                return;
            }
            
            List<RunEventSelectionCandidate> candidates = BuildCandidates(settings);
            selectionPanel.Open(settings, candidates,
                selected => ValidateSelection(settings, selected),
                selected =>
                {
                    RunEventActionResult result = CompleteTransaction(transaction, selected);
                    _isRunning = false;
                    onCompleted?.Invoke(result);
                },
                () =>
                {
                    _isRunning = false;
                    onCompleted?.Invoke(RunEventActionResult.Cancelled());
                });
        }
        
        private RunEventActionResult CompleteTransaction(RunEventTransactionSO transaction,
            IReadOnlyList<RunEventSelectionCandidate> selected)
        {
            ResolveDependencies();
            
            if (!CanPayCosts(transaction.costs) || !ValidateSelection(transaction.selection, selected) ||
                !CanPayCombinedCosts(transaction.costs, transaction.selection, selected))
                return RunEventActionResult.Cancelled();
            
            if (!PayCosts(transaction.costs))
                return RunEventActionResult.Cancelled();
            
            if (transaction.selection != null && transaction.selection.consumeSelected &&
                !ConsumeSelection(transaction.selection, selected))
            {
                Debug.LogError("Run event selection changed while its cost was being consumed.", this);
                return RunEventActionResult.Cancelled();
            }
            
            ApplyOutcome(transaction.successOutcome);
            return new RunEventActionResult(true, true, transaction.successOutcome);
        }
        
        private RunEventActionResult ExecuteChance(RunEventChanceSO action)
        {
            if (action == null || !PayCosts(action.costs))
                return RunEventActionResult.Cancelled();
            
            bool success = UnityEngine.Random.value <= Mathf.Clamp01(action.successChance);
            RunEventOutcomeData outcome = success ? action.successOutcome : action.failureOutcome;
            ApplyOutcome(outcome);
            return new RunEventActionResult(true, success, outcome);
        }
        
        private RunEventActionResult ExecuteDoubleOrNothing(RunEventDoubleOrNothingSO action)
        {
            if (action == null || !PayCosts(action.costs))
                return RunEventActionResult.Cancelled();
            
            bool hasPot = RunEventActionRuntimeStore.TryGetInt(action.StateKey, out int currentPot) && currentPot > 0;
            
            if (action.operation == RunEventDoubleOrNothingOperation.CashOut)
            {
                if (!hasPot)
                    return RunEventActionResult.Cancelled();
                
                CurrencyContainer.Add(action.currencyType, currentPot);
                RunEventActionRuntimeStore.Remove(action.StateKey);
                ApplyOutcome(action.successOutcome);
                return new RunEventActionResult(true, true, action.successOutcome);
            }
            
            if (!hasPot)
            {
                currentPot = Mathf.Max(1, action.startingAmount);
                if (!CurrencyContainer.Spend(action.currencyType, currentPot))
                    return RunEventActionResult.Cancelled();
            }
            
            bool success = UnityEngine.Random.value <= Mathf.Clamp01(action.successChance);
            if (success)
            {
                long multipliedPot = (long)currentPot * Mathf.Max(2, action.successMultiplier);
                int nextPot = multipliedPot > int.MaxValue ? int.MaxValue : (int)multipliedPot;
                RunEventActionRuntimeStore.SetInt(action.StateKey, nextPot);
                ApplyOutcome(action.successOutcome);
                return new RunEventActionResult(true, true, action.successOutcome);
            }
            
            RunEventActionRuntimeStore.Remove(action.StateKey);
            ApplyOutcome(action.failureOutcome);
            return new RunEventActionResult(true, false, action.failureOutcome);
        }
        
        private List<RunEventSelectionCandidate> BuildCandidates(RunEventSelectionSettings settings)
        {
            List<RunEventSelectionCandidate> candidates = new List<RunEventSelectionCandidate>();
            if (settings == null)
                return candidates;
            
            switch (settings.source)
            {
                case RunEventSelectionSource.Skill:
                    AddSkillCandidates(settings, candidates);
                    break;
                case RunEventSelectionSource.Relic:
                    AddRelicCandidates(settings, candidates);
                    break;
                case RunEventSelectionSource.Item:
                    AddItemCandidates(settings, candidates);
                    break;
                case RunEventSelectionSource.Ally:
                    AddAllyCandidates(settings, candidates);
                    break;
            }
            
            return candidates;
        }
        
        private void AddSkillCandidates(RunEventSelectionSettings settings,
            List<RunEventSelectionCandidate> candidates)
        {
            if (_skillContainer == null)
                return;
            
            IReadOnlyList<SkillDataSO> ownedSkills = GetOwnedSkills();
            
            for (int i = 0; i < ownedSkills.Count; i++)
            {
                SkillDataSO skill = ownedSkills[i];
                if (!IsAllowedSkill(settings, skill))
                    continue;
                
                candidates.Add(RunEventSelectionCandidate.FromSkill(skill));
            }
            
            if (settings.sortSkillsByDamageDescending)
                candidates.Sort((first, second) => second.Skill.damage.CompareTo(first.Skill.damage));
        }
        
        private void AddRelicCandidates(RunEventSelectionSettings settings,
            List<RunEventSelectionCandidate> candidates)
        {
            if (_relicContainer == null)
                return;
            
            IReadOnlyList<Relic> relics = _relicContainer.GetRelicList;
            Dictionary<Relic, int> copyCounts = new Dictionary<Relic, int>();
            
            for (int i = 0; i < relics.Count; i++)
            {
                Relic relic = relics[i];
                if (relic == null || !IsAllowed(settings.allowedRelics, relic))
                    continue;
                
                copyCounts.TryGetValue(relic, out int copyIndex);
                copyIndex++;
                copyCounts[relic] = copyIndex;
                candidates.Add(RunEventSelectionCandidate.FromRelic(relic, copyIndex));
            }
        }
        
        private void AddItemCandidates(RunEventSelectionSettings settings,
            List<RunEventSelectionCandidate> candidates)
        {
            if (_inventory == null || _inventory.inventorySlots == null)
                return;
            
            HashSet<ItemDataSO> addedItems = new HashSet<ItemDataSO>();
            int requiredAmount = Mathf.Max(1, settings.itemAmountPerSelection);
            
            for (int i = 0; i < _inventory.inventorySlots.Length; i++)
            {
                ItemDataSO item = _inventory.inventorySlots[i].item;
                if (item == null || !addedItems.Add(item) || !IsAllowed(settings.allowedItems, item))
                    continue;
                
                int amount = _inventory.GetItemAmount(item);
                if (amount >= requiredAmount)
                    candidates.Add(RunEventSelectionCandidate.FromItem(item, amount));
            }
        }
        
        private void AddAllyCandidates(RunEventSelectionSettings settings,
            List<RunEventSelectionCandidate> candidates)
        {
            if (_allyRepository == null || allyDatabase == null)
                return;
            
            IReadOnlyList<string> allyIds = _allyRepository.Service.OwnedAllyIds;
            for (int i = 0; i < allyIds.Count; i++)
            {
                if (!allyDatabase.TryGetById(allyIds[i], out AllySO ally) ||
                    !IsAllowed(settings.allowedAllies, ally))
                {
                    continue;
                }
                
                candidates.Add(RunEventSelectionCandidate.FromAlly(ally));
            }
        }
        
        private static bool IsAllowedSkill(RunEventSelectionSettings settings, SkillDataSO skill)
        {
            if (skill == null || !IsAllowed(settings.allowedSkills, skill))
                return false;
            
            if (settings.useSkillCategoryFilter && skill.skillCategory != settings.skillCategory)
                return false;
            
            return settings.maximumSingleDamage <= 0f || skill.damage <= settings.maximumSingleDamage;
        }
        
        private static bool IsAllowed<T>(List<T> allowedValues, T value) where T : UnityEngine.Object
        {
            return allowedValues == null || allowedValues.Count == 0 || allowedValues.Contains(value);
        }
        
        private static bool CanPotentiallySatisfySelection(RunEventSelectionSettings settings,
            List<RunEventSelectionCandidate> candidates)
        {
            if (settings.source != RunEventSelectionSource.Skill || settings.minimumTotalDamage <= 0f)
                return true;
            
            float highestPossibleDamage = candidates
                .Take(settings.MaximumCount)
                .Where(candidate => candidate.Skill != null)
                .Sum(candidate => candidate.Skill.damage);
            
            return highestPossibleDamage >= settings.minimumTotalDamage;
        }
        
        private bool ValidateSelection(RunEventSelectionSettings settings,
            IReadOnlyList<RunEventSelectionCandidate> selected)
        {
            if (settings == null || settings.source == RunEventSelectionSource.None)
                return selected == null || selected.Count == 0;
            
            if (selected == null || selected.Count < settings.MinimumCount || selected.Count > settings.MaximumCount)
                return false;
            
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] == null || selected[i].Source != settings.source)
                    return false;
            }
            
            if (settings.source == RunEventSelectionSource.Skill)
            {
                float totalDamage = selected.Sum(candidate => candidate.Skill != null ? candidate.Skill.damage : 0f);
                if (totalDamage < settings.minimumTotalDamage)
                    return false;
                
                if (settings.maximumTotalDamage > 0f && totalDamage > settings.maximumTotalDamage)
                    return false;
            }
            
            return CanConsumeSelection(settings, selected);
        }
        
        private bool CanConsumeSelection(RunEventSelectionSettings settings,
            IReadOnlyList<RunEventSelectionCandidate> selected)
        {
            if (!settings.consumeSelected)
                return true;
            
            switch (settings.source)
            {
                case RunEventSelectionSource.Skill:
                    return CanConsumeSelectedSkills(selected);
                case RunEventSelectionSource.Relic:
                    return CanConsumeSelectedRelics(selected);
                case RunEventSelectionSource.Item:
                    return CanConsumeSelectedItems(settings, selected);
                case RunEventSelectionSource.Ally:
                    return selected.All(candidate => candidate.Ally != null &&
                        _allyRepository != null && _allyRepository.Service.IsOwned(candidate.Ally));
                default:
                    return true;
            }
        }
        
        private bool CanConsumeSelectedSkills(IReadOnlyList<RunEventSelectionCandidate> selected)
        {
            if (_skillContainer == null || decksList == null)
                return false;
            
            foreach (IGrouping<SkillDataSO, RunEventSelectionCandidate> group in selected.GroupBy(candidate => candidate.Skill))
            {
                if (group.Key == null || CountOwnedSkill(group.Key) < group.Count())
                    return false;
            }
            
            return true;
        }
        
        private bool CanConsumeSelectedRelics(IReadOnlyList<RunEventSelectionCandidate> selected)
        {
            if (_relicContainer == null)
                return false;
            
            foreach (IGrouping<Relic, RunEventSelectionCandidate> group in selected.GroupBy(candidate => candidate.Relic))
            {
                if (group.Key == null || _relicContainer.GetRelicList.Count(relic => relic == group.Key) < group.Count())
                    return false;
            }
            
            return true;
        }
        
        private bool CanConsumeSelectedItems(RunEventSelectionSettings settings,
            IReadOnlyList<RunEventSelectionCandidate> selected)
        {
            if (_inventory == null)
                return false;
            
            int amountPerSelection = Mathf.Max(1, settings.itemAmountPerSelection);
            foreach (IGrouping<ItemDataSO, RunEventSelectionCandidate> group in selected.GroupBy(candidate => candidate.Item))
            {
                if (group.Key == null || _inventory.GetItemAmount(group.Key) < amountPerSelection * group.Count())
                    return false;
            }
            
            return true;
        }
        
        private bool ConsumeSelection(RunEventSelectionSettings settings,
            IReadOnlyList<RunEventSelectionCandidate> selected)
        {
            if (!settings.consumeSelected)
                return true;
            
            for (int i = 0; i < selected.Count; i++)
            {
                RunEventSelectionCandidate candidate = selected[i];
                bool consumed = settings.source switch
                {
                    RunEventSelectionSource.Skill => TryConsumeSkill(candidate.Skill),
                    RunEventSelectionSource.Relic => ConsumeRelic(candidate.Relic),
                    RunEventSelectionSource.Item => _inventory.TryRemoveItem(candidate.Item,
                        Mathf.Max(1, settings.itemAmountPerSelection)),
                    RunEventSelectionSource.Ally => _allyRepository.Service.Dismiss(candidate.Ally),
                    _ => true
                };
                
                if (!consumed)
                    return false;
            }
            
            return true;
        }
        
        private bool CanPayCosts(IReadOnlyList<RunEventCostData> costs)
        {
            if (costs == null || costs.Count == 0)
                return true;
            
            float requiredHealth = 0f;
            for (int i = 0; i < costs.Count; i++)
            {
                RunEventCostData cost = costs[i];
                if (cost == null)
                    continue;
                
                switch (cost.costType)
                {
                    case RunEventCostType.Currency:
                        if (!CurrencyContainer.Has(cost.currencyType, GetTotalCurrencyCost(costs, cost.currencyType)))
                            return false;
                        break;
                    case RunEventCostType.HealthFlat:
                        requiredHealth += Mathf.Max(1, cost.amount);
                        break;
                    case RunEventCostType.HealthMaxPercent:
                        if (_playerHealth == null)
                            return false;
                        requiredHealth += Mathf.RoundToInt(_playerHealth.TotalMaxHealth * cost.maxHealthPercent);
                        break;
                    case RunEventCostType.Item:
                        if (_inventory == null || cost.item == null ||
                            _inventory.GetItemAmount(cost.item) < GetTotalItemCost(costs, cost.item))
                        {
                            return false;
                        }
                        break;
                    case RunEventCostType.Relic:
                        if (_relicContainer == null || cost.relic == null ||
                            _relicContainer.GetRelicList.Count(relic => relic == cost.relic) < GetTotalRelicCost(costs, cost.relic))
                        {
                            return false;
                        }
                        break;
                    case RunEventCostType.Skill:
                        if (_skillContainer == null || decksList == null || cost.skill == null ||
                            CountOwnedSkill(cost.skill) < GetTotalSkillCost(costs, cost.skill))
                        {
                            return false;
                        }
                        break;
                    case RunEventCostType.Ally:
                        if (_allyRepository == null || cost.ally == null ||
                            GetTotalAllyCost(costs, cost.ally) > 1 || !_allyRepository.Service.IsOwned(cost.ally))
                            return false;
                        break;
                }
            }
            
            return requiredHealth <= 0f || _playerHealth != null && _playerHealth.TotalCurrentHealth - requiredHealth >= 1f;
        }
        
        private bool CanPayCombinedCosts(IReadOnlyList<RunEventCostData> costs,
            RunEventSelectionSettings settings, IReadOnlyList<RunEventSelectionCandidate> selected)
        {
            if (settings == null || !settings.consumeSelected || selected == null || selected.Count == 0)
                return true;
            
            if (costs == null || costs.Count == 0)
                return true;
            
            switch (settings.source)
            {
                case RunEventSelectionSource.Skill:
                    foreach (IGrouping<SkillDataSO, RunEventSelectionCandidate> group in
                             selected.GroupBy(candidate => candidate.Skill))
                    {
                        int requiredCount = group.Count() + GetTotalSkillCost(costs, group.Key);
                        if (group.Key == null || CountOwnedSkill(group.Key) < requiredCount)
                            return false;
                    }
                    break;
                case RunEventSelectionSource.Relic:
                    foreach (IGrouping<Relic, RunEventSelectionCandidate> group in
                             selected.GroupBy(candidate => candidate.Relic))
                    {
                        int requiredCount = group.Count() + GetTotalRelicCost(costs, group.Key);
                        if (group.Key == null ||
                            _relicContainer.GetRelicList.Count(relic => relic == group.Key) < requiredCount)
                        {
                            return false;
                        }
                    }
                    break;
                case RunEventSelectionSource.Item:
                    int amountPerSelection = Mathf.Max(1, settings.itemAmountPerSelection);
                    foreach (IGrouping<ItemDataSO, RunEventSelectionCandidate> group in
                             selected.GroupBy(candidate => candidate.Item))
                    {
                        int requiredCount = group.Count() * amountPerSelection + GetTotalItemCost(costs, group.Key);
                        if (group.Key == null || _inventory.GetItemAmount(group.Key) < requiredCount)
                            return false;
                    }
                    break;
                case RunEventSelectionSource.Ally:
                    for (int i = 0; i < selected.Count; i++)
                    {
                        AllySO ally = selected[i].Ally;
                        if (ally == null || GetTotalAllyCost(costs, ally) > 0)
                            return false;
                    }
                    break;
            }
            
            return true;
        }
        
        private bool PayCosts(IReadOnlyList<RunEventCostData> costs)
        {
            if (!CanPayCosts(costs))
                return false;
            
            if (costs == null)
                return true;
            
            for (int i = 0; i < costs.Count; i++)
            {
                RunEventCostData cost = costs[i];
                if (cost == null)
                    continue;
                
                bool paid = cost.costType switch
                {
                    RunEventCostType.Currency => CurrencyContainer.Spend(cost.currencyType, Mathf.Max(1, cost.amount)),
                    RunEventCostType.HealthFlat => SpendHealth(Mathf.Max(1, cost.amount)),
                    RunEventCostType.HealthMaxPercent => SpendHealth(
                        Mathf.RoundToInt(_playerHealth.TotalMaxHealth * cost.maxHealthPercent)),
                    RunEventCostType.Item => _inventory.TryRemoveItem(cost.item, Mathf.Max(1, cost.amount)),
                    RunEventCostType.Relic => ConsumeRelics(cost.relic, Mathf.Max(1, cost.amount)),
                    RunEventCostType.Skill => ConsumeSkills(cost.skill, Mathf.Max(1, cost.amount)),
                    RunEventCostType.Ally => _allyRepository.Service.Dismiss(cost.ally),
                    _ => false
                };
                
                if (!paid)
                    return false;
            }
            
            return true;
        }
        
        private bool SpendHealth(float amount)
        {
            if (_playerHealth == null || amount <= 0f || _playerHealth.TotalCurrentHealth - amount < 1f)
                return false;
            
            _playerHealth.SetCurrentHealth(_playerHealth.TotalCurrentHealth - amount);
            return true;
        }
        
        private bool ConsumeRelics(Relic relic, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (!ConsumeRelic(relic))
                    return false;
            }
            
            return true;
        }
        
        private bool ConsumeRelic(Relic relic)
        {
            if (_relicContainer == null || relic == null || !_relicContainer.GetRelicList.Contains(relic))
                return false;
            
            Bus<RemoveRelic>.Raise(new RemoveRelic(relic));
            return true;
        }
        
        private bool ConsumeSkills(SkillDataSO skill, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (!TryConsumeSkill(skill))
                    return false;
            }
            
            return true;
        }
        
        private List<SkillDataSO> GetOwnedSkills()
        {
            List<SkillDataSO> ownedSkills = new List<SkillDataSO>();
            if (!TryGetSkillSaveData(out SkillDatas saveData))
                return ownedSkills;
            
            SkillDataListSO skillDatabase = decksList != null ? decksList.skillDataList : null;
            if (skillDatabase == null)
                return ownedSkills;
            
            for (int i = 0; i < saveData.SkillIds.Length; i++)
            {
                SkillDataSO skill = skillDatabase.FindSkill(saveData.SkillIds[i]);
                if (skill != null)
                    ownedSkills.Add(skill);
            }
            
            return ownedSkills;
        }
        
        private int CountOwnedSkill(SkillDataSO skill)
        {
            if (skill == null)
                return 0;
            
            return GetOwnedSkills().Count(ownedSkill => IsSameSkill(ownedSkill, skill));
        }
        
        private bool TryConsumeSkill(SkillDataSO skill)
        {
            if (skill == null || !TryGetSkillSaveData(out SkillDatas saveData))
                return false;
            
            List<int> skillIds = saveData.SkillIds.ToList();
            int removeIndex = skillIds.FindIndex(skillId => skillId == skill.index);
            if (removeIndex < 0)
                return false;
            
            skillIds.RemoveAt(removeIndex);
            SkillDatas updatedSaveData = new SkillDatas
            {
                SkillIds = skillIds.ToArray()
            };
            
            _skillContainer.RestoreSaveData(JsonUtility.ToJson(updatedSaveData));
            RemoveSkillFromDecks(skill);
            
            if (skillIds.Count == 0)
                Bus<SkillUpdateEvent>.Raise(new SkillUpdateEvent(new List<SkillDataSO>()));
            
            Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
            return true;
        }
        
        private bool TryGetSkillSaveData(out SkillDatas saveData)
        {
            saveData = new SkillDatas
            {
                SkillIds = Array.Empty<int>()
            };
            
            if (_skillContainer == null)
                return false;
            
            string json = _skillContainer.GetSaveData();
            if (string.IsNullOrWhiteSpace(json))
                return true;
            
            try
            {
                saveData = JsonUtility.FromJson<SkillDatas>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"RunEventActionRunner : 스킬 저장 데이터 파싱 실패\n{exception.Message}", this);
                return false;
            }
            
            if (saveData.SkillIds == null)
                saveData.SkillIds = Array.Empty<int>();
            
            return true;
        }
        
        private void RemoveSkillFromDecks(SkillDataSO removedSkill)
        {
            if (decksList == null || decksList.deckDataList == null)
                return;
            
            int currentDeckIndex = DeckDataSaver.GetCurrentIndex();
            
            for (int deckIndex = 0; deckIndex < decksList.deckDataList.Count; deckIndex++)
            {
                DeckSkillDataListSO deck = decksList.deckDataList[deckIndex];
                if (deck == null || deck.deckInSkills == null)
                    continue;
                
                bool changed = false;
                
                for (int slotIndex = 0; slotIndex < deck.deckInSkills.Length; slotIndex++)
                {
                    if (!IsSameSkill(deck.deckInSkills[slotIndex], removedSkill))
                        continue;
                    
                    deck.deckInSkills[slotIndex] = null;
                    changed = true;
                }
                
                if (changed)
                    DeckDataSaver.DeckSave(deck.deckInSkills, deckIndex, currentDeckIndex);
            }
        }
        
        private static bool IsSameSkill(SkillDataSO first, SkillDataSO second)
        {
            if (first == null || second == null)
                return false;
            
            if (first == second || first.index == second.index)
                return true;
            
            return !string.IsNullOrWhiteSpace(first.skillName) && first.skillName == second.skillName;
        }
        
        private void ApplyOutcome(RunEventOutcomeData outcome)
        {
            if (outcome == null)
                return;
            
            if (_playerHealth != null &&
                (outcome.healthFlatChange != 0 || !Mathf.Approximately(outcome.healthMaxPercentChange, 0f)))
            {
                float change = outcome.healthFlatChange +
                               _playerHealth.TotalMaxHealth * outcome.healthMaxPercentChange;
                float nextHealth = Mathf.Clamp(_playerHealth.TotalCurrentHealth + Mathf.RoundToInt(change),
                    1f, _playerHealth.TotalMaxHealth);
                _playerHealth.SetCurrentHealth(nextHealth);
            }
            
            if (outcome.currencyChanges == null)
                return;
            
            for (int i = 0; i < outcome.currencyChanges.Count; i++)
            {
                RunEventCurrencyChange change = outcome.currencyChanges[i];
                if (change == null || change.amount == 0)
                    continue;
                
                if (change.amount > 0)
                    CurrencyContainer.Add(change.currencyType, change.amount);
                else
                    CurrencyContainer.ForceSpend(change.currencyType, Mathf.Abs(change.amount));
            }
        }
        
        private void ResolveDependencies()
        {
            _inventory = FindAnyObjectByType<InventoryCode>(FindObjectsInactive.Include);
            _skillContainer = FindAnyObjectByType<SkillContainer>(FindObjectsInactive.Include);
            _relicContainer = FindAnyObjectByType<RelicContainer>(FindObjectsInactive.Include);
            
            if (_playerHealth == null)
            {
                BattlePlayer player = _playerManager.BattlePlayer;
                _playerHealth = player != null ? player.GetModule<EntityHealth>() : null;
            }
            
            if (selectionPanel == null)
                selectionPanel = FindAnyObjectByType<RunEventSelectionPanel>(FindObjectsInactive.Include);
        }
        
        private static int GetTotalCurrencyCost(IReadOnlyList<RunEventCostData> costs, ItemType currencyType)
        {
            return costs.Where(cost => cost != null && cost.costType == RunEventCostType.Currency &&
                                       cost.currencyType == currencyType).Sum(cost => Mathf.Max(1, cost.amount));
        }
        
        private static int GetTotalItemCost(IReadOnlyList<RunEventCostData> costs, ItemDataSO item)
        {
            return costs.Where(cost => cost != null && cost.costType == RunEventCostType.Item && cost.item == item)
                .Sum(cost => Mathf.Max(1, cost.amount));
        }
        
        private static int GetTotalRelicCost(IReadOnlyList<RunEventCostData> costs, Relic relic)
        {
            return costs.Where(cost => cost != null && cost.costType == RunEventCostType.Relic && cost.relic == relic)
                .Sum(cost => Mathf.Max(1, cost.amount));
        }
        
        private static int GetTotalSkillCost(IReadOnlyList<RunEventCostData> costs, SkillDataSO skill)
        {
            return costs.Where(cost => cost != null && cost.costType == RunEventCostType.Skill && cost.skill == skill)
                .Sum(cost => Mathf.Max(1, cost.amount));
        }
        
        private static int GetTotalAllyCost(IReadOnlyList<RunEventCostData> costs, AllySO ally)
        {
            return costs.Where(cost => cost != null && cost.costType == RunEventCostType.Ally && cost.ally == ally)
                .Sum(cost => Mathf.Max(1, cost.amount));
        }
        
    }
}
