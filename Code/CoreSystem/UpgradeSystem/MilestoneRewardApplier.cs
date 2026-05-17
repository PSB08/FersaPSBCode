using CIW.Code;
using UnityEngine;
using YIS.Code.Modules;

namespace Work.PSB.Code.CoreSystem.UpgradeSystem
{
    public class MilestoneRewardApplier : MonoBehaviour
    {
        [SerializeField] private UpgradeDefSO[] allUpgradeDefs;

        public void ApplyAllMilestones(Entity playerEntity, UpgradeService upgradeService)
        {
            if (playerEntity == null || upgradeService == null || allUpgradeDefs == null)
                return;

            foreach (var def in allUpgradeDefs)
            {
                if (def == null) continue;

                int currentLevel = upgradeService.GetCurrentLevel(def);

                foreach (var milestone in def.milestones)
                {
                    if (currentLevel >= milestone.unlockLevel && milestone.effect != null)
                    {
                        milestone.effect.ApplyEffect(playerEntity);
                    }
                }
            }
        }
        
    }
}