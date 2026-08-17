using System;
using System.Collections.Generic;
using YIS.Code.Defines;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public class EnemySkillHand
    {
        private readonly List<int> _deckSkillIndices = new();
        private readonly List<int> _handSkillIndices = new();
        private readonly List<int> _discardedSkillIndices = new();
        private readonly List<int> _affordableHandSkillIndices = new();
        
        public IReadOnlyList<int> HandSkillIndices => _handSkillIndices;
        public IReadOnlyList<int> AffordableHandSkillIndices => _affordableHandSkillIndices;
        public int HandCount => _handSkillIndices.Count;
        
        public void Reset(SkillDataSO[] skills, bool includeDefense)
        {
            _deckSkillIndices.Clear();
            _handSkillIndices.Clear();
            _discardedSkillIndices.Clear();
            _affordableHandSkillIndices.Clear();
            
            if (skills != null)
            {
                for (int i = 0; i < skills.Length; i++)
                {
                    SkillDataSO skill = skills[i];
                    if (skill == null || (!includeDefense && skill.skillCategory == SkillCategory.Defense))
                        continue;

                    _deckSkillIndices.Add(i);
                }
            }
            
            Shuffle(_deckSkillIndices);
        }
        
        public void DrawToLimit(int maxHand)
        {
            while (_handSkillIndices.Count < maxHand)
            {
                RefillDeckFromDiscardIfNeeded();

                if (_deckSkillIndices.Count == 0)
                    break;

                int lastDeckIndex = _deckSkillIndices.Count - 1;
                int skillIndex = _deckSkillIndices[lastDeckIndex];
                _deckSkillIndices.RemoveAt(lastDeckIndex);

                if (!_handSkillIndices.Contains(skillIndex))
                    _handSkillIndices.Add(skillIndex);
            }
        }

        public void RefreshAffordable(Func<int, bool> canPay)
        {
            _affordableHandSkillIndices.Clear();

            if (canPay == null)
                return;

            for (int i = 0; i < _handSkillIndices.Count; i++)
            {
                int idx = _handSkillIndices[i];
                if (canPay(idx))
                    _affordableHandSkillIndices.Add(idx);
            }
        }

        public bool CanGainMoreSkills(int maxHand)
        {
            if (_handSkillIndices.Count >= maxHand)
                return false;

            return _deckSkillIndices.Count > 0 || _discardedSkillIndices.Count > 0;
        }

        public bool Consume(int skillIndex)
        {
            if (skillIndex < 0)
                return false;

            if (!_handSkillIndices.Remove(skillIndex))
                return false;

            if (!_discardedSkillIndices.Contains(skillIndex))
                _discardedSkillIndices.Add(skillIndex);

            return true;
        }

        public void DiscardHand()
        {
            for (int i = 0; i < _handSkillIndices.Count; i++)
            {
                int skillIndex = _handSkillIndices[i];
                if (!_discardedSkillIndices.Contains(skillIndex))
                    _discardedSkillIndices.Add(skillIndex);
            }

            _handSkillIndices.Clear();
            _affordableHandSkillIndices.Clear();
        }

        public void ClearAffordable()
        {
            _affordableHandSkillIndices.Clear();
        }

        public string BuildHandDebugText(Func<int, string> skillTextBuilder)
        {
            if (_handSkillIndices.Count == 0)
                return "비어 있음";

            string text = string.Empty;
            for (int i = 0; i < _handSkillIndices.Count; i++)
            {
                if (i > 0)
                    text += ", ";

                text += skillTextBuilder != null ? skillTextBuilder(_handSkillIndices[i]) 
                    : _handSkillIndices[i].ToString();
            }

            return text;
        }

        private void RefillDeckFromDiscardIfNeeded()
        {
            if (_deckSkillIndices.Count > 0 || _discardedSkillIndices.Count == 0)
                return;

            _deckSkillIndices.AddRange(_discardedSkillIndices);
            _discardedSkillIndices.Clear();
            Shuffle(_deckSkillIndices);
        }

        private static void Shuffle(List<int> values)
        {
            if (values == null)
                return;

            for (int i = values.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (values[i], values[randomIndex]) = (values[randomIndex], values[i]);
            }
        }
        
    }
}
