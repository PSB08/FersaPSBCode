using System;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace PSB.Code.BattleCode.UIs
{
    public class EnemyInfoCloseButton : MonoBehaviour
    {
        [SerializeField] private Button closeButton;

        private void Start()
        {
            closeButton.onClick.AddListener(Close);
        }

        private void Close()
        {
            Bus<EnemyInfoCloseEvent>.Raise(new EnemyInfoCloseEvent());
        }
        
    }
}