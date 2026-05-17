using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.CoreSystem;
using YIS.Code.Events;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Commands
{
    [CreateAssetMenu(fileName = "SkillCommand", menuName = "SO/Commands/SkillCommand", order = 0)]
    public class SkillCommandSO : BaseCommandSO
    {
        [SerializeField] private SkillDataListSO skillList;

        public override bool CanHandle(Context context)
        {
            if (context.Executor == null) return false;
            if (context.SkillIds == null || context.SkillIds.Length == 0) return false;
            if (context.Target == null) return false;
            if (skillList == null) return false;

            for (int i = 0; i < context.SkillIds.Length; i++)
            {
                var id = context.SkillIds[i];
                var so = skillList.FindSkill(id);
                if (so == null) return false;
                
                if (context.Executor.CanExecuteById(id, context.Target))
                    return true;
            }
            return false;
        }

        public override bool Handle(Context context)
        {
            if (!CanHandle(context)) return false;

            bool any = false;

            for (int i = 0; i < context.SkillIds.Length; i++)
            {
                var id = context.SkillIds[i];

                bool isChain =
                    (context.ChainFlags != null && i < context.ChainFlags.Length)
                        ? context.ChainFlags[i]
                        : context.CanChain;

                var so = skillList.FindSkill(id);
                if (so == null) continue;

                if (!context.Executor.CanExecuteById(id, context.Target))
                    continue;

                any = context.Executor.ExecuteById(id, isChain, context.Target);
            }

            Debug.Log($"실행자 {context.Caster}");
            return any;
        }
        
    }
}