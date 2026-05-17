using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Code.Scripts.Enemies.Astar
{
    [CreateAssetMenu(fileName = "BakedData", menuName = "SO/Path/BakedData", order = 0)]
    public class BakedDataSO : ScriptableObject
    {
        public List<NodeData> points = new  List<NodeData>();
        
        private Dictionary<Vector3Int, NodeData> _pointDict;

        private void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_pointDict == null || _pointDict.Count != points.Count)  //_pointDict가 null이거나 저장된 수가 다르다면
                _pointDict = points.ToDictionary(node => node.cellPosition);  //ToDictionary를 통해 만듭니다
        }

        public void ClearPoints()
        {
            points?.Clear();  //points 리스트를 전부 비웁니다
        }

        public void AddPoint(Vector3 worldPosition, Vector3Int cellPosition)
        {
            points.Add(new NodeData(worldPosition, cellPosition));  //새 NodeData를 points에 추가합니다
        }

        public bool HasNode(Vector3Int cellPosition) => _pointDict != null && _pointDict.ContainsKey(cellPosition);
        //특정 cellPosition이 _pointDict에 있는지 존재 여부 확인합니다

        public bool TryGetNode(Vector3Int cellPosition, out NodeData nodeData)
        {
            //셀 좌표에 해당하는 NodeData를 가져오고 있으면 true 없으면 false
            if (HasNode(cellPosition))
            {
                nodeData = _pointDict[cellPosition];
                return true;
            }
            
            nodeData = default;
            return false;
        }
        
        
    }
}