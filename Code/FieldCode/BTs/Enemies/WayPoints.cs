using UnityEngine;

namespace Code.Scripts.Enemies
{
    public class WayPoints : MonoBehaviour
    {
        private WayPoint[] _wayPoints;
        public int Length => _wayPoints.Length;

        public Vector3 this[int index] => _wayPoints[index].transform.position;
        
        private void Awake()
        {
            _wayPoints = GetComponentsInChildren<WayPoint>();
            transform.SetParent(null);
        }
        
    }
}