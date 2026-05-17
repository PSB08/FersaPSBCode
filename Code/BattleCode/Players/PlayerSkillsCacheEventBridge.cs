using PSW.Code.CombinationSkill;
using PSW.Code.EventBus;
using UnityEngine;

namespace PSB.Code.BattleCode.Players
{
    public class PlayerSkillsCacheEventBridge : MonoBehaviour
    {
        private PlayerSkillsCache _cache;

        private void Awake()
        {
            _cache = GetComponent<PlayerSkillsCache>();
        }

        private void OnEnable()
        {
            Bus<SkillIconEquippedEvent>.OnEvent += OnEquipped;
            Bus<SkillIconUnequippedEvent>.OnEvent += OnUnequipped;
            Bus<SkillIconClearAllEvent>.OnEvent += OnClearAll;
        }

        private void OnDisable()
        {
            Bus<SkillIconEquippedEvent>.OnEvent -= OnEquipped;
            Bus<SkillIconUnequippedEvent>.OnEvent -= OnUnequipped;
            Bus<SkillIconClearAllEvent>.OnEvent -= OnClearAll;
        }

        private void OnEquipped(SkillIconEquippedEvent evt)
        {
            if (_cache == null) return;
            if (evt.Skill == null) return;

            if (_cache.TryGetOrCreate(evt.Skill, out var skill) && skill != null)
                skill.gameObject.SetActive(true);
        }

        private void OnUnequipped(SkillIconUnequippedEvent evt)
        {
            if (_cache == null) return;
            if (evt.Skill == null) return;

            _cache.SetActive(evt.Skill, false);
        }

        private void OnClearAll(SkillIconClearAllEvent evt)
        {
            if (_cache == null) return;
            _cache.SetAllInactive();
        }
        
    }
}