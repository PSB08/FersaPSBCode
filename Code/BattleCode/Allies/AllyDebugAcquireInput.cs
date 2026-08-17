using PSB_Lib.Dependencies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PSB.Code.BattleCode.Allies
{
    //동료 얻을 수 있는가 테스트코드입니다.
    public class AllyDebugAcquireInput : MonoBehaviour
    {
        [SerializeField] private AllySO allyToAcquire;
        [SerializeField] private bool autoEquip = true;
        [SerializeField] private BattleAllyLoader battleAllyLoader;

        private AllyPartyService _partyService;

        private void Awake()
        {
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame)
                AcquireDebugAlly();
        }

        public void AcquireDebugAlly()
        {
            if (allyToAcquire == null)
            {
                Debug.LogWarning("[AllyDebugAcquireInput] AllySO is missing.", this);
                return;
            }

            AllyPartyService service = ResolvePartyService();
            if (service == null)
            {
                Debug.LogWarning("[AllyDebugAcquireInput] AllyPartyService is missing.", this);
                return;
            }

            bool changed = service.Acquire(allyToAcquire, autoEquip);
            if (service.IsEquipped(allyToAcquire))
                battleAllyLoader?.SpawnIfNeeded(allyToAcquire);

            Debug.Log(changed
                ? $"[AllyDebugAcquireInput] Acquired ally: {allyToAcquire.AllyId}"
                : $"[AllyDebugAcquireInput] Ally already owned: {allyToAcquire.AllyId}", this);
        }

        private AllyPartyService ResolvePartyService()
        {
            if (_partyService != null)
                return _partyService;

            if (AllyPartyRepository.Instance != null)
            {
                _partyService = AllyPartyRepository.Instance.Service;
                return _partyService;
            }

            AllyPartyRepository repository = FindAnyObjectByType<AllyPartyRepository>(FindObjectsInactive.Include);
            _partyService = repository != null ? repository.Service : null;
            return _partyService;
        }
        
    }
}
