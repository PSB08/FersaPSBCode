using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Code.Scripts.Enemies.Astar
{
    public class PathAgent : MonoBehaviour
    {
        [SerializeField] private BakedDataSO bakedData;

        public int GetPath(Vector3Int startPosition, Vector3Int destination, Vector3[] pointArr)
        {
            // 전체 경로 노드 리스트 계산
            List<AStarNode> result = CalculatePath(startPosition, destination);
            int cornerIndex = 0;
            
            if (result.Count > 0)
            {
                // 첫 번째 지점(시작점)
                pointArr[cornerIndex] = result[0].worldPosition;
                cornerIndex++;

                // 중간 코너 지점들 추출
                for (int i = 1; i < result.Count - 1; i++)
                {
                    if(cornerIndex >= pointArr.Length) break;
                    
                    // 이전 방향
                    Vector3Int beforeDirection = result[i].cellPosition - result[ i - 1 ].cellPosition;
                    // 다음 방향
                    Vector3Int nextDirection = result[i + 1].cellPosition - result[i].cellPosition;

                    // 방향이 바뀌는 지점만 추가
                    if (beforeDirection != nextDirection)
                    {
                        pointArr[cornerIndex] = result[i].worldPosition;
                        cornerIndex++;
                    }
                }
                
                // 마지막 지점(목적지)
                pointArr[cornerIndex] = result[^1].worldPosition; 
                cornerIndex++;
            }
            
            // 반환값: 경로에 들어간 포인트 개수
            return cornerIndex;
        }

        // A* 알고리즘으로 경로를 계산하여 노드 리스트 반환
        private List<AStarNode> CalculatePath(Vector3Int start, Vector3Int end)
        {
            PriorityQueue<AStarNode> openList = new PriorityQueue<AStarNode>();  // 탐색 후보
            List<AStarNode> closedList = new List<AStarNode>();  // 탐색 완료
            List<AStarNode> path = new List<AStarNode>();  // 최종 경로
            
            bool result = false;

            // 시작/목표 노드 데이터 가져오기 실패 시 빈 경로 반환
            if (!bakedData.TryGetNode(start, out NodeData startNodeData))
                return path;

            if (!bakedData.TryGetNode(end, out NodeData endNodeData)) 
                return path;
            
            // 시작 노드를 openList에 추가
            openList.Push(new AStarNode
            {
                nodeData = startNodeData,
                cellPosition = startNodeData.cellPosition,
                worldPosition = startNodeData.worldPosition,
                parentNode = null,
                G = 0, F = CalcH(startNodeData.cellPosition, endNodeData.cellPosition)
            });

            // 경로 탐색 루프
            while (openList.Count > 0)
            {
                // F 값이 가장 낮은 노드 꺼내기
                AStarNode currentNode = openList.Pop(); 
                
                // 현재 노드의 이웃 노드 탐색
                foreach (LinkData link in currentNode.nodeData.neighbours)
                {
                    // 이미 방문한 노드면 무시
                    bool isVisited = closedList.Any(n => n.cellPosition == link.endCellPosition);
                    if(isVisited) continue;
                    
                    // 이웃 노드 데이터 가져오기 실패 시 무시
                    if(!bakedData.TryGetNode(link.endCellPosition, out NodeData nextNode))
                        continue;

                    // G 값 계산
                    float newG = link.cost + currentNode.G;
                    
                    // 새로운 AStarNode 생성
                    AStarNode nextAStarNode = new AStarNode
                    {
                        nodeData = nextNode,
                        cellPosition = nextNode.cellPosition,
                        worldPosition = nextNode.worldPosition,
                        parentNode = currentNode,
                        G = newG, F = newG + CalcH(nextNode.cellPosition, endNodeData.cellPosition)
                    };

                    // 이미 openList에 같은 노드가 없으면 추가
                    AStarNode existNode = openList.Contains(nextAStarNode);
                    if (existNode == null)
                    {
                        openList.Push(nextAStarNode);
                    }
                } // end foreach
                
                // 현재 노드를 closedList에 추가
                closedList.Add(currentNode);

                // 목표 노드에 도달했으면 탐색 종료
                if (currentNode.nodeData == endNodeData)
                {
                    result = true; 
                    break;
                }
            } // end of while

            // 경로를 찾았다면 추적하여 path 구성
            if (result)
            {
                AStarNode last = closedList[^1]; 
                while (last.parentNode != null)
                {
                    path.Add(last);
                    last = last.parentNode; 
                }
                path.Add(last);  // 시작 노드 포함
                path.Reverse();  // 순서를 시작→목표로 변경
            }

            return path;
        }

        private float CalcH(Vector3Int start, Vector3Int end) => Vector3Int.Distance(start, end);
        
    }
}