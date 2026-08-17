using System;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.MoveFeet
{
    public class TilemapStepSurfaceResolver : MonoBehaviour
    {
        [Header("Providers")]
        [SerializeField] private Transform providerRoot;
        [SerializeField] private bool autoCollectProvidersFromRoot = true;
        [SerializeField] private bool includeInactiveProviders = false;
        [SerializeField] private TilemapStepSurfaceProvider[] providers;

        [Header("Fallback")]
        [SerializeField] private StepSurfaceSO defaultSurface;

        public StepSurfaceSO DefaultSurface => defaultSurface;

        private bool _isSorted;

        private void Awake()
        {
            RefreshProviders();
        }

        private void OnValidate()
        {
            _isSorted = false;
        }

        public void RefreshProviders()
        {
            if (autoCollectProvidersFromRoot)
            {
                Transform root = providerRoot != null ? providerRoot : transform;
                providers = root.GetComponentsInChildren<TilemapStepSurfaceProvider>(includeInactiveProviders);
            }

            SortProviders();
        }

        public bool TryResolve(Vector3 worldPosition, out StepSurfaceSO surface)
        {
            surface = null;

            if (!_isSorted)
                SortProviders();

            if (providers == null)
                return false;

            foreach (TilemapStepSurfaceProvider provider in providers)
            {
                if (provider == null)
                    continue;

                if (provider.TryGetSurface(worldPosition, out surface))
                    return surface != null;
            }

            return false;
        }

        private void SortProviders()
        {
            if (providers == null)
            {
                _isSorted = true;
                return;
            }

            Array.Sort(providers, CompareProviders);
            _isSorted = true;
        }

        private int CompareProviders(TilemapStepSurfaceProvider a, TilemapStepSurfaceProvider b)
        {
            if (a == null && b == null)
                return 0;

            if (a == null)
                return 1;

            if (b == null)
                return -1;

            return CompareByRendererSorting(a, b);
        }

        private int CompareByRendererSorting(TilemapStepSurfaceProvider a, TilemapStepSurfaceProvider b)
        {
            int sortingLayerCompare = b.SortingLayerValue.CompareTo(a.SortingLayerValue);
            if (sortingLayerCompare != 0)
                return sortingLayerCompare;

            int sortingOrderCompare = b.SortingOrder.CompareTo(a.SortingOrder);
            if (sortingOrderCompare != 0)
                return sortingOrderCompare;

            int manualCompare = b.ManualPriority.CompareTo(a.ManualPriority);
            if (manualCompare != 0)
                return manualCompare;

            return b.SiblingIndex.CompareTo(a.SiblingIndex);
        }
        
    }
}