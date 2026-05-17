using System.Collections;
using System.Collections.Generic;
using CIW.Code;
using CIW.Code.Player.Field;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.Gimmicks
{
    public class SlowTileZone : MonoBehaviour
    {
        [Header("Slow Settings")]
        [SerializeField] private float startMultiplier = 0.7f;
        [SerializeField] private float minMultiplier = 0.15f;
        [SerializeField] private float timeToMin = 3.0f;

        private Dictionary<Entity, Coroutine> _activeRoutines = new Dictionary<Entity, Coroutine>();

        private void OnDisable()
        {
            foreach (var routine in _activeRoutines.Values)
            {
                if (routine != null) StopCoroutine(routine);
            }
            _activeRoutines.Clear();
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            Entity entity = other.GetComponent<Entity>();
            if (entity != null)
            {
                PlayerFieldMovement movement = entity.GetModule<PlayerFieldMovement>();
                if (movement != null)
                {
                    if (_activeRoutines.ContainsKey(entity))
                    {
                        StopCoroutine(_activeRoutines[entity]);
                    }
                    
                    Coroutine routine = StartCoroutine(SlowDownRoutine(movement));
                    _activeRoutines[entity] = routine;
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Entity entity = other.GetComponent<Entity>();
            if (entity != null && _activeRoutines.ContainsKey(entity))
            {
                StopCoroutine(_activeRoutines[entity]);
                _activeRoutines.Remove(entity);

                PlayerFieldMovement movement = entity.GetModule<PlayerFieldMovement>();
                if (movement != null)
                {
                    movement.SetMoveSpeedMultiplier(1f);
                }
            }
        }

        private IEnumerator SlowDownRoutine(PlayerFieldMovement movement)
        {
            float elapsedTime = 0f;

            movement.SetMoveSpeedMultiplier(startMultiplier);

            while (elapsedTime < timeToMin)
            {
                elapsedTime += Time.deltaTime;
                
                float currentMultiplier = Mathf.Lerp(startMultiplier, minMultiplier, elapsedTime / timeToMin);
                movement.SetMoveSpeedMultiplier(currentMultiplier);

                yield return null;
            }

            movement.SetMoveSpeedMultiplier(minMultiplier);
        }
        
    }
}