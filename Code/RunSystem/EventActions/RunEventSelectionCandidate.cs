using PSB.Code.BattleCode.Allies;
using UnityEngine;
using Work.CSH.Scripts.Relics;
using YIS.Code.Items;
using YIS.Code.Skills;

namespace Work.PSB.Code.RunSystem
{
    public sealed class RunEventSelectionCandidate
    {
        public string CandidateId { get; }
        public RunEventSelectionSource Source { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public SkillDataSO Skill { get; }
        public Relic Relic { get; }
        public ItemDataSO Item { get; }
        public AllySO Ally { get; }
        
        private RunEventSelectionCandidate(string candidateId, RunEventSelectionSource source,
            string displayName, string description, Sprite icon, SkillDataSO skill = null,
            Relic relic = null, ItemDataSO item = null, AllySO ally = null)
        {
            CandidateId = candidateId;
            Source = source;
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            Skill = skill;
            Relic = relic;
            Item = item;
            Ally = ally;
        }
        
        public static RunEventSelectionCandidate FromSkill(SkillDataSO skill)
        {
            string displayName = skill != null && skill.visualData != null
                ? skill.visualData.uiName : skill != null ? skill.skillName : string.Empty;
            Sprite icon = skill != null && skill.visualData != null ? skill.visualData.icon : null;
            string description = skill != null ? $"피해 {skill.damage:0.#} / 비용 {skill.cost}" : string.Empty;
            string id = skill != null ? $"Skill:{skill.skillName}" : "Skill:None";
            
            return new RunEventSelectionCandidate(id, RunEventSelectionSource.Skill,
                displayName, description, icon, skill);
        }
        
        public static RunEventSelectionCandidate FromRelic(Relic relic, int copyIndex)
        {
            string displayName = relic != null && relic.visualData != null
                ? relic.visualData.relicName : relic != null ? relic.name : string.Empty;
            string description = relic != null && relic.visualData != null
                ? relic.visualData.descripction : string.Empty;
            Sprite icon = relic != null && relic.visualData != null ? relic.visualData.sprite : null;
            string id = relic != null ? $"Relic:{relic.GetInstanceID()}:{copyIndex}" : $"Relic:None:{copyIndex}";
            
            return new RunEventSelectionCandidate(id, RunEventSelectionSource.Relic,
                displayName, description, icon, relic: relic);
        }
        
        public static RunEventSelectionCandidate FromItem(ItemDataSO item, int amount)
        {
            string displayName = item != null && !string.IsNullOrWhiteSpace(item.itemName)
                ? item.itemName : item != null ? item.name : string.Empty;
            string description = $"보유 {Mathf.Max(0, amount)}개";
            Sprite icon = item != null && item.visualData != null ? item.visualData.icon : null;
            string id = item != null ? $"Item:{item.itemId}" : "Item:None";
            
            return new RunEventSelectionCandidate(id, RunEventSelectionSource.Item,
                displayName, description, icon, item: item);
        }
        
        public static RunEventSelectionCandidate FromAlly(AllySO ally)
        {
            string displayName = ally != null && !string.IsNullOrWhiteSpace(ally.allyName)
                ? ally.allyName : ally != null ? ally.name : string.Empty;
            string id = ally != null ? $"Ally:{ally.AllyId}" : "Ally:None";
            
            return new RunEventSelectionCandidate(id, RunEventSelectionSource.Ally,
                displayName, ally != null ? ally.AllyId : string.Empty,
                ally != null ? ally.icon : null, ally: ally);
        }
        
    }
}
