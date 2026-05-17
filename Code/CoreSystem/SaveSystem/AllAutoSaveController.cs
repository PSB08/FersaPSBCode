using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using UnityEngine;

namespace PSB.Code.CoreSystem.SaveSystem
{
    public class AllAutoSaveController : MonoBehaviour
    {
        public static AllAutoSaveController Instance { get; private set; }

        [SerializeField] private float saveCooldown = 5f;
        private float _lastSaveTime = -999f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            Bus<RequestSaveEvent>.OnEvent += OnRequestSave;
            Bus<ItemGainedEvent>.OnEvent += OnItemGained;
        }

        private void OnDisable()
        {
            Bus<RequestSaveEvent>.OnEvent -= OnRequestSave;
            Bus<ItemGainedEvent>.OnEvent -= OnItemGained;
        }

        private void OnItemGained(ItemGainedEvent evt)
        {
            TrySaveWithCooldown();
        }

        private void OnRequestSave(RequestSaveEvent evt)
        {
            TrySaveWithCooldown();
        }

        private void TrySaveWithCooldown()
        {
            if (Time.unscaledTime - _lastSaveTime < saveCooldown)
                return;

            _lastSaveTime = Time.unscaledTime;
            Debug.Log("[AutoSaveController] Auto Save 실행");

            // 자동 저장은 callback 필요 없음
            Bus<SavePrefEvent>.Raise(new SavePrefEvent(null));
        }
        
    }
}