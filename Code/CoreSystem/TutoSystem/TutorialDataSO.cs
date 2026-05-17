using System.Collections.Generic;
using UnityEngine;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    [CreateAssetMenu(fileName = "TutoData", menuName = "SO/Tutorial/Data", order = 0)]
    public class TutorialDataSO : ScriptableObject
    {
        [SerializeField] private string tutorialId;
        [SerializeField] private string displayName;
        [SerializeField] private List<TutorialPageSO> pages = new();

        public string TutorialId => tutorialId;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName;

                return !string.IsNullOrWhiteSpace(tutorialId) ? tutorialId : name;
            }
        }

        public List<TutorialPageSO> Pages => pages;

        public TutorialPageSO GetPage(int index)
        {
            if (pages == null || pages.Count == 0) return null;
            if (index < 0 || index >= pages.Count) return null;
            return pages[index];
        }

        public int PageCount => pages?.Count ?? 0;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(tutorialId))
                tutorialId = name;
        }
#endif
        
    }
}