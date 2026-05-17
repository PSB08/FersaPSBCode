using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Scripts.Enemies.Astar
{
    public class PathBaker : MonoBehaviour
    {
        [Header("Map Root")]
        [SerializeField] private Transform mapRoot;

        [Header("Global Grid")]
        [SerializeField] private GridLayout globalGrid;

        [Header("Bake Output")]
        [SerializeField] private BakedDataSO bakedData;

        [Header("Options")]
        [SerializeField] private bool isCornerCheck = true;

#if UNITY_EDITOR
        [Header("Gizmos")]
        [SerializeField] private bool isDrawGizmos = true;
        [SerializeField] private Color nodeColor = Color.yellow;
        [SerializeField] private Color edgeColor = Color.black;
#endif
        
        private readonly List<Tilemap> _groundMaps = new();
        private readonly List<Tilemap> _blockedMaps = new();

        private const string GroundName = "Background";
        private const string BlockedName = "Wall";
        private const string BlockedName2 = "Objects";
        private const string BlockedName3 = "UD";
        private const string BlockedName4 = "LR";

        [ContextMenu("Bake Map Data")]
        private void BakeMapData()
        {
            Debug.Assert(mapRoot != null, "[PathBaker] mapRoot가 비어있음");
            Debug.Assert(bakedData != null, "[PathBaker] bakedData가 비어있음");

            if (globalGrid == null)
            {
                globalGrid = mapRoot.GetComponentInParent<GridLayout>();
                if (globalGrid == null)
                    globalGrid = GetComponentInParent<GridLayout>();
            }
            Debug.Assert(globalGrid != null, "[PathBaker] globalGrid가 null임. 전역 GridLayout을 할당하세요.");

            CollectTilemaps();

            Debug.Assert(_groundMaps.Count > 0, $"[PathBaker] '{GroundName}' Tilemap을 mapRoot 아래에서 하나도 못 찾음");

            bakedData.ClearPoints();

            if (!TryGetWorldBoundsFromGround(out Bounds worldBounds))
                return;
            
            BoundsInt scanCells = WorldBoundsToGlobalCellBounds(worldBounds);

            for (int x = scanCells.xMin; x < scanCells.xMax; x++)
            {
                for (int y = scanCells.yMin; y < scanCells.yMax; y++)
                {
                    Vector3Int globalCell = new Vector3Int(x, y, 0);
                    Vector3 worldCenter = GetGlobalCellCenterWorld(globalCell);

                    if (CanMoveWorld(worldCenter))
                    {
                        bakedData.AddPoint(worldCenter, globalCell);
                    }
                }
            }
            RecordNeighbors_GlobalCells();

            SaveIfInUnityEditor();

            Debug.Log($"[PathBaker] Bake 완료: ground={_groundMaps.Count}, blocked={_blockedMaps.Count}, nodes={bakedData.points.Count}");
        }
        
        private void CollectTilemaps()
        {
            _groundMaps.Clear();
            _blockedMaps.Clear();
            
            var allTilemaps = mapRoot.GetComponentsInChildren<Tilemap>(true);

            for (int i = 0; i < allTilemaps.Length; i++)
            {
                Tilemap tm = allTilemaps[i];
                if (tm == null) continue;

                if (tm.gameObject.name == GroundName)
                    _groundMaps.Add(tm);
                else if (tm.gameObject.name == BlockedName || tm.gameObject.name == BlockedName2
                         || tm.gameObject.name == BlockedName3 || tm.gameObject.name == BlockedName4)
                    _blockedMaps.Add(tm);
            }
        }

        private bool TryGetWorldBoundsFromGround(out Bounds bounds)
        {
            bounds = default;
            bool hasAny = false;

            for (int i = 0; i < _groundMaps.Count; i++)
            {
                Tilemap m = _groundMaps[i];
                if (m == null) continue;

                m.CompressBounds();
                BoundsInt cb = m.cellBounds;
                if (cb.size.x <= 0 || cb.size.y <= 0) continue;

                Vector3 wMin = m.CellToWorld(new Vector3Int(cb.xMin, cb.yMin, 0));
                Vector3 wMax = m.CellToWorld(new Vector3Int(cb.xMax, cb.yMax, 0));

                Bounds b = new Bounds();
                b.SetMinMax(Vector3.Min(wMin, wMax), Vector3.Max(wMin, wMax));

                if (!hasAny)
                {
                    hasAny = true;
                    bounds = b;
                }
                else
                {
                    bounds.Encapsulate(b.min);
                    bounds.Encapsulate(b.max);
                }
            }

            return hasAny;
        }

        private BoundsInt WorldBoundsToGlobalCellBounds(Bounds worldBounds)
        {
            Vector3Int c0 = globalGrid.WorldToCell(worldBounds.min);
            Vector3Int c1 = globalGrid.WorldToCell(worldBounds.max);

            int xMin = Mathf.Min(c0.x, c1.x) - 1;
            int xMax = Mathf.Max(c0.x, c1.x) + 2;
            int yMin = Mathf.Min(c0.y, c1.y) - 1;
            int yMax = Mathf.Max(c0.y, c1.y) + 2;

            return new BoundsInt(xMin, yMin, 0, xMax - xMin, yMax - yMin, 1);
        }

        private Vector3 GetGlobalCellCenterWorld(Vector3Int globalCell)
        {
            return globalGrid.CellToWorld(globalCell) + (Vector3)(globalGrid.cellSize * 0.5f);
        }
        
        private bool CanMoveWorld(Vector3 worldPos)
        {
            if (!HasTileAny_World(_groundMaps, worldPos))
                return false;
            
            if (HasTileAny_World(_blockedMaps, worldPos))
                return false;

            return true;
        }

        private static bool HasTileAny_World(List<Tilemap> maps, Vector3 worldPos)
        {
            if (maps == null) return false;

            for (int i = 0; i < maps.Count; i++)
            {
                Tilemap m = maps[i];
                if (m == null) continue;

                Vector3Int cell = m.WorldToCell(worldPos);
                if (m.HasTile(cell)) return true;
            }
            return false;
        }
        
        private void RecordNeighbors_GlobalCells()
        {
            foreach (NodeData nodeData in bakedData.points)
            {
                nodeData.neighbours.Clear();

                Vector3Int current = nodeData.cellPosition;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        Vector3Int next = current + new Vector3Int(dx, dy, 0);

                        if (bakedData.TryGetNode(next, out NodeData adjacent))
                        {
                            if (CheckCorner_Global(next, current))
                                nodeData.AddNeighbour(adjacent);
                        }
                    }
                }
            }
        }

        private bool CheckCorner_Global(Vector3Int next, Vector3Int current)
        {
            if (!isCornerCheck) return true;

            int dx = next.x - current.x;
            int dy = next.y - current.y;

            bool isDiagonal = dx != 0 && dy != 0;
            if (!isDiagonal) return true;

            Vector3 wA = GetGlobalCellCenterWorld(new Vector3Int(next.x, current.y, 0));
            Vector3 wB = GetGlobalCellCenterWorld(new Vector3Int(current.x, next.y, 0));

            return CanMoveWorld(wA) && CanMoveWorld(wB);
        }
        
        private void SaveIfInUnityEditor()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(bakedData);
            AssetDatabase.SaveAssets();
