using System;
using UnityEngine;

namespace Code.Scripts.Enemies.Astar
{
    public class AStarNode : IComparable<AStarNode>
    {
        public float G;  // 시작점에서 현재 노드까지의 실제 이동 비용
        public float F;  // G + H (총 비용, 우선순위 비교용)

        // 노드 위치 정보
        public Vector3 worldPosition;  // 실제 월드 좌표
        public Vector3Int cellPosition;  // 타일맵의 셀 좌표
        
        public NodeData nodeData;  // 노드와 관련된 데이터
        
        public AStarNode parentNode;  // 경로 추적을 위해 이전 노드 참조
        
        public int CompareTo(AStarNode other)
        {
            // F 값 비교
            if(Mathf.Approximately(other.F, this.F))
                return 0;
            return other.F < F ? -1 : 1;
        }

        public override bool Equals(object obj)
        {
            if (obj is AStarNode node)
            {
                return Equals(node);
            }
            return false;
        }

        // 같은 셀 좌표면 동일 노드
        private bool Equals(AStarNode node)
        {
            if (node is null) return false;
            return cellPosition == node.cellPosition;
        }
        
        // 해시코드 생성
        public override int GetHashCode() => cellPosition.GetHashCode();

        public static bool operator ==(AStarNode lhs, AStarNode rhs)
        {
            if (lhs is null)
            {
                if (rhs is null) return true;
                return false;
            }
            
            return lhs.Equals(rhs);
        }

        public static bool operator !=(AStarNode lhs, AStarNode rhs)
        {
            return !(lhs == rhs);
        }
        
    }
}