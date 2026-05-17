using System.Collections;
using System.Collections.Generic;
using CIW.Code;
using UnityEngine;
using Work.CSH.Scripts.PlayerComponents;

namespace Work.PSB.Code.FieldCode.Gimmicks
{
    public class MemoryPathGimmick : FieldGimmick
    {
        [Header("Path Visuals")]
        [SerializeField] private List<GameObject> normalPathObjects;
        [SerializeField] private List<GameObject> hintPathObjects;
        
        [Header("Puzzle Settings")]
        [SerializeField] private float hintShowDuration = 3f;
        [SerializeField] private bool hideNormalPathAfterHint = false;

        [Header("Trap Settings")]
        [SerializeField] private List<GameObject> trapObjects;

        private bool _isHintShowing = false;
        private bool _isPuzzleActivated = false;

        protected override void Start()
        {
            ResetGimmick();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCleared || _isHintShowing || _isPuzzleActivated) return;

            var entity = other.GetComponentInParent<Entity>();
            if (entity != null && entity is FieldPlayer)
            {
                StartCoroutine(ShowHintRoutine());
            }
        }

        private IEnumerator ShowHintRoutine()
        {
            _isHintShowing = true;
            _isPuzzleActivated = true;

            SetObjectsActive(normalPathObjects, false);
            SetObjectsActive(hintPathObjects, true);
            SetObjectsActive(trapObjects, false);

            yield return new WaitForSeconds(hintShowDuration);
            
            SetObjectsActive(normalPathObjects, !hideNormalPathAfterHint);
            SetObjectsActive(hintPathObjects, hideNormalPathAfterHint);
            SetObjectsActive(trapObjects, true);

            foreach (var trapObj in trapObjects)
            {
                if (trapObj == null) continue;
                var gimmicks = trapObj.GetComponentsInChildren<FieldGimmick>();
                foreach (var g in gimmicks) g.ResetGimmick();
            }

            _isHintShowing = false;
        }

        public void ReachGoal()
        {
            OnSuccess();
            
            SetObjectsActive(trapObjects, false);
            SetObjectsActive(normalPathObjects, false);
            SetObjectsActive(hintPathObjects, false);
        }

        public override void ResetGimmick()
        {
            base.ResetGimmick();
            StopAllCoroutines();
            
            _isHintShowing = false;
            _isPuzzleActivated = false;

            SetObjectsActive(normalPathObjects, true);
            SetObjectsActive(hintPathObjects, false);
            SetObjectsActive(trapObjects, false);
        }

        private void SetObjectsActive(List<GameObject> list, bool isActive)
        {
            if (list == null) return;
            foreach (var obj in list)
            {
                if (obj != null) obj.SetActive(isActive);
            }
        }
        
    }
}