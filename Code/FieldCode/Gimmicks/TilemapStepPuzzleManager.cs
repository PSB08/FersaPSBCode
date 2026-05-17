using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Work.CSH.Scripts.PlayerComponents;
using DG.Tweening;

namespace Work.PSB.Code.FieldCode.Gimmicks
{
    [Serializable]
    public class PuzzleAssets
    {
        public TileBase defaultPathTile;
        public TileBase startNodeTile;
        public TileBase checkpointNodeTile;
        public TileBase endNodeTile;
        public TileBase activeTile;
        public GameObject stepVfxPrefab;
        public GameObject checkpointUnlockVfxPrefab;
    }
    
    public class TilemapStepPuzzleManager : FieldGimmick
    {
        [Header("Grid System")]
        [SerializeField] private Tilemap puzzleTilemap;
        [SerializeField] private List<EdgeCollider2D> phasePaths;
        
        [Header("Visual Assets")]
        [SerializeField] private PuzzleAssets puzzleAssets;

        [Header("DoTween Settings")]
        [SerializeField] private float phaseRevealDuration = 0.4f;
        [SerializeField] private float phaseHideDuration = 0.3f;
        [SerializeField] private Vector3 tileCenterOffset = new Vector3(0f, 0f, 0f);

        [Header("Gimmick Settings")]
        [SerializeField] private FieldPlayer targetPlayer;
        [SerializeField] private Vector2 detectionOffset = new Vector2(0, 0.5f);
        [SerializeField] private float detectionThreshold = 0.4f;
        [SerializeField] private bool failOnStepOutside = true;
        [SerializeField] private bool failOnRevisit = true;

        private List<List<Vector3Int>> _phasesCells = new List<List<Vector3Int>>();
        private HashSet<Vector3Int> _allTargetCells = new HashSet<Vector3Int>();
        private HashSet<Vector3Int> _unlockedCells = new HashSet<Vector3Int>();
        private HashSet<Vector3Int> _steppedCells = new HashSet<Vector3Int>();
        private List<GameObject> _spawnedEffects = new List<GameObject>();
        
        private Dictionary<Vector3Int, int> _checkpointToNextPhase = new Dictionary<Vector3Int, int>();
        private Vector3Int _startCell;
        private Vector3Int _endCell;
        private Vector3Int _lastPlayerCell = new Vector3Int(-999, -999, -999);
        
        private int _currentUnlockedPhaseIndex = 0;
        private bool _isPuzzleActive = false;
        private bool _isAnimatingReset = false;

        private string RevealTweenId => this.GetInstanceID() + "_Reveal";
        private string HideTweenId => this.GetInstanceID() + "_Hide";
        private string PopTweenId => this.GetInstanceID() + "_Pop";

        protected override void Awake()
        {
            base.Awake();
            InitializePhases();
        }

        protected override void Start()
        {
            base.Start();
            ResetGimmick();
        }
        
        protected override void OnDestroy()
        {
            DOTween.Kill(RevealTweenId);
            DOTween.Kill(HideTweenId);
            DOTween.Kill(PopTweenId);
    
            base.OnDestroy();
        }

        private void InitializePhases()
        {
            if (puzzleTilemap == null || phasePaths == null || phasePaths.Count == 0) return;

            _phasesCells.Clear();
            _allTargetCells.Clear();
            _checkpointToNextPhase.Clear();

            for (int i = 0; i < phasePaths.Count; i++)
            {
                if (phasePaths[i] == null) continue;

                List<Vector3Int> cells = ParseEdgeToCells(phasePaths[i]);
                _phasesCells.Add(cells);

                foreach (var c in cells) _allTargetCells.Add(c);

                if (i < phasePaths.Count - 1 && cells.Count > 0)
                {
                    _checkpointToNextPhase[cells[cells.Count - 1]] = i + 1;
                }
            }

            if (_phasesCells.Count > 0 && _phasesCells[0].Count > 0)
            {
                _startCell = _phasesCells[0][0];
                var lastPhase = _phasesCells[_phasesCells.Count - 1];
                if (lastPhase.Count > 0) _endCell = lastPhase[lastPhase.Count - 1];
            }
        }

