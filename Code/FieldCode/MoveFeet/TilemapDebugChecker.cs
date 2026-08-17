using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Work.PSB.Code.FieldCode.MoveFeet
{
    public class TilemapDebugChecker : MonoBehaviour
    {
        [SerializeField] private Transform checkTarget;
        [SerializeField] private Transform tilemapRoot;
        [SerializeField] private KeyCode checkKey = KeyCode.F9;

        [Header("Options")]
        [SerializeField] private bool sortByRendererOrder = true;
        [SerializeField] private bool pingTileAsset = true;

        private Tilemap[] _tilemaps;

        #if UNITY_EDITOR
        private void Awake()
        {
            RefreshTilemaps();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(checkKey))
            {
                Vector3 position = checkTarget != null
                    ? checkTarget.position
                    : transform.position;

                CheckPosition(position);
            }
        }

        [ContextMenu("Refresh Tilemaps")]
        public void RefreshTilemaps()
        {
            Transform root = tilemapRoot != null ? tilemapRoot : transform.root;
            _tilemaps = root.GetComponentsInChildren<Tilemap>(true);

            if (sortByRendererOrder)
            {
                System.Array.Sort(_tilemaps, CompareTilemaps);
            }

            //Debug.Log($"[TilemapDebugChecker] Tilemap Count: {_tilemaps.Length}", this);
        }

        [ContextMenu("Check Current Position")]
        public void CheckCurrentPosition()
        {
            Vector3 position = checkTarget != null
                ? checkTarget.position
                : transform.position;

            CheckPosition(position);
        }

        public void CheckPosition(Vector3 worldPosition)
        {
            if (_tilemaps == null || _tilemaps.Length == 0)
                RefreshTilemaps();

            Debug.Log($"========== Tile Check Position: {worldPosition} ==========", this);

            bool foundAny = false;

            foreach (Tilemap tilemap in _tilemaps)
            {
                if (tilemap == null)
                    continue;

                Vector3Int cell = tilemap.WorldToCell(worldPosition);
                TileBase tile = tilemap.GetTile(cell);

                if (tile == null)
                    continue;

                foundAny = true;

                TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();

                string sortingInfo = renderer != null
                    ? $"SortingLayer: {SortingLayer.IDToName(renderer.sortingLayerID)}, Order: {renderer.sortingOrder}"
                    : "No TilemapRenderer";

                string tileType = tile.GetType().Name;

                Debug.Log(
                    $"[FOUND TILE]\n" +
                    $"Tilemap: {tilemap.name}\n" +
                    $"Cell: {cell}\n" +
                    $"Tile Asset Name: {tile.name}\n" +
                    $"Tile Type: {tileType}\n" +
                    $"{sortingInfo}",
                    tilemap
                );

                if (pingTileAsset)
                {
                    EditorGUIUtility.PingObject(tile);
                    Selection.activeObject = tile;
                }
            }

            if (!foundAny)
            {
                Debug.LogWarning($"[TilemapDebugChecker] 해당 위치에 찍힌 Tile이 없습니다. Position: {worldPosition}", this);
            }

            Debug.Log("==========================================================", this);
        }

        private int CompareTilemaps(Tilemap a, Tilemap b)
        {
            if (a == null && b == null)
                return 0;

            if (a == null)
                return 1;

            if (b == null)
                return -1;

            TilemapRenderer ar = a.GetComponent<TilemapRenderer>();
            TilemapRenderer br = b.GetComponent<TilemapRenderer>();

            int aLayerValue = ar != null ? SortingLayer.GetLayerValueFromID(ar.sortingLayerID) : 0;
            int bLayerValue = br != null ? SortingLayer.GetLayerValueFromID(br.sortingLayerID) : 0;

            int layerCompare = bLayerValue.CompareTo(aLayerValue);
            if (layerCompare != 0)
                return layerCompare;

            int aOrder = ar != null ? ar.sortingOrder : 0;
            int bOrder = br != null ? br.sortingOrder : 0;

            int orderCompare = bOrder.CompareTo(aOrder);
            if (orderCompare != 0)
                return orderCompare;

            return b.transform.GetSiblingIndex().CompareTo(a.transform.GetSiblingIndex());
        }
#endif
        
    }
}