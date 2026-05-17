using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Players;
using PSB_Lib.Dependencies;
using PSW.Code.Dial;
using PSW.Code.EventBus;
using System;
using System.Collections;
using UnityEngine;
using Work.CSH.Scripts.Battle;
using Work.CSH.Scripts.Managers;
using Work.CSH.Scripts.Relics;
using Work.CSH.Scripts.UIs;
using Random = UnityEngine.Random;


namespace Work.PSB.Code.CoreSystem
{
    public class BattleTurnCoordinator : MonoBehaviour
    {
        [SerializeField] private BattleEnterContextSO enterContext;
        [SerializeField] private TurnBeforeExecutor turnBeforeExecutor;
        [SerializeField] private TurnCoinToss turnCoinToss;
        [SerializeField] private SkillPanels_Controller skillPanelsController;
        [SerializeField] private TurnManagerSO tm;

        [Inject] private BattleEnemyManager em;
        [Inject] private PlayerManager _playerManager;
        private bool isEnemyAllDead = true;
        private void Start()
        {
            isEnemyAllDead = true;
            StartCoroutine(Co_StartTurnWhenReady());
        }

        private IEnumerator Co_StartTurnWhenReady()
        {
            if (enterContext == null)
            {
                Debug.LogError("BattleTurnCoordinator: enterContext 미할당");
                yield break;
            }

            if (!enterContext.TryConsume(out var by))
                yield break;

            while (_playerManager == null || _playerManager.BattlePlayer == null || _playerManager.BattlePlayer.TurnManager == null)
                yield return null;

            
            yield return new WaitForSeconds(0.6f);

            yield return new WaitUntil(() => turnBeforeExecutor.Execute(by));

            foreach (var e in em.GetEnemies())
            {
                if (e != null && e.gameObject.activeInHierarchy && !e.IsDead)
                {
                    isEnemyAllDead = false;
                    break;
                }
            }
            if(isEnemyAllDead)
            {
                tm.SetEnemyTurn();
                yield break;
            }
            Bus<OnBattleStart>.Raise(new OnBattleStart(_playerManager.BattlePlayer, em.GetEnemies()));

            yield return new WaitForSeconds(0.5f);
            //_playerManager.BattlePlayer.GetModule<EntityStat>().TryGetStat("PlayerTurnProbility", out StatSO turnProbility);

            bool isPlayerTurn = RandomTurn(50);
            StartCoroutine(turnCoinToss.ShowTurnTextAction(isPlayerTurn));



            


        }
        public bool RandomTurn(int v)
        {
            if (tm == null) throw new Exception();

            int rand = Random.Range(1, 101);
            return rand >= v;
            
        }
    }
}