using PSW.Code.EventBus;
using UnityEngine;

namespace Work.PSB.Code.CoreSystem.Sounds
{
    public class SFXSoundPlayer : MonoBehaviour
    {
        [SerializeField] private SoundSO sfxSound;

        public void PlaySfx()
        {
            if (sfxSound == null || sfxSound.clip == null) return;
            
            Bus<PlaySFXEvent>.Raise(SoundEvents.PlaySFXEvent.Initialize(transform.position, sfxSound));
        }
        
    }
}