using PSB_Lib.Dependencies;
using UnityEngine;

namespace PSB.Code.BattleCode.Players
{
    public class BattlePlayerBootstrap : MonoBehaviour
    {
        [SerializeField] private BattlePlayer player;

        [Inject] private PlayerManager _playerManager;

        private void Awake()
        {
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);

            if (_playerManager != null)
                _playerManager.SetPlayer(player);
        }

        private void OnDestroy()
        {
            if (_playerManager != null && player != null)
                _playerManager.ClearPlayer(player);
        }
        
    }
}