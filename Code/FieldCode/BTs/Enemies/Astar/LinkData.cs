using System;
using UnityEngine;

namespace Code.Scripts.Enemies.Astar
{
    [Serializable]
    public struct LinkData
    {
        public Vector3 startPosition;  //월드 좌표의 시작 위치
        public Vector3Int startCellPosition;  //시작 위치의 셀 좌표
        public Vector3 endPosition;  //월드 좌표의 끝 위치
        public Vector3Int endCellPosition;  //끝 위치의 셀 좌표

        public float cost;  //이동 비용
        
    }
}