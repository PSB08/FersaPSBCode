using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.CoreSystem.Sounds;
using Work.PSB.Code.FieldCode.MoveFeet;
using YIS.Code.Effects;

namespace Work.PSB.Code.FieldCode.BTs
{
    public class AnimationStepEventReceiver : MonoBehaviour
    {
        private static readonly int FootHash = Animator.StringToHash("FOOT");

        [Header("Default Step SFX")]
        [SerializeField] private SoundSO stepSound;

        [Header("Default Step VFX")]
        [SerializeField] private PoolItemSO stepVfxItem;
        [SerializeField] private Transform vfxSpawnPoint;

        [Header("Surface Resolver")]
        [SerializeField] private TilemapStepSurfaceResolver surfaceResolver;
        [SerializeField] private StepSurfaceSO defaultSurface;

        [Header("Step Sample Point")]
        [SerializeField] private Transform surfaceSamplePoint;
        [SerializeField] private Transform facingReference;

        [Header("Tile Sampling")]
        [SerializeField] private bool checkOverlayTileBeforeGroundTile = true;
        [SerializeField] private float overlaySampleUpOffset = 0.05f;
        [SerializeField] private float groundSampleDownOffset = 0.05f;

        [Header("Fallback")]
        [SerializeField] private bool playDefaultWhenSurfaceMissing = false;

        [Inject] private PoolManagerMono _poolManager;

        private StepFootSide _nextFootSide = StepFootSide.Left;
        private SoundSO _lastPlayedStepSound;

        public void OnStep()
        {
            StepFootSide footSide = _nextFootSide;

            _nextFootSide = _nextFootSide == StepFootSide.Left
                ? StepFootSide.Right
                : StepFootSide.Left;

            HandleStep(footSide);
        }

        public void OnLeftStep()
        {
            HandleStep(StepFootSide.Left);
            _nextFootSide = StepFootSide.Right;
        }

        public void OnRightStep()
        {
            HandleStep(StepFootSide.Right);
            _nextFootSide = StepFootSide.Left;
        }

        private void HandleStep(StepFootSide footSide)
        {
            bool foundSurface = TryGetCurrentSurface(footSide, out StepSurfaceSO currentSurface);

            if (!foundSurface)
            {
                if (!playDefaultWhenSurfaceMissing)
                    return;

                currentSurface = GetFallbackSurface();
            }

            SoundSO currentStepSound = GetStepSound(currentSurface, footSide);
            PoolItemSO currentStepVfxItem = GetStepVfxItem(currentSurface);

            Vector3 stepPosition = GetSurfaceSampleOrigin(footSide);

            PlayStepSound(currentStepSound, stepPosition);
            PlayStepVfx(currentStepVfxItem, footSide);

            if (currentStepSound != null)
                _lastPlayedStepSound = currentStepSound;
        }

        private bool TryGetCurrentSurface(StepFootSide footSide, out StepSurfaceSO surface)
        {
            surface = null;

            if (surfaceResolver == null)
                return false;

            Vector3 origin = GetSurfaceSampleOrigin(footSide);

            if (checkOverlayTileBeforeGroundTile)
            {
                Vector3 overlaySamplePosition =
                    origin + Vector3.up * overlaySampleUpOffset;

                if (surfaceResolver.TryResolve(overlaySamplePosition, out surface))
                    return surface != null;
            }

            Vector3 groundSamplePosition =
                origin + Vector3.down * groundSampleDownOffset;

            if (surfaceResolver.TryResolve(groundSamplePosition, out surface))
                return surface != null;

            return false;
        }

        private StepSurfaceSO GetFallbackSurface()
        {
            if (surfaceResolver != null && surfaceResolver.DefaultSurface != null)
                return surfaceResolver.DefaultSurface;

            return defaultSurface;
        }

        private Vector3 GetSurfaceSampleOrigin(StepFootSide footSide)
        {
            if (surfaceSamplePoint != null)
                return surfaceSamplePoint.position;

            if (vfxSpawnPoint != null)
                return vfxSpawnPoint.position;

            return transform.position;
        }

        private Vector3 GetVfxSpawnPosition(StepFootSide footSide)
        {
            if (vfxSpawnPoint != null)
                return vfxSpawnPoint.position;

            return GetSurfaceSampleOrigin(footSide);
        }

        private Transform GetFacingReference()
        {
            if (facingReference != null)
                return facingReference;

            return transform;
        }

        private SoundSO GetStepSound(StepSurfaceSO surface, StepFootSide footSide)
        {
            if (surface != null)
            {
                SoundSO surfaceSound = surface.GetStepSound(footSide, _lastPlayedStepSound);

                if (surfaceSound != null)
                    return surfaceSound;
            }

            return stepSound;
        }

        private PoolItemSO GetStepVfxItem(StepSurfaceSO surface)
        {
            if (surface != null && surface.StepVfxItem != null)
                return surface.StepVfxItem;

            return stepVfxItem;
        }

        private void PlayStepSound(SoundSO sound, Vector3 position)
        {
            if (sound == null)
                return;

            PlaySFXEvent soundEvt = SoundEvents.PlaySFXEvent.Initialize(position, sound);
            Bus<PlaySFXEvent>.Raise(soundEvt);
        }

        private void PlayStepVfx(PoolItemSO vfxItem, StepFootSide footSide)
        {
            if (vfxItem == null || _poolManager == null)
                return;

            var p = _poolManager.Pop<PoolAnimatorEffect>(vfxItem);
            var evt = p.GetComponentInChildren<EffectTrigger>();

            if (evt != null)
            {
                System.Action onEnd = null;

                onEnd = () =>
                {
                    evt.OnEndTrigger -= onEnd;
                    p.DestroyObj();
                };

                evt.OnEndTrigger += onEnd;
            }

            Vector3 spawnPos = GetVfxSpawnPosition(footSide);

            Transform facing = GetFacingReference();
            bool isFacingLeft = facing.lossyScale.x < 0f;

            Quaternion spawnRot = isFacingLeft
                ? Quaternion.identity
                : Quaternion.Euler(0f, 180f, 0f);

            p.PlayClipEffect(spawnPos, spawnRot, FootHash);
        }
        
    }
}