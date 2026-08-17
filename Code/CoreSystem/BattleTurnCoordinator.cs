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
using Work.PSB.Code.RunSystem;
using Random = UnityEngine.Random;


namespace Work.PSB.Code.CoreSystem
{
    public class BattleTurnCoordinator : MonoBehaviour
    {
        [SerializeField] private BattleEnterContextSO enterContext;
        [SerializeField] private TurnBeforeExecutor turnBeforeExecutor;
        [SerializeField] private CoinToss coinToss;
        [SerializeField] private SkillPanels_Controller skillPanelsController;
        [SerializeField] private TurnManagerSO tm;

        [SerializeField] bool isTuto = false;

        [Inject] private BattleEnemyManager em;
        [Inject] private PlayerManager _playerManager;
        private bool isEnemyAllDead = true;
        private void Start()
        {
            isEnemyAllDead = true;
            StartCoroutine(Co_StartTurnWhenReady());
        }

        private void OnDestroy()
        {
            if (tm != null)
            {
                tm.OnTurnStarted -= TurnStartEvent;
                tm.OnTurnEnded -= TurnEndEvent;
            }
        }

        private IEnumerator Co_StartTurnWhenReady()
        {
            BattleEnterBy by;
            if (enterContext == null || !enterContext.TryConsume(out by))
            {
                if (!RunBattleContext.IsActive)
                {
                    Debug.LogError("BattleTurnCoordinator: enterContext 미할당 또는 진입 요청 없음");
                    yield break;
                }

                by = BattleEnterBy.Player;
            }

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
            }
            Bus<OnBattleStart>.Raise(new OnBattleStart(_playerManager.BattlePlayer, em.GetEnemies()));

            yield return new WaitForSeconds(0.5f);
            //_playerManager.BattlePlayer.GetModule<EntityStat>().TryGetStat("PlayerTurnProbility", out StatSO turnProbility);

            tm.OnTurnStarted += TurnStartEvent;
            tm.OnTurnEnded += TurnEndEvent;

            //bool isPlayerTurn = RandomTurn(50);
            bool isPlayerTurn = isTuto ? true : RandomTurn(50);
            StartCoroutine(coinToss.Toss(isPlayerTurn));
        }



        private void TurnStartEvent(bool v)
        {
            if (v)
                Bus<OnTurnStart>.Raise(new OnTurnStart(_playerManager.BattlePlayer, em.GetEnemies()));
        }

        private void TurnEndEvent(bool v)
        {
            if (v)
                Bus<OnTurnEnd>.Raise(new OnTurnEnd(_playerManager.BattlePlayer, em.GetEnemies()));
        }

        public bool RandomTurn(int v)
        {
            if (tm == null) throw new Exception();

            int rand = Random.Range(1, 101);
            return rand >= v;
            
        }
    }
}
