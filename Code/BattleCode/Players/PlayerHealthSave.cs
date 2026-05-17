using System;
using UnityEngine;

namespace PSB.Code.BattleCode.Players
{
    public static class PlayerHealthSave
    {
        private const string KeyCur = "PLAYER_HP_CUR";
        private const string KeyMax = "PLAYER_HP_MAX";

        private static bool _hasCached;
        private static float _cachedCur;
        private static float _cachedMax;

        public static event Action<float, float> OnSnapshotChanged;
        public static event Action OnResetRequested;

        public static void SaveSnapshot(float currentHp, float maxHp)
        {
            if (float.IsNaN(currentHp) || float.IsInfinity(currentHp)) return;
            if (float.IsNaN(maxHp) || float.IsInfinity(maxHp)) return;

            maxHp = Mathf.Max(1f, maxHp);
            currentHp = Mathf.Clamp(currentHp, 0f, maxHp);

            PlayerPrefs.SetFloat(KeyCur, currentHp);
            PlayerPrefs.SetFloat(KeyMax, maxHp);
            PlayerPrefs.Save();

            _hasCached = true;
            _cachedCur = currentHp;
            _cachedMax = maxHp;

            OnSnapshotChanged?.Invoke(currentHp, maxHp);
        }

        public static bool TryGetCached(out float cur, out float max)
        {
            cur = 0f;
            max = 0f;
            if (!_hasCached) return false;

            cur = _cachedCur;
            max = _cachedMax;
            return max > 0f;
        }

        public static bool TryLoad(out float currentHp, out float maxHp)
        {
            currentHp = 0f;
            maxHp = 0f;

            if (!PlayerPrefs.HasKey(KeyCur) || !PlayerPrefs.HasKey(KeyMax))
                return false;

            float cur = PlayerPrefs.GetFloat(KeyCur, -1f);
            float max = PlayerPrefs.GetFloat(KeyMax, -1f);

            if (cur < 0f) return false;
            if (max <= 0f) return false;
            if (float.IsNaN(cur) || float.IsInfinity(cur)) return false;
            if (float.IsNaN(max) || float.IsInfinity(max)) return false;

            cur = Mathf.Clamp(cur, 0f, max);

            currentHp = cur;
            maxHp = max;

            _hasCached = true;
            _cachedCur = cur;
            _cachedMax = max;

            return true;
        }

        public static void Reset()
        {
            Debug.Log("<color=yellow>[PlayerHealthSave]초기화</color>");

            if (PlayerPrefs.HasKey(KeyCur)) PlayerPrefs.DeleteKey(KeyCur);
            if (PlayerPrefs.HasKey(KeyMax)) PlayerPrefs.DeleteKey(KeyMax);
            PlayerPrefs.Save();

            _hasCached = false;
            _cachedCur = 0f;
            _cachedMax = 0f;
            
            OnResetRequested?.Invoke();
        }

        public static void Flush() => PlayerPrefs.Save();
        
    }
}
