using PSW.Code.Dial;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Skills.Sequences
{
    public class SkillDropVisual : MonoBehaviour
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer gradeRenderer;

        [Header("Data")]
        [SerializeField] private SkillCircleDataListSO circleDataList;

        private void Reset()
        {
            iconRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init(SkillRewardSO reward)
        {
            if (reward == null || reward.skill == null)
                return;

            SkillDataSO skill = reward.skill;

            if (iconRenderer != null && skill.visualData != null && skill.visualData.icon != null)
            {
                iconRenderer.sprite = skill.visualData.icon;
            }

            if (gradeRenderer != null && circleDataList != null)
            {
                gradeRenderer.color = circleDataList.GetOutLineColor(skill.grade);
            }
        }
        
    }
}