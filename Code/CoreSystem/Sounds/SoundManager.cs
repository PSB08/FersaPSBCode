using System;
using System.Collections.Generic;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSW.Code.EventBus;
using UnityEngine;

namespace Work.PSB.Code.CoreSystem.Sounds
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private PoolItemSO soundPlayer;
        [SerializeField] private float sameSoundCool = 0.05f;

        [Inject] private PoolManagerMono _poolManager;
        
        private Dictionary<int, SoundPlayer> _soundPlayerDict = new Dictionary<int, SoundPlayer>();
        private Dictionary<AudioClip, float> _lastPlayTimeDict = new Dictionary<AudioClip, float>();

        private void Awake()
        {
            Bus<PlaySFXEvent>.OnEvent += HandlePlaySfxEvent;
            Bus<StopSoundEvent>.OnEvent += HandleStopSoundEvent;
        }

        private void OnDestroy()
        {
            Bus<PlaySFXEvent>.OnEvent -= HandlePlaySfxEvent;
            Bus<StopSoundEvent>.OnEvent -= HandleStopSoundEvent;
        }
        
        private void HandlePlaySfxEvent(PlaySFXEvent evt)
        {
            if (evt.clip == null) return;

            if (!evt.clip.loop)
            {
                if (_lastPlayTimeDict.TryGetValue(evt.clip.clip, out float lastPlayTime))
                {
                    if (Time.time - lastPlayTime < sameSoundCool)
                    {
                        return; 
                    }
                }
                _lastPlayTimeDict[evt.clip.clip] = Time.time;
            }

            SoundPlayer player = _poolManager.Pop<SoundPlayer>(soundPlayer);
            player.transform.position = evt.position;
            player.PlaySound(evt.clip);

            if (evt.channel > 0 && evt.clip.loop)
            {
                if (_soundPlayerDict.TryGetValue(evt.channel, out SoundPlayer beforePlayer))
                {
                    Debug.Log($"beforePlayer : {beforePlayer}");
                    beforePlayer.StopAndGotoPool();
                    _soundPlayerDict.Remove(evt.channel);
                }
                _soundPlayerDict.Add(evt.channel, player);
            }
            else if (evt.channel <= 0 && evt.clip.loop)
            {
                Debug.LogWarning($"사운드 루프 설정이 되었으나 채널이 0 이하입니다. {evt.clip.name}");
            }
        }

        private void HandleStopSoundEvent(StopSoundEvent evt)
        {
            if (_soundPlayerDict.TryGetValue(evt.channel, out SoundPlayer beforePlayer))
            {
                beforePlayer.StopAndGotoPool();
                _soundPlayerDict.Remove(evt.channel);
            }
        }
        
    }
}