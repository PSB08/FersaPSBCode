using Code.Scripts.Enemies;
using UnityEngine;
using Work.PSB.Code.FieldCode.BTs;
using YIS.Code.Modules;

namespace Work.PSB.Code.FieldCode.MiniGames.MonsterHunt
{
    public class FieldMonsterData : MonoBehaviour, IModule
    {
        [SerializeField] private MonsterHuntMiniGameManager miniGameManager;

        public bool IsAlive = true;

        private FieldEnemy _owner;
        private Vector3 _initialPos;
        private bool _cached;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as FieldEnemy;
        }
        
        private void Awake()
        {
            CacheInitial();
        }

        private void CacheInitial()
        {
            if (_cached) return;
            _initialPos = transform.position;
            _cached = true;
        }

        public void ForceHide()
        {
            gameObject.SetActive(false);
        }

        public void ResetAndSpawn()
        {
            CacheInitial();
            IsAlive = true;
            transform.position = _initialPos;
            gameObject.SetActive(true);
        }

        public void ApplyDead()
        {
            IsAlive = false;
            NormalFieldEnemy normalEnemy = _owner as NormalFieldEnemy;
            if (normalEnemy != null)
            {
                normalEnemy.ChangeState(EnemyState.Dead);
            }
        }

        public void OnCaptured()
        {
            if (!IsAlive) return;
            if (miniGameManager == null) return;

            miniGameManager.OnMonsterCaptured(this, despawn: true);
        }
        
    }
}