using PSB.Code.CoreSystem.Events;
using PSW.Code.BaseSystem;
using PSW.Code.EventBus;
using UnityEngine;
using Work.CSH.Scripts.Interacts;
using YIS.Code.Events;

namespace PSB.Code.BattleCode.UIs.BossShopUI
{
    public class BossShop : MonoBehaviour, IInteractable
    {
        [field: SerializeField] public string Name { get; set; }
        public Transform Transform => transform;
        public void OnInteract()
        {
            Bus<UIPressrdEvent>.Raise(new UIPressrdEvent(UIType.BossShop));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Bus<UIPopDownEvent>.Raise(new UIPopDownEvent(UIType.BossShop));
        }

    }
}