        private List<Vector3Int> ParseEdgeToCells(EdgeCollider2D col)
        {
            List<Vector3Int> cells = new List<Vector3Int>();
            Vector2[] points = col.points;
            Transform trans = col.transform;

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 startWorld = trans.TransformPoint(points[i]);
                Vector3 endWorld = trans.TransformPoint(points[i + 1]);
                
                int steps = Mathf.CeilToInt(Vector3.Distance(startWorld, endWorld) * 5f); 

                for (int j = 0; j <= steps; j++)
                {
                    Vector3 pointOnLine = Vector3.Lerp(startWorld, endWorld, (float)j / steps);
                    Vector3Int cellPos = puzzleTilemap.WorldToCell(pointOnLine);
                    
                    if (cells.Count == 0 || cells[cells.Count - 1] != cellPos) cells.Add(cellPos);
                }
            }
            return cells;
        }

        private void Update()
        {
            if (isCleared || targetPlayer == null || _allTargetCells.Count == 0 || _isAnimatingReset) return;

            Vector3 samplingPosition = targetPlayer.transform.position + (Vector3)detectionOffset;
            Vector3Int currentCell = puzzleTilemap.WorldToCell(samplingPosition);

            if (currentCell != _lastPlayerCell)
            {
                Vector3 cellCenter = puzzleTilemap.GetCellCenterWorld(currentCell);
                
                if (Vector2.Distance(samplingPosition, cellCenter) < 
                    (puzzleTilemap.cellSize.x * (1f - detectionThreshold)))
                {
                    ProcessStep(currentCell);
                    _lastPlayerCell = currentCell;
                }
            }
        }

        private void ProcessStep(Vector3Int cellPos)
        {
            if (!_isPuzzleActive)
            {
                if (cellPos == _startCell)
                {
                    _isPuzzleActive = true;
                    StepOnCell(cellPos);
                    PopPermanentCellsDo(); 
                    UnlockPhaseDo(0);
                }
                return;
            }

            if (!_unlockedCells.Contains(cellPos) && failOnStepOutside)
            {
                FailPuzzle(); 
                return;
            }

            if (!IsAdjacent(_lastPlayerCell, cellPos) && cellPos != _lastPlayerCell)
            {
                FailPuzzle(); 
                return;
            }
            
            if (_steppedCells.Contains(cellPos) && failOnRevisit && cellPos != _lastPlayerCell) 
            { 
                FailPuzzle();
                return;
            }

            StepOnCell(cellPos);

            if (_checkpointToNextPhase.TryGetValue(cellPos, out int nextPhaseIndex))
            {
                if (nextPhaseIndex > _currentUnlockedPhaseIndex)
                {
                    _currentUnlockedPhaseIndex = nextPhaseIndex;
                    PlayEffect(puzzleAssets.checkpointUnlockVfxPrefab, cellPos);
                    UnlockPhaseDo(nextPhaseIndex);
                }
            }

            if (cellPos == _endCell)
            {
                if (_steppedCells.Count == _allTargetCells.Count) OnSuccess();
                else FailPuzzle();
            }
        }

        private bool IsAdjacent(Vector3Int pos1, Vector3Int pos2)
        {
            if (pos1.x == -999) return true;
            return (Mathf.Abs(pos1.x - pos2.x) == 1 && pos1.y == pos2.y) 
                   || (pos1.x == pos2.x && Mathf.Abs(pos1.y - pos2.y) == 1);
        }

