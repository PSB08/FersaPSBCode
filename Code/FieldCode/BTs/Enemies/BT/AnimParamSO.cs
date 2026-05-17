using UnityEngine;

namespace Code.Scripts.Enemies.BT
{
    [CreateAssetMenu(fileName = "AnimParam", menuName = "SO/Anim/Param", order = 0)]
    public class AnimParamSO : ScriptableObject
    {
        public string paramName;

        public int paramHash;

        private void OnValidate()
        {
            if(!string.IsNullOrEmpty(paramName))
                paramHash = Animator.StringToHash(paramName);
        }
    }
}