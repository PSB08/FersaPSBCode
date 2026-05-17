using UnityEngine;

namespace PSB.Code.BattleCode.UIs
{
    public class EndPanelSizeToggle_Model : ModelCompo<Vector2>
    {
        [SerializeField] private SizeData sizeData;
        [SerializeField] private float popTime = 0.25f;

        private bool _isUp;

        public override Vector2 InitModel()
        {
            _isUp = false;
            return sizeData.Down;
        }

        public float GetPopTime() => popTime;

        public Vector2 GetShowSize()
        {
            _isUp = true;
            return sizeData.Up;
        }

        public Vector2 GetHideSize()
        {
            _isUp = false;
            return sizeData.Down;
        }

        public bool IsShown => _isUp;
        
    }
}