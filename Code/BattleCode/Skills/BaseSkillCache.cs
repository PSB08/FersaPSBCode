using System.Collections.Generic;
using UnityEngine;
using Work.YIS.Code.Skills;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Skills
{
    public abstract class BaseSkillCache : MonoBehaviour
    {
        [SerializeField] protected Transform skillsRoot;

        protected readonly Dictionary<SkillEnum, BaseSkill> Cache = new();

        protected virtual void Awake()
        {
            if (skillsRoot == null) skillsRoot = transform;
        }

        public bool TryGet(SkillEnum id, out BaseSkill skill)
            => Cache.TryGetValue(id, out skill) && skill != null;
        
        public bool TryGetOrCreate(SkillEnum id, SkillDataSO so, out BaseSkill skill)
        {
            skill = null;

            if (TryGet(id, out skill))
                return true;

            if (skillsRoot == null) return false;
            if (so == null || so.skillPrefab == null) return false;

            var go = Instantiate(so.skillPrefab, skillsRoot);

            if (!go.TryGetComponent(out skill))
            {
                Destroy(go);
                return false;
            }

            skill.SetData(so);
            skill.Initialize();

            go.SetActive(false);
            Cache[id] = skill;
            return true;
        }

        public void SetActive(SkillEnum id, bool active)
        {
            if (Cache.TryGetValue(id, out var s) && s != null)
                s.gameObject.SetActive(active);
        }

        public void SetActive(SkillEnum[] ids, bool active)
        {
            if (ids == null) return;
            for (int i = 0; i < ids.Length; i++)
                SetActive(ids[i], active);
        }

        public void SetAllInactive()
        {
            foreach (var kv in Cache)
            {
                if (kv.Value != null)
                    kv.Value.gameObject.SetActive(false);
            }
        }
        
        protected abstract bool TryResolveSO(SkillEnum id, out SkillDataSO so);
        
        public bool TryGetOrCreate(SkillEnum id, out BaseSkill skill)
        {
            skill = null;

            if (TryGet(id, out skill))
                return true;

            if (!TryResolveSO(id, out var so))
                return false;

            return TryGetOrCreate(id, so, out skill);
        }
        
    }
}