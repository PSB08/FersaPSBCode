using UnityEngine;

namespace Work.PSB.Code.FieldCode.MapSaves
{
    public class PlayerStateHandler : MonoBehaviour
    {
        private Vector3 _initialPosition;
        private bool _cached;

        private void Awake()
        {
            if (!_cached)
            {
                _initialPosition = transform.position;
                _cached = true;
            }

            SceneObjectRegistry.RegisterPlayer(this);
        }

        public void SavePlayer(ref SceneState state)
        {
            state.playerPosition = transform.position;
        }

        public void LoadPlayer(SceneState state)
        {
            if (state == null)
            {
                ResetToInitial();
                return;
            }

            transform.position = state.playerPosition;
        }

        public void ResetToInitial()
        {
            transform.position = _initialPosition;
        }
        
    }
}