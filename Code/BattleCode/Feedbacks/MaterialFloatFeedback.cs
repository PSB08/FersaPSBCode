using DG.Tweening;
using UnityEngine;
using YIS.Code.Feedbacks;

namespace PSB.Code.BattleCode.Feedbacks
{
    //피드백 프리팹이 실제 적 Renderer를 런타임에 받아서 쉐이더를 적용할 수 있게 하는 인터페이스
    public interface IRendererTargetFeedback
    {
        void SetTargetRenderer(Renderer renderer);
    }

    public class MaterialFloatFeedback : Feedback, IRendererTargetFeedback
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private bool findRendererInParent = true;
        //원래 적 스프라이트 텍스처를 임시 머티리얼에 바인딩할지 결정
        [SerializeField] private bool bindSpriteTexture = true;
        //있으면 원본 머티리얼 대신 이 머티리얼을 복제해서 사용
        [SerializeField] private Material overrideMaterial;
        //스프라이트 크기에 맞춰 Sweep 시작, 끝 값을 자동 보정할지 결정
        [SerializeField] private bool useSpriteUvSweepRange = true;
        
        //스프라이트 대각선 범위 대비 빛줄기 폭 비율
        [SerializeField, Min(0f)] private float normalizedShineWidth = 0.18f;
        //Sweep이 스프라이트 바깥에서 시작, 끝나도록 더하는 여유 비율
        [SerializeField, Min(0f)] private float normalizedSweepPadding = 0.08f;
        
        //자동 범위 계산을 끄는 경우 사용할 시작 값
        [SerializeField] private float fromValue = 0f;
        //자동 범위 계산을 끄는 경우 사용할 종료 값
        [SerializeField] private float toValue = 1f;
        
        //쉐이더 값이 from에서 to까지 움직이는 시간
        [SerializeField, Min(0f)] private float duration = 0.35f;
        [SerializeField] private Ease ease = Ease.Linear;
        //피드백 종료 시 원래 메테리얼로 되돌릴지 결정
        [SerializeField] private bool restoreOriginalMaterialOnStop = true;
        
        private Renderer _resolvedRenderer;
        private Material _runtimeMaterial;
        private Material _originalMaterial;
        private Tween _tween;
        private bool _createdRuntimeMaterial;

        public void SetTargetRenderer(Renderer renderer)
        {
            //외부 컨트롤러가 실제 적 Renderer를 피드백에 주입
            targetRenderer = renderer;
        }

        public override void PlayFeedback()
        {
            //대상 SpriteRenderer의 원본 텍스처를 유지한 채 런타임 메테리얼로 쓸기 값을 애니메이션
            Renderer renderer = ResolveRenderer();
            if (renderer == null)
            {
                //Renderer가 없으면 쉐이더를 적용할 수 없으므로 중단
                Debug.LogError("[MaterialFloatFeedback] Target Renderer is missing.", this);
                return;
            }

            //문자열 프로퍼티 이름을 빠른 int id
            int propertyId = Shader.PropertyToID("_Sweep");
            //원본 메테리얼 또는 override 메테리얼 복제본을 준비
            _runtimeMaterial = ResolveMaterial(renderer);
            //적 스프라이트가 사라지지 않도록 원본 텍스처를 런타임 메테리얼에 연결
            BindSourceSpriteTexture(renderer, _runtimeMaterial);

            if (_runtimeMaterial == null)
            {
                Debug.LogError("[MaterialFloatFeedback] Runtime Material is missing.", this);
                return;
            }

            if (!_runtimeMaterial.HasProperty(propertyId))
            {
                Debug.LogError($"[MaterialFloatFeedback] Material has no _Sweep property.", this);
                return;
            }
            
            float startValue = fromValue;
            float endValue = toValue;
            //스프라이트 크기에 맞는 시작, 끝 값으로 보정
            ConfigureSpriteSweepRange(renderer, _runtimeMaterial, ref startValue, ref endValue);

            _tween?.Kill();
            _runtimeMaterial.SetFloat(propertyId, startValue); //시작 값을 먼저 세팅
            ApplyRuntimeMaterial(renderer); //Renderer에 런타임 메테리얼을 적용
            _tween = _runtimeMaterial
                .DOFloat(endValue, propertyId, duration)
                .SetEase(ease); //지정 시간 동안 쉐이더 값을 끝 값까지 움직이기
        }

        public override void StopFeedback()
        {
            //피드백 종료 시 임시 메테리얼을 정리하고 원래 메테리얼로 되돌리기
            
            _tween?.Kill();
            _tween = null;

            Renderer renderer = _resolvedRenderer;
            Material originalMaterial = _originalMaterial;
            Material runtimeMaterial = _runtimeMaterial;

            if (restoreOriginalMaterialOnStop && renderer != null && originalMaterial != null)
            {
                //원래 메테리얼 복원이 켜져 있으면 Renderer를 원상복구
                renderer.sharedMaterial = originalMaterial;
            }

            if (_createdRuntimeMaterial && runtimeMaterial != null)
            {
                //직접 생성한 런타임 메테리얼은 메모리에 남지 않게 제거
                Destroy(runtimeMaterial);
            }

            _createdRuntimeMaterial = false;
            _resolvedRenderer = null;
            _runtimeMaterial = null;
            _originalMaterial = null;
        }

