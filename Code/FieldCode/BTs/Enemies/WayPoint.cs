using UnityEngine;

namespace Code.Scripts.Enemies
{
    public class WayPoint : MonoBehaviour
    {
        public Vector3 Position => transform.position;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Position, 0.15f);
        }
        
    }
}