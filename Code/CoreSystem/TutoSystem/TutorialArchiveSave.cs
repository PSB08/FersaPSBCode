using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    public static class TutorialArchiveSave
    {
        public static event Action<string, bool> OnViewedChanged;

        private static string Key(string tutorialId) => $"TUTO_VIEWED_{tutorialId}";

        public static bool IsViewed(string tutorialId)
        {
            if (string.IsNullOrWhiteSpace(tutorialId)) return false;
            return PlayerPrefs.GetInt(Key(tutorialId), 0) == 1;
        }

        public static void MarkViewed(string tutorialId)
        {
            if (string.IsNullOrWhiteSpace(tutorialId)) return;

            if (IsViewed(tutorialId))
                return;

            PlayerPrefs.SetInt(Key(tutorialId), 1);
            PlayerPrefs.Save();

            OnViewedChanged?.Invoke(tutorialId, true);
        }

        public static void ResetViewed(string tutorialId)
        {
            if (string.IsNullOrWhiteSpace(tutorialId)) return;

            bool existed = PlayerPrefs.HasKey(Key(tutorialId));

            PlayerPrefs.DeleteKey(Key(tutorialId));
            PlayerPrefs.Save();

            if (existed)
                OnViewedChanged?.Invoke(tutorialId, false);
        }

        public static void ResetAll(IEnumerable<TutorialDataSO> tutorials)
        {
            if (tutorials == null) return;

            foreach (var t in tutorials)
            {
                if (t == null || string.IsNullOrWhiteSpace(t.TutorialId))
                    continue;

                PlayerPrefs.DeleteKey(Key(t.TutorialId));
                OnViewedChanged?.Invoke(t.TutorialId, false);
            }

            PlayerPrefs.Save();
        }
        
    }
}