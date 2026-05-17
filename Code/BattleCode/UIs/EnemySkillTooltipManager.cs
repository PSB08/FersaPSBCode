using PSB.Code.BattleCode.Enemies;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.UIs
{
    public static class EnemySkillTooltipManager
    {
        public static EnemySkillTooltipUI Current { get; private set; }

        public static void Show(EnemySkillTooltipUI prefab, RectTransform anchor, 
            BattleEnemy enemy, SkillDataSO skill)
        {
            if (prefab == null || anchor == null || enemy == null || skill == null)
                return;
            
            if (Current == null)
            {
                Current = Object.Instantiate(prefab, anchor);
                var rt = (RectTransform)Current.transform;
                rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                Current.transform.SetParent(anchor, worldPositionStays: false);
                ((RectTransform)Current.transform).anchoredPosition = Vector2.zero;
            }

            Current.Set(enemy, skill);
            Current.gameObject.SetActive(true);
        }

        public static void Hide()
        {
            if (Current == null) return;
            Current.gameObject.SetActive(false);
        }

        public static bool IsShowing => Current != null && Current.gameObject.activeSelf;
        
    }
}