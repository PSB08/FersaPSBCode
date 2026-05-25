using PSB_Lib.ObjectPool.RunTime;
using UnityEngine;
using UnityEngine.UI;

namespace PSB.Code.BattleCode.UIs
{
    public class PhaseIcon : MonoBehaviour, IPoolable
    {
        [SerializeField] private Image iconImage;
        
        [Header("Sprites")]
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite consumedSprite;

        [field: SerializeField] public PoolItemSO PoolItem { get; set; }
        
        private Pool _pool;

        public void Init()
        {
            SetState(true);
            gameObject.SetActive(true);
        }
        
        public void SetState(bool isActive)
        {
            if (iconImage != null)
            {
                if (isActive)
                {
                    iconImage.sprite = activeSprite;
                    iconImage.color = Color.white; 
                }
                else
                {
                    iconImage.sprite = consumedSprite;
                    iconImage.color = new Color(0.5f, 0.5f, 0.5f, 1f); 
                }
            }
        }

        public void SetUpPool(Pool pool)
        {
            _pool = pool;
        }

        public void ResetItem()
        {
            gameObject.SetActive(false);
        }
        
    }
}