using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Events;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.UIs
{
    public static class SystemLogNameResolver
    {
        public static string GetTargetName(ModuleOwner target, SystemLogOwner owner)
        {
            if (target == null)
                return "알 수 없는 대상";

            if (target is BattleEnemy enemy)
            {
                if (enemy.enemySO != null && !string.IsNullOrEmpty(enemy.enemySO.enemyName))
                    return enemy.enemySO.enemyName;

                return enemy.name;
            }

            if (owner == SystemLogOwner.Player)
                return "플레이어";

            return target.name;
        }

        public static string GetTargetName(ModuleOwner target, SystemLogOwner owner, string playerName)
        {
            if (target == null)
                return "알 수 없는 대상";

            if (target is BattleEnemy enemy)
            {
                if (enemy.enemySO != null && !string.IsNullOrEmpty(enemy.enemySO.enemyName))
                    return enemy.enemySO.enemyName;

                return enemy.name;
            }

            if (owner == SystemLogOwner.Player)
            {
                if (!string.IsNullOrEmpty(playerName))
                    return playerName;

                return "플레이어";
            }

            return target.name;
        }
        
    }
}