        private Renderer ResolveRenderer()
        {
            if (targetRenderer != null)
            {
                //이미 주입된 Renderer가 있으면 그대로 사용
                return targetRenderer;
            }

            if (findRendererInParent)
            {
                //피드백 프리팹이 적 하위에 붙은 경우 부모에서 Renderer 찾기
                targetRenderer = GetComponentInParent<Renderer>();
            }

            //찾은 Renderer 또는 null을 반환
            return targetRenderer;
        }

        private Material ResolveMaterial(Renderer renderer)
        {
            _resolvedRenderer = renderer; //StopFeedback에서 복구할 수 있게 Renderer를 저장
            _originalMaterial = renderer.sharedMaterial; //원래 sharedMaterial을 저장
            _createdRuntimeMaterial = false; //기본값은 직접 생성하지 않은 메테리얼

            if (overrideMaterial == null)
            {
                //override가 없으면 Unity가 제공하는 renderer.material 인스턴스를 사용
                Material materialInstance = renderer.material;
                //renderer.material이 원본과 다르면 Unity가 만든 인스턴스이므로 정리 대상
                _createdRuntimeMaterial = materialInstance != null && materialInstance != _originalMaterial;
                return materialInstance;
            }

            //override 메테리얼은 원본 에셋을 건드리지 않도록 복제
            Material runtimeMaterial = new Material(overrideMaterial);
            _createdRuntimeMaterial = true;
            return runtimeMaterial;
        }

        private void ApplyRuntimeMaterial(Renderer renderer)
        {
            if (_createdRuntimeMaterial && overrideMaterial != null && renderer != null && _runtimeMaterial != null)
            {
                //override 복제 메테리얼을 Renderer에 실제 적용
                renderer.sharedMaterial = _runtimeMaterial;
            }
        }

        private void BindSourceSpriteTexture(Renderer renderer, Material material)
        {
            if (!bindSpriteTexture || material == null)
            {
                return;
            }

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            if (spriteRenderer == null && findRendererInParent)
            {
                spriteRenderer = GetComponentInParent<SpriteRenderer>();
            }

            //SpriteRenderer의 sprite texture를 우선 사용
            Texture texture = spriteRenderer != null && spriteRenderer.sprite != null
                ? spriteRenderer.sprite.texture
                : null;

            if (texture == null && _originalMaterial != null && _originalMaterial.HasProperty("_MainTex"))
            {
                //sprite texture가 없으면 원래 메테리얼의 _MainTex를 fallback으로 사용
                texture = _originalMaterial.GetTexture("_MainTex");
            }

            if (texture == null)
            {
                Debug.LogError("[MaterialFloatFeedback] Source sprite texture is missing.", this);
                return;
            }

            if (material.HasProperty("_MainTex"))
            {
                //Built-in 스타일 메인 텍스처 슬롯에 원본 텍스처
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseMap"))
            {
                //URP 스타일 BaseMap 슬롯에도 원본 텍스처
                material.SetTexture("_BaseMap", texture);
            }
        }

        private void ConfigureSpriteSweepRange(Renderer renderer, Material material, ref float startValue, ref float endValue)
        {
            //서로 다른 크기의 스프라이트에서도 대각선 쓸기가 끝까지 지나가도록 sprite bounds로 범위를 계산
            if (!useSpriteUvSweepRange || material == null)
                return;

            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            if (spriteRenderer == null && findRendererInParent)
                spriteRenderer = GetComponentInParent<SpriteRenderer>();

            Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            if (sprite == null)
                return;

            Bounds bounds = sprite.bounds;
            Vector2[] sweepPoints =
            {
                new(bounds.min.x, bounds.min.y),
                new(bounds.min.x, bounds.max.y),
                new(bounds.max.x, bounds.min.y),
                new(bounds.max.x, bounds.max.y),
            };

            float minDiagonal = float.PositiveInfinity;
            float maxDiagonal = float.NegativeInfinity;

            foreach (Vector2 point in sweepPoints)
            {
                float diagonal = point.y - point.x;
                minDiagonal = Mathf.Min(minDiagonal, diagonal);
                maxDiagonal = Mathf.Max(maxDiagonal, diagonal);
            }

            float diagonalRange = maxDiagonal - minDiagonal;
            if (diagonalRange <= Mathf.Epsilon)
                return;

            float shineWidth = Mathf.Max(diagonalRange * normalizedShineWidth, 0.0001f);
            int widthPropertyId = Shader.PropertyToID("_ShineWidth");
            if (material.HasProperty(widthPropertyId))
                material.SetFloat(widthPropertyId, shineWidth);

            float padding = Mathf.Max(shineWidth, diagonalRange * normalizedSweepPadding);
            startValue = minDiagonal - padding;
            endValue = maxDiagonal + padding;
        }

        private void OnDestroy()
        {
            StopFeedback();
        }
        
    }
}
