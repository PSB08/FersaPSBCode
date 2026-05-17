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
        
        [field: SerializeField] public PoolItemSO PoolItem { get; private set; }
        public GameObject GameObject => gameObject;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void SetUpPool(Pool pool) => _myPool = pool;

        public void ResetItem()
        { }

        public void PlaySound(SoundSO data)
        {
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
            }
            _audioSource.clip = data.clip;
            _audioSource.loop = data.loop;

            if (!data.loop)
            {
                float duration = _audioSource.clip.length + 0.2f;
                DisableSound(duration);
            }

            _audioSource.Play();
        }

        private async void DisableSound(float duration)
        {
            await Awaitable.WaitForSecondsAsync(duration);
            _myPool.Push(this);
        }

        public void StopAndGotoPool()
        {
            _audioSource.Stop();
            _myPool.Push(this);
        }
        
    }
}