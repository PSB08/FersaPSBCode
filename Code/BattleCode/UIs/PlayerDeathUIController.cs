using System.Collections;
using PSB.Code.BattleCode.Players;
using UnityEngine;
using Work.PSB.Code.CoreSystem;
using Work.Scripts.UI;

namespace PSB.Code.BattleCode.UIs
{
    public class PlayerDeathUIController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TransitionController controller;
        [SerializeField] private bool checkOnEnable = true;

        [Header("Size Animation")]
        [SerializeField] private EndPanelSizeToggle_Model sizeModel;
        [SerializeField] private EndPanelSizeToggle_View sizeView;

        [SerializeField] private float reviveHpRatio = 0.5f;
        [SerializeField] private StageUIDataSO stageUIDataSO;

        private bool _isShown;
        private Coroutine _showCo;

        private void Awake()
        {
            sizeView.Init(sizeModel.InitModel());
            _isShown = false;
        }

        private void OnEnable()
        {
            PlayerHealthSave.OnSnapshotChanged += OnHpChanged;

            if (checkOnEnable)
                CheckNow();
        }

        private void OnDisable()
        {
            PlayerHealthSave.OnSnapshotChanged -= OnHpChanged;

            if (_showCo != null)
            {
                StopCoroutine(_showCo);
                _showCo = null;
            }
        }

        private void OnHpChanged(float cur, float max)
        {
            if (max <= 0f) return;

            if (cur <= 0f)
            {
                if (_showCo == null)
                    _showCo = StartCoroutine(ShowCoroutine());
            }
            else
            {
                if (_showCo != null)
                {
                    StopCoroutine(_showCo);
                    _showCo = null;
                }

                Hide();
            }
        }

        public void CheckNow()
        {
            if (!PlayerHealthSave.TryLoad(out float cur, out float max))
                return;

            if (max <= 0f) return;

            if (cur <= 0f)
            {
                if (_showCo == null)
                    _showCo = StartCoroutine(ShowCoroutine());
            }
            else
            {
                Hide();
            }
        }

        private IEnumerator ShowCoroutine()
        {
            yield return null;
            _showCo = null;

            if (!PlayerHealthSave.TryLoad(out float cur, out float max))
                yield break;

            if (max <= 0f) yield break;
            if (cur > 0f) yield break;

            Show();
        }

        private void Show()
        {
            if (_isShown) return;

            _isShown = true;
            Time.timeScale = 0f;

            sizeView.AnimateTo(
                sizeModel.GetShowSize(),
                sizeModel.GetPopTime()
            );
        }

        public void Hide()
        {
            if (!_isShown) return;

            _isShown = false;
            Time.timeScale = 1f;

            sizeView.AnimateTo(
                sizeModel.GetHideSize(),
                sizeModel.GetPopTime()
            );
        }

        public void ExitBtn()
        {
            Hide();
            stageUIDataSO.DeleteJson();
            Invoke(nameof(DoTransition), sizeModel.GetPopTime());
        }

        private void DoTransition()
        {
            controller.nextScene = "PSB_Field";
            controller.Transition();
        }

        public void Revive()
        {
            if (!PlayerHealthSave.TryLoad(out float cur, out float max))
                return;

            float reviveHp = Mathf.Max(1f, max * reviveHpRatio);
            PlayerHealthSave.SaveSnapshot(reviveHp, max);
        }

        public void FullHeal()
        {
            if (!PlayerHealthSave.TryLoad(out float cur, out float max))
                return;

            PlayerHealthSave.SaveSnapshot(max, max);
        }
        
    }
}
