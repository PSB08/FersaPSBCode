using PSB_Lib.ObjectPool.RunTime;
using UnityEngine;
using Work.PSB.Code.CoreSystem.Sounds;

namespace Work.PSB.Code.FieldCode.MoveFeet
{
    public enum StepFootSide
    {
        Left,
        Right
    }
    
    [CreateAssetMenu(fileName = "StepSurface", menuName = "SO/Map/StepSurface", order = 0)]
    public class StepSurfaceSO : ScriptableObject
    {
        public SoundSO soundEffect;
        public PoolItemSO StepVfxItem;

        [SerializeField] private SoundSO[] sharedStepSounds;
        [SerializeField] private SoundSO[] leftStepSounds;
        [SerializeField] private SoundSO[] rightStepSounds;

        public SoundSO GetStepSound(StepFootSide footSide, SoundSO lastPlayedSound)
        {
            SoundSO[] footSounds = footSide == StepFootSide.Left
                ? leftStepSounds
                : rightStepSounds;

            SoundSO selected = PickNonRepeatingSound(footSounds, lastPlayedSound);

            if (selected != null)
                return selected;

            selected = PickNonRepeatingSound(sharedStepSounds, lastPlayedSound);

            if (selected != null)
                return selected;

            return soundEffect;
        }

        private static SoundSO PickNonRepeatingSound(SoundSO[] sounds, SoundSO lastPlayedSound)
        {
            if (sounds == null || sounds.Length == 0)
                return null;

            int validCount = 0;

            for (int i = 0; i < sounds.Length; i++)
            {
                if (sounds[i] != null)
                    validCount++;
            }

            if (validCount == 0)
                return null;

            if (validCount == 1)
            {
                for (int i = 0; i < sounds.Length; i++)
                {
                    if (sounds[i] != null)
                        return sounds[i];
                }
            }

            for (int i = 0; i < 8; i++)
            {
                SoundSO candidate = sounds[UnityEngine.Random.Range(0, sounds.Length)];

                if (candidate != null && candidate != lastPlayedSound)
                    return candidate;
            }

            for (int i = 0; i < sounds.Length; i++)
            {
                if (sounds[i] != null && sounds[i] != lastPlayedSound)
                    return sounds[i];
            }

            for (int i = 0; i < sounds.Length; i++)
            {
                if (sounds[i] != null)
                    return sounds[i];
            }

            return null;
        }
        
    }
}