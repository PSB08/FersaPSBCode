using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PSB.Code.BattleCode.Players
{
    [CreateAssetMenu(fileName = "PlayerInput", menuName = "SO/Player/PlayerInput", order = 10)]
    public class PlayerInputSO : KeybindingInputSO, Controls.IPlayerActions
    {
        public event Action<UIType> OnUIPressrd;
        public event Action OnNextPressed;
        public event Action OnBeforePressed;
        
        private Controls _controls;
        
        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();

        }
        
        private void OnDisable()
        {
            _controls.Player.Disable();
        }
        public override InputActionAsset GetActionAsset() => _controls.asset;
        public override void StartRebinding(string actionName, int bindingIndex = -1) => StartRebinding(_controls.asset, actionName, bindingIndex);
        public override List<InputAction> GetActions()
        {
            return _controls.asset.actionMaps[0].actions.ToList();
        }

        public void OnTabInventory(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnUIPressrd?.Invoke(UIType.Inventory);
        }

        public void OnNext(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnNextPressed?.Invoke();
        }

        public void OnBefore(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnBeforePressed?.Invoke();
        }

        public void OnTabUpgrade(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnUIPressrd?.Invoke(UIType.Upgrade);
        }

        public void OnTabArchive(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnUIPressrd?.Invoke(UIType.TutoArchive);
        }

        public void OnTabQuest(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnUIPressrd?.Invoke(UIType.Quest);
        }

        public void OnTabMinimap(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnUIPressrd?.Invoke(UIType.Minimap);
        }

        
    }
}