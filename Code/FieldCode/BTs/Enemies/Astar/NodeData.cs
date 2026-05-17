using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Scripts.Enemies.Astar
{
    [Serializable]
    public struct NodeData
    {
        public Vector3 worldPosition; 
        public Vector3Int cellPosition; 
        public List<LinkData> neighbours;  //이 NodeData와 연결된 이웃들 리스트

        public NodeData(Vector3 worldPosition, Vector3Int cellPosition)
        {
            //노드 생성 할 때 위치랑 셀 좌표 초기화
            this.worldPosition = worldPosition;
            this.cellPosition = cellPosition;
            neighbours = new List<LinkData>();
        }

        public void AddNeighbour(NodeData neighbour)
        {
            //이웃 노드를 받아서 새 LinkData 생성
            neighbours.Add(new LinkData
            {
                startPosition = worldPosition,
                startCellPosition = cellPosition,
                endPosition = neighbour.worldPosition,
                endCellPosition = neighbour.cellPosition,
                cost = Vector3Int.Distance(cellPosition, neighbour.cellPosition)
            });
        }

        public override int GetHashCode() => cellPosition.GetHashCode();
        
        public override bool Equals(object obj)
        {
            if (obj is NodeData data)
            {
                return data.cellPosition == cellPosition;
                //cellPosition이 같으면 동일 노드로 판단
            }
            return false;
        }

        public static bool operator ==(NodeData lhs, NodeData rhs)
        {
            return lhs.Equals(rhs);
        }
        public static bool operator !=(NodeData lhs, NodeData rhs)
        {
            return !(lhs == rhs);
        }
        
        
    }
}