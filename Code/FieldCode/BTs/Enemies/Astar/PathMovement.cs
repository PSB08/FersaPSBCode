using CIW.Code;
using Code.Scripts.Enemies.BT;
using UnityEngine;
using UnityEngine.Tilemaps;
using YIS.Code.Modules;

namespace Code.Scripts.Enemies.Astar
{
    public class PathMovement : MonoBehaviour, IModule
    {
        [Header("Path")]
        [SerializeField] private PathAgent agent;
        [SerializeField] private int maxPathCount = 128;

        [Header("Global Grid")]
        [SerializeField] private GridLayout globalGrid;

        [Header("Floor")]
        [SerializeField] private Tilemap[] groundMaps;

        [field: SerializeField] public float arriveDistance = 0.05f;

        private Vector3[] _pathArr;
        private int _totalPathCount;

        public bool IsArrived { get; private set; }
        public bool IsPathFailed { get; private set; }
        public bool IsStop { get; set; }

        private Entity _entity;
        private AgentMovement _movement;

        private int _currentPathIndex = 0;
        private Vector2 _prevPosition;

        public void Initialize(ModuleOwner owner)
        {
            _entity = owner as Entity;
            _movement = owner.GetModule<AgentMovement>();
            _pathArr = new Vector3[maxPathCount];

            if (globalGrid == null)
            {
                globalGrid = GetComponentInParent<GridLayout>();
            }

            Debug.Assert(agent != null, "[PathMovement] agent가 비어있음");
            Debug.Assert(globalGrid != null, "[PathMovement] globalGrid가 비어있음 (Baker와 동일한 Grid 지정)");
            Debug.Assert(groundMaps != null && groundMaps.Length > 0, "[PathMovement] groundMaps가 비어있음");
        }

        private bool HasFloorAtWorld(Vector3 worldPos)
        {
            if (groundMaps == null) return false;

            for (int i = 0; i < groundMaps.Length; i++)
            {
                var g = groundMaps[i];
                if (g == null) continue;

                Vector3Int cell = g.WorldToCell(worldPos);
                if (g.HasTile(cell)) return true;
            }
            return false;
        }

        public void SetDestination(Vector3 destination)
        {
            _totalPathCount = 0;
            IsArrived = false;
            IsPathFailed = false;

            if (agent == null || globalGrid == null)
            {
                IsPathFailed = true;
                return;
            }

            if (!HasFloorAtWorld(destination))
            {
                IsPathFailed = true;
                return;
            }

            Vector3Int startCell = globalGrid.WorldToCell(transform.position);
            Vector3Int endCell   = globalGrid.WorldToCell(destination);

            _totalPathCount = agent.GetPath(startCell, endCell, _pathArr);

            if (_totalPathCount < 2)
            {
                IsPathFailed = true;
                return;
            }

            _prevPosition = _entity.Transform.position;
            _currentPathIndex = 1;
        }

        private void Update()
        {
            if (_totalPathCount <= 0)
            {
                return;
            }
            
            if (_movement == null || _entity == null) return;

            if (IsStop)
            {
                _movement.StopImmediately();
                _movement.SetMovement(Vector2.zero);
                return;
            }

            if (_currentPathIndex >= _totalPathCount)
            {
                _movement.StopImmediately();
                return;
            }

            if (!CheckArrive())
            {
                Vector2 direction = _pathArr[_currentPathIndex] - _entity.Transform.position;
                _movement.SetMovement(direction.normalized);
            }
            else
            {
                _movement.StopImmediately();
                _movement.SetMovement(Vector2.zero);
            }
        }

        private bool CheckArrive()
        {
            Vector2 nextGoal = _pathArr[_currentPathIndex];
            Vector2 currPos = _entity.Transform.position;

            if (Vector2.Distance(nextGoal, currPos) <= arriveDistance)
            {
                _currentPathIndex++;
                _prevPosition = currPos;

                if (_currentPathIndex >= _totalPathCount)
                {
                    IsArrived = true;
                    return true;
                }
                return false;
            }

            Vector2 prevDir = (nextGoal - _prevPosition).normalized;
            Vector2 curDir  = (nextGoal - currPos).normalized;

            _prevPosition = currPos;

            if (Vector2.Dot(prevDir, curDir) < -0.1f)
            {
                _currentPathIndex++;
                if (_currentPathIndex >= _totalPathCount)
                {
                    IsArrived = true;
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            if (_totalPathCount <= 0 || _pathArr == null) return;

            for (int i = 0; i < _totalPathCount - 1; i++)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_pathArr[i], 0.12f);
                Gizmos.DrawLine(_pathArr[i], _pathArr[i + 1]);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_pathArr[_totalPathCount - 1], 0.14f);
        }
        
    }
}
