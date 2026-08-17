using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Work.PSB.Code.FieldCode.MoveFeet
{
    [Serializable]
    public class TileStepSurfacePair
    {
        public TileBase[] tile;
        public StepSurfaceSO surface;
    }

    public class TilemapStepSurfaceProvider : MonoBehaviour
    {
        [Header("Tilemap")]
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private TilemapRenderer tilemapRenderer;

        [Header("Surface")]
        [SerializeField] private StepSurfaceSO defaultSurface;
        [SerializeField] private bool useDefaultSurfaceForUnmappedTiles = true;

        [SerializeField] private TileStepSurfacePair[] tileSurfaces;

        [Header("Manual Priority")]
        [SerializeField] private int manualPriority;

        private Dictionary<TileBase, StepSurfaceSO> _surfaceMap;

        public int ManualPriority => manualPriority;
        public int SiblingIndex => transform.GetSiblingIndex();

        public int SortingLayerValue
        {
            get
            {
                CacheComponents();

                if (tilemapRenderer == null)
                    return 0;

                return SortingLayer.GetLayerValueFromID(tilemapRenderer.sortingLayerID);
            }
        }

        public int SortingOrder
        {
            get
            {
                CacheComponents();

                if (tilemapRenderer == null)
                    return 0;

                return tilemapRenderer.sortingOrder;
            }
        }

        private void Awake()
        {
            CacheComponents();
            BuildMap();
        }

        private void OnValidate()
        {
            CacheComponents();
        }

        private void CacheComponents()
        {
            if (tilemap == null)
                tilemap = GetComponent<Tilemap>();

            if (tilemapRenderer == null)
                tilemapRenderer = GetComponent<TilemapRenderer>();
        }

        private void BuildMap()
        {
            _surfaceMap = new Dictionary<TileBase, StepSurfaceSO>();

            if (tileSurfaces == null)
                return;

            foreach (TileStepSurfacePair pair in tileSurfaces)
            {
                if (pair == null)
                    continue;

                if (pair.tile == null || pair.surface == null)
                    continue;

                foreach (TileBase tile in pair.tile)
                {
                    if (tile == null)
                        continue;

                    _surfaceMap[tile] = pair.surface;
                }
            }
        }

        public bool TryGetSurface(Vector3 worldPosition, out StepSurfaceSO surface)
        {
            surface = null;

            CacheComponents();

            if (tilemap == null)
                return false;

            if (_surfaceMap == null)
                BuildMap();

            Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
            TileBase tile = tilemap.GetTile(cellPosition);

            if (tile == null)
                return false;

            if (_surfaceMap != null && _surfaceMap.TryGetValue(tile, out StepSurfaceSO mappedSurface))
            {
                if (mappedSurface != null)
                {
                    surface = mappedSurface;
                    return true;
                }
            }
            
            if (useDefaultSurfaceForUnmappedTiles && defaultSurface != null)
            {
                surface = defaultSurface;
                return true;
            }

            return false;
        }
        
    }
}