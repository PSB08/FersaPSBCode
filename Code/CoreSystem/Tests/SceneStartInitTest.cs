using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.Players;
using PSW.Code.EventBus;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Work.PSB.Code.FieldCode.MapSaves;
using Work.Scripts.UI;

namespace Work.PSB.Code.CoreSystem.Tests
{
    public class SceneStartInitTest : MonoBehaviour
    {
        [SerializeField] private StageUIDataSO stageDataSO;
        [SerializeField] private TransitionController transitionController;
        
        private IEnumerator Start()
        {
            yield return null;

            var backupPlayer = SceneObjectRegistry.GetPlayer();

            SceneObjectRegistry.Clear();
            
            if (backupPlayer != null)
            {
                SceneObjectRegistry.RegisterPlayer(backupPlayer);
            }

            stageDataSO.DeleteJson();
            PlayerHealthSave.Reset();
            Bus<VillageResetEvent>.Raise(new VillageResetEvent());
        }

        #if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
                stageDataSO.DeleteJson();
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                transitionController.nextScene = "PSB_Tuto_Field";
                transitionController.Transition();
            }

            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                transitionController.nextScene = "SW_Title";
                transitionController.Transition();
            }
        }
        #endif
        
    }
}