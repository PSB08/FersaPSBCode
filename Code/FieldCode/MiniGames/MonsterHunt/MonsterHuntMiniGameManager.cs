using System;
using System.Collections.Generic;
using PSW.Code.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Work.PSB.Code.FieldCode.MiniGames.MonsterHunt
{
    public class MonsterHuntMiniGameManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float timeLimitSeconds = 20f;

        [Header("Runtime")]
        [SerializeField] private float timeLeft;
        [SerializeField] private int total;
        [SerializeField] private int cleared;

        [Header("UI")] 
        [SerializeField] private TextMeshProUGUI timeLeftTxt;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI totalText;
        [SerializeField] private TextMeshProUGUI clearedText;
        [SerializeField] private TextMeshProUGUI fiftyText;
        [SerializeField] private TextMeshProUGUI seventyFiveText;
        [SerializeField] private TextMeshProUGUI ninetyText;
        
        [Header("Events")] 
        public UnityEvent OnFailGame;
        public UnityEvent OnEndMinigame;

        private readonly List<FieldMonsterData> _alive = new();
        private bool _running;

        private void Start()
        {
            TextUpdater();
        }

        private void Update()
        {
            if (!_running) return;

            timeLeft -= Time.deltaTime;
            
            TextUpdater();
            
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                EndByTimeOver();
            }
        }

        private void TextUpdater()
        {
            if (timerText != null)
            {
                int totalSeconds = Mathf.CeilToInt(timeLeft);
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;

                timerText.text = $"{minutes:00}:{seconds:00}";
            }

            if (totalText != null)
                totalText.text = $"총합 : {total}마리";
            
            if (clearedText != null)
                clearedText.text = $"현재 잡은 수 : {cleared}";

            int target50 = Mathf.CeilToInt(total * 0.5f);
            int target75 = Mathf.CeilToInt(total * 0.75f);
            int target90 = Mathf.CeilToInt(total * 0.9f);

            if (fiftyText != null)
                fiftyText.text = $"목표1 : {target50}";

            if (seventyFiveText != null)
                seventyFiveText.text = $"목표2 : {target75}";

            if (ninetyText != null)
                ninetyText.text = $"목표3 : {target90}";
        }
        
        public void StartGame(FieldMonsterData[] monstersInScene)
        {
            _alive.Clear();

            if (monstersInScene != null)
            {
                foreach (var m in monstersInScene)
                {
                    if (m == null) continue;
                    if (!m.IsAlive) continue;

                    if (!_alive.Contains(m))
                        _alive.Add(m);
                }
            }

            total = _alive.Count;
            cleared = 0;

            timeLeft = timeLimitSeconds;
            _running = true;
            
            if (total == 0)
            {
                Finish(success: true, reachedTier: 90);
            }
        }
        
        public void OnMonsterCaptured(FieldMonsterData monster, bool despawn = true)
        {
            if (!_running) return;
            if (monster == null) return;
            if (!monster.IsAlive) return;
            if (total <= 0) return;
            if (!_alive.Contains(monster)) return;

            if (despawn)
                monster.ApplyDead();

            _alive.Remove(monster);
            cleared++;

            if (cleared >= total)
            {
                int tier = EvaluateRewardTier();
                Finish(success: true, reachedTier: tier);
            }
        }

        private void EndByTimeOver()
        {
            if (!_running) return;

            Bus<MonsterHuntTimeOver>.Raise(new MonsterHuntTimeOver());

            DespawnAllRemaining();

            int tier = EvaluateRewardTier();
            bool success = tier >= 50;

            Finish(success, tier);
        }

        private void DespawnAllRemaining()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                var m = _alive[i];
                if (m == null) continue;
                m.ApplyDead();
            }
            _alive.Clear();
        }
        
        private int EvaluateRewardTier()
        {
            if (total <= 0) return 90;

            float p = (float)cleared / total;

            if (p >= 0.90f) return 90;
            if (p >= 0.75f) return 75;
            if (p >= 0.50f) return 50;
            return 0;
        }

        private void Finish(bool success, int reachedTier)
        {
            if (!_running) return;
            _running = false;
            
            if (success)
            {
                RaiseTierEventsAtEnd(reachedTier);
            }
            else
            {
                FailNoReward();
            }

            Bus<MonsterHuntFinished>.Raise(new MonsterHuntFinished
            {
                success = success,
                reachedTier = reachedTier,
                cleared = cleared,
                total = total
            });

            OnEndMinigame?.Invoke();

            totalText.text = "";
            clearedText.text = "";
            timerText.text = "";
            fiftyText.text = "";
            seventyFiveText.text = "";
            ninetyText.text = "";
            timeLeftTxt.text = "";
        }

        private void RaiseTierEventsAtEnd(int reachedTier)
        {
            if (reachedTier >= 50)
                Bus<MonsterHuntReached50>.Raise(new MonsterHuntReached50());

            if (reachedTier >= 75)
                Bus<MonsterHuntReached75>.Raise(new MonsterHuntReached75());

            if (reachedTier >= 90)
                Bus<MonsterHuntReached90>.Raise(new MonsterHuntReached90());
        }

        private void FailNoReward()
        {
            Debug.Log("<color=red>Fail</color>");
            OnFailGame?.Invoke();
        }
        
    }
}
