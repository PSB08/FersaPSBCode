using UnityEngine;

namespace Work.PSB.Code.CoreSystem.Sounds
{
    [CreateAssetMenu(fileName = "Sound clip", menuName = "SO/Sound", order = 0)]
    public class SoundSO : ScriptableObject
    {
        public enum AudioTypes { SFX, MUSIC }
        
        public AudioTypes audioType;
        public AudioClip clip;
        public bool loop = false;
        public bool randomizePitch = false;
        
        [Range(0, 1f)]
        public float randomPitchModifier = 0.1f;
        [Range(0.1f, 2f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        
        [Header("Spatial")]
        [Range(0f, 1f)]
        public float spatialBlend = 0f;

        public float minDistance = 1f;
        public float maxDistance = 10f;
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
        public float dopplerLevel = 0f;
        
    }
}