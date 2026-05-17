using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    [CreateAssetMenu(fileName = "TutoArchive", menuName = "SO/Tutorial/Archive", order = 0)]
    public class TutorialArchiveSO : ScriptableObject
    {
        [SerializeField] private List<TutorialDataSO> tutorials = new();

        public List<TutorialDataSO> Tutorials => tutorials;

        public TutorialDataSO GetById(string tutorialId)
        {
            return tutorials.FirstOrDefault(x => x != null && x.TutorialId == tutorialId);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var valid = tutorials.Where(x => x != null).ToList();
            var dup = valid.GroupBy(x => x.TutorialId)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (dup.Count > 0)
                Debug.LogWarning($"[TutorialArchiveDatabase] 중복 tutorialId 있음: {string.Join(", ", dup)}", this);
        }
#endif
        
    }
}