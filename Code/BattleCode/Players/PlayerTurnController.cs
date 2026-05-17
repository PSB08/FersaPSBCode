using UnityEngine;
using Work.CSH.Scripts.Interfaces;
using Work.CSH.Scripts.Managers;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Players
{
    public class PlayerTurnController : MonoBehaviour, IModule, ITurnable
    {
        [field: SerializeField] public TurnManagerSO TurnManager { get; set; }
        
        private BattlePlayer _player;

        private PlayerTargetSelector _selector;
        private PlayerCombatController _combat;
        private BuffModule _buffModule;

        public void Initialize(ModuleOwner owner)
        {
            _player = owner as BattlePlayer;
            if (_player == null)
            {
                Debug.LogError("PlayerTurnController: entity is not BattlePlayer.");
                return;
            }

            TurnManager = _player.TurnManager;
            _selector = _player.GetModule<PlayerTargetSelector>();
            _combat   = _player.GetModule<PlayerCombatController>();
            _buffModule = _player.GetModule<BuffModule>();

            if (TurnManager != null)
                TurnManager.AddITurnableList(this);

            SetActiveForPlayerTurn(false);
        }

        private void OnDestroy()
        {
            if (TurnManager != null)
                TurnManager.RemoveITurnableList(this);
        }

        public void OnStartTurn(bool isPlayerTurn)
        {
            if (!isPlayerTurn) return;
            
            SetActiveForPlayerTurn(true);

            _combat?.OnPlayerTurnStarted();
            _selector?.UnlockInput();
        }

        public void OnEndTurn(bool isPlayerTurn)
        {
            if (!isPlayerTurn) return;

            SetActiveForPlayerTurn(false);
            _buffModule.UpdateTime();
        }

        private void SetActiveForPlayerTurn(bool active)
        {
            if (_selector != null) _selector.enabled = active;
            if (_combat != null) _combat.enabled = active;
        }
        
    }
}