using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.MapSaves
{
    [Serializable]
    public class EnemySaveState
    {
        public string id;
        public bool isAlive;
        public Vector3 position;
    }
    
    [Serializable]
    public class BoxSaveState
    {
        public string id;
        public bool isCollected;     // 먹었는지
        public Vector3 position;
    }
    
    [Serializable]
    public class GimmickSaveState
    {
        public string id;
        public bool isCleared;
    }
    
    [Serializable]
    public class TalkSaveState
    {
        public string id;
        public bool isFinished;
    }
    
    [Serializable]
    public class SceneState
    {
        public Vector3 playerPosition;
        public List<EnemySaveState> enemies = new List<EnemySaveState>();
        public List<BoxSaveState> boxes = new List<BoxSaveState>();
        public List<GimmickSaveState> gimmicks = new List<GimmickSaveState>();
        public List<TalkSaveState> talks = new List<TalkSaveState>();
    }
    
}