using PSB.Code.BattleCode.Enemies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.UIs
{
    public class EnemySkillTooltipUI : MonoBehaviour
    {
        public SkillDataSO CurrentSkill { get; private set; }
        public BattleEnemy CurrentEnemy { get; private set; }

        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI desc;
        [SerializeField] private TextMeshProUGUI damage;

        public void Set(BattleEnemy enemy, SkillDataSO skill)
        {
            CurrentSkill = skill;
            CurrentEnemy = enemy;

            if (icon != null)
                icon.sprite = skill.visualData.icon;
            
            if (title != null)
                title.text = string.IsNullOrEmpty(skill.visualData.uiName) ? 
                    skill.skillName : skill.visualData.uiName;
            
            if (desc != null)
                desc.text = skill.visualData.itemDescription;
            
            if (damage != null)
                damage.text = $"데미지 : {skill.damage}";
        }
        
    }
}