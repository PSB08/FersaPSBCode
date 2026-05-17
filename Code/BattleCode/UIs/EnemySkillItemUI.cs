using PSB.Code.BattleCode.Enemies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.UIs
{
    public class EnemySkillItemUI : MonoBehaviour
    {
        [SerializeField] private EnemySkillTooltipUI tooltipPrefab;
        private RectTransform _tooltipAnchor;

        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI damageText;

        private EnemySkillTooltipUI _tooltip;
        private BattleEnemy _enemy;
        private SkillDataSO _skill;

        public void Set(BattleEnemy enemy, SkillDataSO skill, RectTransform tooltipAnchor)
        {
            if (enemy == null || skill == null) return;

            _enemy = enemy;
            _skill = skill;
            _tooltipAnchor = tooltipAnchor;

            icon.sprite = skill.visualData.icon;
            
            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(skill.visualData.uiName)
                    ? skill.skillName
                    : skill.visualData.uiName;

            if (damageText != null)
                damageText.text = $"DMG {Mathf.RoundToInt(skill.damage)}";
        }
        
        public void ToggleTooltip()
        {
            var cur = EnemySkillTooltipManager.Current;
            
            if (cur != null && EnemySkillTooltipManager.IsShowing &&
                cur.CurrentEnemy == _enemy && cur.CurrentSkill == _skill)
            {
                EnemySkillTooltipManager.Hide();
                return;
            }
            
            EnemySkillTooltipManager.Show(tooltipPrefab, _tooltipAnchor, _enemy, _skill);
        }
        
    }
}