#endif
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!isDrawGizmos || bakedData == null || bakedData.points == null) return;

            foreach (NodeData nodeData in bakedData.points)
            {
                Gizmos.color = nodeColor;
                Gizmos.DrawWireSphere(nodeData.worldPosition, 0.15f);

                foreach (LinkData link in nodeData.neighbours)
                {
                    Gizmos.color = edgeColor;
                    DrawArrowGizmos(link.startPosition, link.endPosition);
                }
            }
        }

        private void DrawArrowGizmos(Vector3 from, Vector3 to)
        {
            Vector3 direction = (from - to).normalized;

            Vector3 arrowStart = to - direction * 0.2f;
            Vector3 arrowEnd = to - direction * 0.15f;
            const float arrowSize = 0.05f;

            Vector3 triangleA = arrowStart + (Quaternion.Euler(0, 0, -90f) * direction) * arrowSize;
            Vector3 triangleB = arrowStart + (Quaternion.Euler(0, 0, 90f) * direction) * arrowSize;

            Gizmos.DrawLine(from, arrowStart);
            Gizmos.DrawLine(triangleA, arrowEnd);
            Gizmos.DrawLine(triangleB, arrowEnd);
            Gizmos.DrawLine(triangleA, triangleB);
        }
#endif
        
    }
}