        private void StepOnCell(Vector3Int cellPos)
        {
            _steppedCells.Add(cellPos);
            if (puzzleAssets.activeTile != null)
            {
                puzzleTilemap.SetTile(cellPos, puzzleAssets.activeTile);
                SetTileScale(cellPos, 1f);
            }
            PlayEffect(puzzleAssets.stepVfxPrefab, cellPos);
        }

        private void PlayEffect(GameObject prefab, Vector3Int cellPos)
        {
            if (prefab != null)
                _spawnedEffects.Add(Instantiate(prefab, puzzleTilemap.GetCellCenterWorld(cellPos), 
                    Quaternion.identity, transform));
        }

        private void SetTileScale(Vector3Int cell, float scaleVal)
        {
            puzzleTilemap.SetTileFlags(cell, TileFlags.None);
            Vector3 scale = new Vector3(scaleVal, scaleVal, 1f);
            
            Matrix4x4 matrix = Matrix4x4.Translate(tileCenterOffset) * 
                               Matrix4x4.Scale(scale) * Matrix4x4.Translate(-tileCenterOffset);
            
            puzzleTilemap.SetTransformMatrix(cell, matrix);
            
            if (Mathf.Approximately(scaleVal, 1f)) 
                puzzleTilemap.SetTileFlags(cell, TileFlags.LockTransform);
        }

        private HashSet<Vector3Int> GetPermanentCells()
        {
            HashSet<Vector3Int> permanentCells = new HashSet<Vector3Int>
            {
                _startCell, 
                _endCell
            };
            
            foreach (var cp in _checkpointToNextPhase.Keys)
            {
                permanentCells.Add(cp);
            }
            
            return permanentCells;
        }

        private void PopPermanentCellsDo()
        {
            List<Vector3Int> cellsToPop = new List<Vector3Int>();

            foreach (var pCell in GetPermanentCells())
            {
                if (pCell == _startCell) 
                    continue;

                puzzleTilemap.SetTile(pCell, pCell == _endCell ? puzzleAssets.endNodeTile 
                    : puzzleAssets.checkpointNodeTile);
                
                SetTileScale(pCell, 0f);
                cellsToPop.Add(pCell);
            }

            if (cellsToPop.Count == 0) 
                return;

            DOVirtual.Float(0f, 1f, phaseRevealDuration, (t) => 
            {
                foreach (var cell in cellsToPop)
                {
                    if (puzzleTilemap.HasTile(cell))
                    {
                        SetTileScale(cell, t);
                    }
                }
            }).SetEase(Ease.OutBack).SetId(PopTweenId);
        }

        private void UnlockPhaseDo(int phaseIndex)
        {
            if (phaseIndex >= _phasesCells.Count)
                return;

            List<Vector3Int> cellsToAnimate = new List<Vector3Int>();
            HashSet<Vector3Int> permanentCells = GetPermanentCells();

            foreach (var cell in _phasesCells[phaseIndex])
            {
                _unlockedCells.Add(cell);
                if (_steppedCells.Contains(cell) || permanentCells.Contains(cell))
                    continue;

                puzzleTilemap.SetTile(cell, puzzleAssets.defaultPathTile);
                SetTileScale(cell, 0f);
                cellsToAnimate.Add(cell);
            }

            if (cellsToAnimate.Count == 0) return;

            DOVirtual.Float(0f, 1f, phaseRevealDuration, (t) => 
            {
                foreach (var cell in cellsToAnimate)
                {
                    if (puzzleTilemap.HasTile(cell))
                    {
                        SetTileScale(cell, t);
                    }
                }
            }).SetEase(Ease.OutBack).SetId(RevealTweenId);
        }
        
