using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIS.Code.Skills;

namespace Work.PSB.Code.RunSystem
{
    public class SkillRewardSlotUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI gradeText;
        [SerializeField] private TextMeshProUGUI infoText;
        
        private SkillDataSO _skill;
        private Action<SkillDataSO> _onSelected;
        
        public void Bind(SkillDataSO skill, Action<SkillDataSO> onSelected)
        {
            _skill = skill;
            _onSelected = onSelected;
            gameObject.SetActive(skill != null);
            
            if (skill == null)
                return;
            
            if (icon != null)
            {
                icon.enabled = skill.visualData != null && skill.visualData.icon != null;
                
                if (icon.enabled)
                    icon.sprite = skill.visualData.icon;
            }
            
            if (nameText != null)
            {
                nameText.text = skill.visualData != null && !string.IsNullOrWhiteSpace(skill.visualData.uiName)
                    ? skill.visualData.uiName
                    : skill.skillName;
            }

            if (infoText != null)
                infoText.text = $"피해 {skill.damage} / 코스트 {skill.cost}"
                                + $"\n {skill.visualData.itemDescription}";
                                //+ $"\n {skill.visualData.normalDescription}"
                                //+ $"\n {skill.visualData.chainDescription}";
            
            if (gradeText != null)
                gradeText.text = skill.grade.ToString();
            
            if (button != null)
            {
                button.onClick.RemoveListener(Select);
                button.onClick.AddListener(Select);
                button.interactable = true;
            }
        }
        
        public void Lock()
        {
            if (button != null)
                button.interactable = false;
        }
        
        private void Select()
        {
            if (_skill == null)
                return;
            
            _onSelected?.Invoke(_skill);
        }
        
    }
}
