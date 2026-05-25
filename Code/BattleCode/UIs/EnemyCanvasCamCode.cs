using UnityEngine;

namespace PSB.Code.BattleCode.UIs
{
    public class EnemyCanvasCamCode : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;

        private void Awake()
        {
            canvas.worldCamera = Camera.main;
        }
        
    }
}