using System.Collections;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.Playables;
using Work.PSB.Code.FieldCode.MapSaves;

namespace Work.PSB.Code.FieldCode
{
    public class AllDieEnemiesOpenPortal : MonoBehaviour
    {
        [SerializeField] private PlayableDirector normalPortalEntity;

        private void OnEnable()
        {
            Bus<EnemyAllNotAlive>.OnEvent += OnAllEnemiesDead;
        }

        private void OnDisable()
        {
            Bus<EnemyAllNotAlive>.OnEvent -= OnAllEnemiesDead;
        }

        private void OnAllEnemiesDead(EnemyAllNotAlive evt)
        {
            normalPortalEntity.Play();
        }
        
    }
}