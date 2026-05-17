using System.Collections.Generic;
using Code.Scripts.Enemies;
using Code.Scripts.Enemies.BT;
using PSB.Code.BattleCode.BattleSystems.BattlePhases;
using PSB.Code.CoreSystem.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.PSB.Code.CoreSystem;
using Work.PSB.Code.FieldCode.BTs;
using Work.PSB.Code.FieldCode.MapSaves;
using YIS.Code.Modules;

namespace Work.PSB.Code.FieldCode
{
    [RequireComponent(typeof(LineRenderer))]
    public class FieldEnemyData : MonoBehaviour, IModule
    {
        [SerializeField] private string enemyID = "";
        [SerializeField] private BattleEncounterSO battleEncounter;
        [SerializeField] private BattlePresentationSO battlePresentation;
        [SerializeField] private BattleEnterContextSO enterContext;

        [Header("연속전투 설정")]
        [SerializeField] private float linkRadius = 4f;
        [SerializeField] private LayerMask enemyLayer;
        private LineRenderer _lineRenderer;

        private FieldEnemy _owner;
        private AgentMovement _movement;
        private FieldEnemyAngle _angle;
        private FieldEnemySensor _sensor;
        private Collider2D _collider;

        public string EnemyID => enemyID;
        public bool IsAlive = true;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as FieldEnemy;
            _movement = owner.GetModule<AgentMovement>();
            _angle = owner.GetModule<FieldEnemyAngle>();
            _sensor = owner.GetModule<FieldEnemySensor>();
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(enemyID))
            {
                Debug.LogError($"[FieldEnemyData] enemyID is empty on {name}");
            }

            SceneObjectRegistry.RegisterEnemy(this);
            _collider = GetComponent<Collider2D>();
            
            SetupLineRenderer();
        }

        private void OnDestroy()
        {
            SceneObjectRegistry.UnRegisterEnemy(this);
        }

        private void SetupLineRenderer()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.startWidth = 0.05f;
            _lineRenderer.endWidth = 0.05f;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = Color.red;
            _lineRenderer.endColor = Color.red;
            _lineRenderer.positionCount = 0;
            _lineRenderer.sortingOrder = 50;
        }

        private void Update()
        {
            if (!IsAlive)
            {
                if (_lineRenderer != null) _lineRenderer.positionCount = 0;
                return;
            }

            DrawLinkLines();
        }

        private void DrawLinkLines()
        {
            var linkedEnemies = GetLinkedEnemies();
            
            if (linkedEnemies.Count > 0)
            {
                _lineRenderer.positionCount = linkedEnemies.Count * 2;
                int idx = 0;
                
                foreach (var other in linkedEnemies)
                {
                    _lineRenderer.SetPosition(idx++, transform.position);
                    _lineRenderer.SetPosition(idx++, other.transform.position);
                }
            }
            else
            {
                _lineRenderer.positionCount = 0;
            }
        }

        private List<FieldEnemyData> GetLinkedEnemies()
        {
            List<FieldEnemyData> linked = new List<FieldEnemyData>();
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, linkRadius, enemyLayer);
            
            foreach (var col in cols)
            {
                if (col.gameObject ==gameObject) continue;
                
                if (col.TryGetComponent(out FieldEnemyData otherData))
                {
                    if (otherData.IsAlive)
                    {
                        linked.Add(otherData);
                    }
                }
            }
            return linked;
        }

        public void Hit()
        {
            if (!IsAlive) return;

            NormalFieldEnemy normalEnemy = _owner as NormalFieldEnemy;

            if (normalEnemy == null)
            {
                Debug.LogError("실패 NormalFieldEnemy가 아닙니다.");
                return;
            }

            if (enterContext != null && enterContext.HasRequest)
            {
                if (enterContext.EnterBy == BattleEnterBy.Player)
                {
                    normalEnemy.ChangeState(EnemyState.Hit);
                }
                else
                {
                    normalEnemy.ChangeState(EnemyState.Idle);
                }

                var player = SceneObjectRegistry.GetPlayer();
                if (player != null)
                { 
                    StartBattle(player);
                }
            }
            else
            {
                normalEnemy.ChangeState(EnemyState.Hit);
            }
        }

        public void StartBattle(PlayerStateHandler player)
        {
            float dirX = player.transform.position.x - transform.position.x;
            _owner.EntityRenderer.FlipController(dirX);

            var linkedEnemies = GetLinkedEnemies();
            
            BattleEncounterSO combinedEncounter = ScriptableObject.CreateInstance<BattleEncounterSO>();
            combinedEncounter.name = "Dynamic_Chain_Encounter";
            
            List<BattlePhaseData> allPhases = new List<BattlePhaseData>();
            
            List<string> involvedEnemyIDs = new List<string> { enemyID };

            if (battleEncounter != null && battleEncounter.phases != null)
            {
                allPhases.AddRange(battleEncounter.phases);
            }

            foreach (var linked in linkedEnemies)
            {
                if (linked.battleEncounter != null && linked.battleEncounter.phases != null)
                {
                    allPhases.AddRange(linked.battleEncounter.phases);
                }
                involvedEnemyIDs.Add(linked.EnemyID);
                linked.EngageInChainBattle(); 
            }

            combinedEncounter.phases = allPhases.ToArray();

            BattleContext.Set(SceneManager.GetActiveScene().name, involvedEnemyIDs);
            
            BattleRuntimeData.Set(combinedEncounter, battlePresentation);

            _collider.enabled = false;
            _movement.CanManualMove = false;
            _movement.StopImmediately();

            _movement.RigidCompo.linearVelocity = Vector2.zero;
            _movement.RigidCompo.angularVelocity = 0f;

            if (_angle != null) _angle.gameObject.SetActive(false);
            if (_sensor != null) _sensor.gameObject.SetActive(false);
            if (_lineRenderer != null) _lineRenderer.positionCount = 0;
        }

        public void EngageInChainBattle()
        {
            //IsAlive = false;
            _collider.enabled = false;
            _movement.CanManualMove = false;
            _movement.StopImmediately();
            
            if (_lineRenderer != null) _lineRenderer.positionCount = 0;
            gameObject.SetActive(false);
        }

        public void LoadEnemy(SceneState state)
        {
            if (state == null || state.enemies == null)
                return;

            int idx = state.enemies.FindIndex(e => e.id == enemyID);

            if (idx < 0)
            {
                SceneSaveSystem.SetEnemyAlive(
                    SceneManager.GetActiveScene().name,
                    enemyID,
                    IsAlive
                );
                return;
            }

            var saved = state.enemies[idx];
            IsAlive = saved.isAlive;

            if (!IsAlive)
            {
                gameObject.SetActive(false);
                return;
            }

            transform.position = saved.position;
            gameObject.SetActive(true);
        }

        public void RestoreActiveIfAlive()
        {
            if (!IsAlive) return;
            gameObject.SetActive(true);
        }

        public bool HasEnterContext()
        {
            return enterContext != null;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, linkRadius);
        }
#endif
        
    }
}