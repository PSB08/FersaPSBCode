using PSB_Lib.ObjectPool.RunTime;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.BTs
{
    public class PoolParticleEffect : MonoBehaviour, IPoolable
    {
        [field:SerializeField] public string VFXName { get; private set; }
        [SerializeField] private bool isOnPosition;
        [SerializeField] private ParticleSystem particle;
        
        private Pool _myPool;

        public GameObject GameObject => gameObject;
        public PoolItemSO PoolItem { get; }
        public void SetUpPool(Pool pool) => _myPool = pool;
        public void ResetItem() { }

        public void PlayVFX(Vector3 position, Quaternion rotation)
        {
            if(isOnPosition == false)
                transform.SetPositionAndRotation(position, rotation);
            
            particle.Play(true);

            WaitAndReturnToPool();
        }

        private async void WaitAndReturnToPool()
        {
            await Awaitable.WaitForSecondsAsync(particle.main.duration);

            while (particle != null && particle.IsAlive(true))
            {
                await Awaitable.WaitForSecondsAsync(0.1f);
            }

            if (this != null && gameObject.activeSelf && _myPool != null)
            {
                _myPool.Push(this);
            }
        }

        public void StopVFX()
        {
            particle.Stop(true);
            
            if (this != null && gameObject.activeSelf && _myPool != null)
            {
                _myPool.Push(this);
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(VFXName) == false)
                gameObject.name = VFXName;
        }
        
    }
}