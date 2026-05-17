using UnityEngine;
using UnityEngine.Serialization;

namespace Work.PSB.Code.CoreSystem.Tests
{
    public class PlayerLayerCode : MonoBehaviour
    {
        [SerializeField] private Transform feetPoint;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private SpriteRenderer maskingVisual;
        
        [Space(10)]
        [SerializeField] private string[] sortingLayers;
        [SerializeField] private float searchRadius = 3f;
        
        [Space(10)]
        [SerializeField] private float xPadding = 0.04f;
        
        [Space(10)]
        [SerializeField] private string forcedSortingLayerName = "Tree_forward";
        [SerializeField] private int forcedSortingOrder = 0;

        private SpriteRenderer[] _allRenderers;

        private int _defaultSortingLayerId;
        private int _defaultSortingOrder;

        private void Awake()
        {
            if (visual == null)
            {
                Debug.LogError($"{name} : visual is null");
                return;
            }

            _defaultSortingLayerId = visual.sortingLayerID;
            _defaultSortingOrder = visual.sortingOrder;
        }

        private void Start()
        {
            RefreshRenderers();
        }
        
        public void RefreshRenderers()
        {
            _allRenderers = FindObjectsByType<SpriteRenderer>
                (FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        private void LateUpdate()
        {
            ResolveLayer();
        }

        private void ResolveLayer()
        {
            if (_allRenderers == null || _allRenderers.Length == 0)
                RefreshRenderers();

            Vector3 feet = feetPoint.position;
            Bounds playerBounds = visual.bounds;

            SpriteRenderer bestRenderer = null;
            float bestScore = float.MaxValue;

            if (_allRenderers != null)
            {
                for (int i = 0; i < _allRenderers.Length; i++)
                {
                    SpriteRenderer sr = _allRenderers[i];
                    if (!CanEvaluateRenderer(sr)) continue;
                    if (!IsSortingLayer(sr.sortingLayerName)) continue;
                    if (!IsInSearchRadius(sr, feet)) continue;

                    if (!TryGetScore(sr, feet, playerBounds, out float score)) continue;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestRenderer = sr;
                    }
                }
            }

            bool forcedFront = bestRenderer != null;
            ApplyFront(forcedFront);
        }

        private bool TryGetScore(SpriteRenderer sr, Vector3 feet, Bounds playerBounds, out float score)
        {
            Bounds objBounds = BuildObjBounds(sr.bounds);

            bool valid =
                IsXOverlap(playerBounds, objBounds) &&
                IsFeetInsideObj(feet, objBounds) &&
                DoesUpper(playerBounds, objBounds);

            if (!valid)
            {
                score = 0f;
                return false;
            }

            float distX = Mathf.Abs(feet.x - objBounds.center.x);
            float distY = Mathf.Abs(feet.y - objBounds.min.y);
            score = distX * 2f + distY;
            return true;
        }

        private void ApplyFront(bool forcedFront)
        {
            if (forcedFront)
            {
                visual.sortingLayerName = forcedSortingLayerName;
                visual.sortingOrder = forcedSortingOrder;

                if (maskingVisual != null)
                    maskingVisual.enabled = false;
            }
            else
            {
                visual.sortingLayerID = _defaultSortingLayerId;
                visual.sortingOrder = _defaultSortingOrder;

                if (maskingVisual != null)
                    maskingVisual.enabled = true;
            }
        }

        private bool IsInSearchRadius(SpriteRenderer sr, Vector3 feet)
        {
            float dist = (sr.bounds.center - feet).sqrMagnitude;
            return dist <= searchRadius * searchRadius;
        }

        private Bounds BuildObjBounds(Bounds objectBounds)
        {
            float width = objectBounds.size.x * 0.28f;
            float height = objectBounds.size.y * 0.22f;

            Vector3 center = new Vector3
            (
                objectBounds.center.x,
                objectBounds.min.y + height * 0.5f,
                objectBounds.center.z
            );

            return new Bounds(center, new Vector3(width, height, objectBounds.size.z));
        }

        private bool IsXOverlap(Bounds playerBounds, Bounds objBounds)
        {
            return playerBounds.max.x >= objBounds.min.x - xPadding &&
                   playerBounds.min.x <= objBounds.max.x + xPadding;
        }

        private bool IsFeetInsideObj(Vector3 feet, Bounds objBounds)
        {
            return feet.y >= objBounds.min.y - 0.8f &&
                   feet.y <= objBounds.max.y;
        }

        private bool DoesUpper(Bounds playerBounds, Bounds objBounds)
        {
            return playerBounds.max.y >= objBounds.min.y;
        }

        private bool CanEvaluateRenderer(SpriteRenderer sr)
        {
            if (sr == null) return false;
            if (!sr.enabled) return false;
            if (sr == visual || sr == maskingVisual) return false;
            
            return true;
        }

        private bool IsSortingLayer(string layerName)
        {
            if (sortingLayers == null || sortingLayers.Length == 0)
                return true;

            for (int i = 0; i < sortingLayers.Length; i++)
            {
                if (sortingLayers[i] == layerName)
                    return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(feetPoint.position, searchRadius);
        }
        
    }
}