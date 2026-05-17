using UnityEngine;

namespace Work.PSB.Code.FieldCode.MiniGames.MonsterHunt
{
    public class FieldMonsterData : MonoBehaviour
    {
        [SerializeField] private MonsterHuntMiniGameManager miniGameManager;

        public bool IsAlive = true;

        private Vector3 _initialPos;
        private bool _cached;

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
            gameObject.SetActive(false);
        }

        public void OnCaptured()
        {
            if (!IsAlive) return;
            if (miniGameManager == null) return;

            miniGameManager.OnMonsterCaptured(this, despawn: true);
        }
        
    }
}