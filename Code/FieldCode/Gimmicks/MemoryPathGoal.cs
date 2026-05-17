using UnityEngine;
using Work.CSH.Scripts.PlayerComponents;

namespace Work.PSB.Code.FieldCode.Gimmicks
{
    public class MemoryPathGoal : MonoBehaviour
    {
        [SerializeField] private MemoryPathGimmick masterGimmick;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var entity = other.GetComponentInParent<FieldPlayer>();
            
            if (entity != null && masterGimmick != null)
            {
                masterGimmick.ReachGoal();
                
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
            }
        }
        
    }
}