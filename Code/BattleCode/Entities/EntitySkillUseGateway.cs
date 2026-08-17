using CIW.Code;
using PSB.Code.BattleCode.Skills;
using System.Threading.Tasks;
using YIS.Code.CoreSystem.Skills;
using YIS.Code.Defines;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Entities
{
    public class EntitySkillUseGateway
    {
        private readonly BattleSkillUseService _skillUseService;
        
        public bool IsAvailable => _skillUseService != null;
        
        public EntitySkillUseGateway(BattleSkillUseService skillUseService)
        {
            _skillUseService = skillUseService;
        }
        
        public Task<SkillUseResult> UseAsync(Entity caster, SkillDataSO skillData,
            Entity directTarget, SkillTargetCandidates candidates, bool canActivateChain)
        {
            if (_skillUseService == null)
            {
                return Task.FromResult(SkillUseResult.Failed(SkillUseFailureReason.InvalidRequest));
            }
            
            SkillUseRequest request = new(caster, caster, skillData,
                directTarget, candidates, canActivateChain);
            
            return _skillUseService.UseSkillAsync(request);
        }
        
        public static BtSkillUseResult ToBtResult(SkillUseResult result)
        {
            if (result.Success)
                return BtSkillUseResult.Success;
            
            return result.FailureReason == SkillUseFailureReason.InvalidTarget
                ? BtSkillUseResult.NoTarget
                : BtSkillUseResult.Failed;
        }
        
    }
}