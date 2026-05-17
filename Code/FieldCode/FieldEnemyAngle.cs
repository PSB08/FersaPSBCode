using UnityEngine;
using UnityEngine.Rendering.Universal;
using YIS.Code.Modules;

namespace Work.PSB.Code.FieldCode
{
    public class FieldEnemyAngle : MonoBehaviour, IModule
    {
        [Header("Lights")]
        [SerializeField] private Light2D baseLight;
        [SerializeField] private Light2D progressLight;

        [Header("FOV Shape")]
        [SerializeField, Range(0f, 360f)] private float viewAngle = 90f;
        [SerializeField] private float viewDistance = 4f;
        [SerializeField, Min(3)] private int meshResolution = 30;

        [SerializeField] private float lookAngle = -90f;

        [Header("Occlusion")]
        //[SerializeField] private bool useRaycastTrim = true;
        [SerializeField] private LayerMask wallMask;

        [Header("Visual")]
        [SerializeField] private Color idleColor = new Color(1f, 0f, 0f, 0.25f);
        [SerializeField] private Color alertColor = new Color(1f, 0.15f, 0.15f, 0.65f);

        [SerializeField, Range(0f, 2f)] private float idleIntensity = 0.35f;
        [SerializeField, Range(0f, 4f)] private float alertIntensity = 1.25f;

        [SerializeField, Range(0f, 3f)] private float falloffSize = 0.75f;

        [Header("Detect Spread")]
        [SerializeField] private bool useRadiusSpread = true;
        [SerializeField] private AnimationCurve radiusCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Update")]
        [SerializeField, Range(0f, 0.5f)] private float updateInterval = 0.05f;

        private float _progress01;
        private float _timeAcc;

        private Vector3[] _pathBase;
        private Vector3[] _pathProgress;

        public float ViewAngle => viewAngle;
        public float ViewDistance => viewDistance;
        
        public void Initialize(ModuleOwner owner)
        {
        }

        private void Awake()
        {
            SetupLight(baseLight);

            if (progressLight != null)
            {
                SetupLight(progressLight);
                progressLight.enabled = false;
            }

            ApplyVisual(0f);
            ForceRebuildShape();
        }

        private static void SetupLight(Light2D l)
        {
            if (l == null) return;
            l.lightType = Light2D.LightType.Freeform;
            l.blendStyleIndex = 0;
        }

        private void LateUpdate()
        {
            ApplyVisual(_progress01);

            if (updateInterval <= 0f)
            {
                RebuildShapes();
                return;
            }

            _timeAcc += Time.deltaTime;
            if (_timeAcc >= updateInterval)
            {
                _timeAcc = 0f;
                RebuildShapes();
            }
        }

        public void SetDetectProgress(float p01)
        {
            _progress01 = Mathf.Clamp01(p01);
            ApplyVisual(_progress01);
        }

        public void ResetDetectProgress()
        {
            SetDetectProgress(0f);
        }

        public void SetDetecting(bool detecting)
        {
            if (progressLight == null) return;

            if (detecting)
            {
                _progress01 = 0f;

                progressLight.enabled = false;
                ApplyVisual(0f);
                RebuildProgressNow(0f);

                progressLight.enabled = true;
            }
            else
            {
                progressLight.enabled = false;
                _progress01 = 0f;
            }
        }
        
        private void RebuildProgressNow(float p01)
        {
            if (progressLight == null) return;

            float maxDist = viewDistance;

            if (useRadiusSpread)
            {
                float t = radiusCurve != null ? radiusCurve.Evaluate(p01) : p01;
                maxDist = Mathf.Lerp(0f, viewDistance, Mathf.Clamp01(t));
            }

            RebuildOne(progressLight, ref _pathProgress, maxDist);
        }
        
        public void ForceRebuildShape()
        {
            _timeAcc = 0f;
            RebuildShapes();
        }

        public void SetLookDirection(bool isLeft)
        {
            isLeft = !isLeft;
            lookAngle = isLeft ? 90f : -90f;
        }

        public Vector3 GetViewDirection()
        {
            float dirSign = transform.lossyScale.x >= 0 ? 1f : -1f;
            float centerAngle = -90f * dirSign;
            Quaternion rot = Quaternion.Euler(0, 0, centerAngle);
            return rot * transform.up;
        }

        private void ApplyVisual(float p01)
        {
            if (baseLight != null)
            {
                baseLight.color = idleColor;
                baseLight.intensity = idleIntensity;
                TrySetFalloff(baseLight, falloffSize);
            }
            
            if (progressLight != null)
            {
                progressLight.color = Color.Lerp(idleColor, alertColor, p01);
                progressLight.intensity = Mathf.Lerp(idleIntensity, alertIntensity, p01);
                TrySetFalloff(progressLight, falloffSize);
            }
        }

        private static void TrySetFalloff(Light2D light, float size)
        {
            try { light.shapeLightFalloffSize = size; }
            catch { /* ignore */ }
        }

        private void RebuildShapes()
        {
            RebuildOne(baseLight, ref _pathBase, viewDistance);
            
            if (progressLight != null && progressLight.enabled)
            {
                float maxDist = viewDistance;

                if (useRadiusSpread)
                {
                    float t = radiusCurve != null ? radiusCurve.Evaluate(_progress01) : _progress01;
                    maxDist = Mathf.Lerp(0f, viewDistance, Mathf.Clamp01(t));
                }

                RebuildOne(progressLight, ref _pathProgress, maxDist);
            }
        }

        private void RebuildOne(Light2D lightObj, ref Vector3[] pathLocal, float maxDist)
        {
            if (lightObj == null) return;

            // origin 1개 + arcPoints(meshResolution+1개)
            int arcCount = meshResolution + 1;
            int total = 1 + arcCount;

            if (pathLocal == null || pathLocal.Length != total)
                pathLocal = new Vector3[total];

            pathLocal[0] = Vector3.zero;

            float angleStep = viewAngle / meshResolution;
            Vector3 originWorld = transform.position;

            for (int i = 0; i <= meshResolution; i++)
            {
                float angle = lookAngle - viewAngle * 0.5f + i * angleStep;

                Quaternion rotLocal = Quaternion.Euler(0, 0, angle);
                Vector3 localDir = rotLocal * Vector3.up;

                float dist = maxDist;

                /*if (useRaycastTrim)
                {
                    Vector3 worldDir = transform.TransformDirection(localDir).normalized;
                    RaycastHit2D hit = Physics2D.Raycast(originWorld, worldDir, maxDist, wallMask);
                    if (hit.collider != null)
                        dist = hit.distance;
                }*/

                pathLocal[1 + i] = localDir * dist;
            }

            lightObj.SetShapePath(pathLocal);
        }
        
    }
}
