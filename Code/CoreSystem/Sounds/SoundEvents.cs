using PSW.Code.EventBus;
using UnityEngine;

namespace Work.PSB.Code.CoreSystem.Sounds
{
    public static class SoundEvents
    {
        public static PlaySFXEvent PlaySFXEvent = new PlaySFXEvent();
        public static StopSoundEvent StopSoundEvent = new StopSoundEvent();
    }
    
    public class PlaySFXEvent : IEvent
    {
        public Vector3 position;
        public SoundSO clip;
        public int channel;

        public PlaySFXEvent Initialize(Vector3 position, SoundSO clip, int channel = 0)
        {
            this.position = position;
            this.clip = clip;
            this.channel = channel;
            
            return this;
        }
    }

    public class StopSoundEvent : IEvent
    {
        public int channel;

        public StopSoundEvent Initialize(int channel = 0)
        {
            this.channel = channel;
            return this;
        }
    }
    
}