        private void HideCellsDo(List<Vector3Int> cellsToHide, Action onComplete)
        {
            if (cellsToHide.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            DOVirtual.Float(1f, 0f, phaseHideDuration, (t) => 
                {
                    foreach (var cell in cellsToHide)
                    {
                        if (puzzleTilemap.HasTile(cell))
                        {
                            SetTileScale(cell, t);
                        }
                    }
                })
            .SetEase(Ease.InBack)
            .SetId(HideTweenId)
            .OnComplete(() => onComplete?.Invoke());
        }

        private void HideAndResetDo()
        {
            _isAnimatingReset = true;
            _isPuzzleActive = false;

            DOTween.Kill(RevealTweenId);
            DOTween.Kill(HideTweenId);
            DOTween.Kill(PopTweenId);

            foreach (var effect in _spawnedEffects) if (effect != null) Destroy(effect);
            _spawnedEffects.Clear();

            if (_phasesCells.Count > 0 && _phasesCells[0].Count > 0)
            {
                puzzleTilemap.SetTile(_startCell, puzzleAssets.startNodeTile);
                SetTileScale(_startCell, 1f);
            }

            List<Vector3Int> cellsToHide = new List<Vector3Int>();
            foreach (var cell in _allTargetCells)
            {
                if (cell == _startCell) 
                    continue;
                if (puzzleTilemap.HasTile(cell)) 
                    cellsToHide.Add(cell);
            }

            HideCellsDo(cellsToHide, () => 
            {
                foreach (var cell in cellsToHide)
                {
                    puzzleTilemap.SetTile(cell, null);
                }
                FinalizeReset();
            });
        }

        private void FinalizeReset()
        {
            _steppedCells.Clear();
            _unlockedCells.Clear();
            
            _currentUnlockedPhaseIndex = 0;
            _lastPlayerCell = new Vector3Int(-999, -999, -999);
            
            _unlockedCells.Add(_startCell);
            _isAnimatingReset = false;
        }

        private void FailPuzzle() { OnFail(targetPlayer); ResetGimmick(); }
        
        public override void ResetGimmick()
        {
            base.ResetGimmick();
            if (puzzleTilemap != null) HideAndResetDo();
        }

        protected override void OnSuccess()
        {
            if (isCleared) return;
            StartCoroutine(ClearSequenceRoutine());
        }

        private IEnumerator ClearSequenceRoutine()
        {
            _isAnimatingReset = true;
            _isPuzzleActive = false;

            DOTween.Kill(RevealTweenId);
            DOTween.Kill(HideTweenId);
            DOTween.Kill(PopTweenId);

            List<Vector3Int> cellsToHide = new List<Vector3Int>();
            
            foreach (var cell in _allTargetCells)
            {
                if (puzzleTilemap.HasTile(cell))
                {
                    cellsToHide.Add(cell);
                }
            }

            bool isAnimDone = false;
            HideCellsDo(cellsToHide, () => isAnimDone = true);

            yield return new WaitUntil(() => isAnimDone);
            base.OnSuccess();
        }

        private void OnDrawGizmos()
        {
            if (phasePaths == null || puzzleTilemap == null) 
                return;
            if (!Application.isPlaying) 
                InitializePhases();

            for (int p = 0; p < _phasesCells.Count; p++)
            {
                Color phaseColor = Color.Lerp(Color.cyan, Color.magenta, 
                    (float)p / Mathf.Max(1, _phasesCells.Count - 1));
                
                foreach (var cell in _phasesCells[p])
                {
                    Gizmos.color = new Color(phaseColor.r, phaseColor.g, phaseColor.b, 0.3f);
                    
                    if (cell == _startCell)
                    {
                        Gizmos.color = Color.green;
                    }
                    else if (cell == _endCell)
                    {
                        Gizmos.color = Color.red;
                    }
                    else if (_checkpointToNextPhase.ContainsKey(cell))
                    {
                        Gizmos.color = Color.yellow;
                    }

                    Gizmos.DrawCube(puzzleTilemap.GetCellCenterWorld(cell), puzzleTilemap.cellSize * 0.9f);
                }
            }

            if (targetPlayer != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(targetPlayer.transform.position + (Vector3)detectionOffset, 0.1f);
            }
        }
        
    }
}