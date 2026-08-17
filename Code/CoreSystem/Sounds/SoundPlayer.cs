using PSB_Lib.ObjectPool.RunTime;
using UnityEngine;
using UnityEngine.Audio;

namespace Work.PSB.Code.CoreSystem.Sounds
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundPlayer : MonoBehaviour, IPoolable
    {
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup bgmGroup;

        private AudioSource _audioSource;
        private Pool _myPool;
        private int _playVersion;

        [field: SerializeField] public PoolItemSO PoolItem { get; private set; }
        public GameObject GameObject => gameObject;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnDestroy()
        {
            _playVersion++;
            _myPool = null;
        }

        public void SetUpPool(Pool pool)
        {
            _myPool = pool;
        }

        public void ResetItem()
        {
            _playVersion++;

            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.clip = null;
                _audioSource.loop = false;
            }
        }

        public void PlaySound(SoundSO data)
        {
            if (data == null || data.clip == null)
                return;

            _playVersion++;
            int currentVersion = _playVersion;

            _audioSource.outputAudioMixerGroup = data.audioType switch
            {
                SoundSO.AudioTypes.SFX => sfxGroup,
                SoundSO.AudioTypes.MUSIC => bgmGroup,
                _ => sfxGroup
            };

            _audioSource.volume = data.volume;
            _audioSource.pitch = data.pitch;

            if (data.randomizePitch)
            {
                _audioSource.pitch += Random.Range(-data.randomPitchModifier, data.randomPitchModifier);
                _audioSource.pitch = Mathf.Max(0.01f, _audioSource.pitch);
            }

            _audioSource.clip = data.clip;
            _audioSource.loop = data.loop;

            _audioSource.spatialBlend = data.spatialBlend;
            _audioSource.minDistance = data.minDistance;
            _audioSource.maxDistance = data.maxDistance;
            _audioSource.rolloffMode = data.rolloffMode;
            _audioSource.dopplerLevel = data.dopplerLevel;

            _audioSource.Play();

            if (!data.loop)
            {
                float duration = data.clip.length / Mathf.Abs(_audioSource.pitch) + 0.2f;
                DisableSound(duration, currentVersion);
            }
        }

        private async void DisableSound(float duration, int version)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                await Awaitable.NextFrameAsync();
            }

            if (this == null)
                return;

            if (_myPool == null)
                return;

            if (version != _playVersion)
                return;

            if (_audioSource != null)
                _audioSource.Stop();

            _myPool.Push(this);
        }

        public void StopAndGotoPool()
        {
            _playVersion++;

            if (_audioSource != null)
                _audioSource.Stop();

            if (_myPool == null)
                return;

            _myPool.Push(this);
        }
        
    }
}