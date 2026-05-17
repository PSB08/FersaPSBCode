using PSB.Code.BattleCode.Events;
using PSW.Code.Battle;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Defines;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Enemies
{
    public class EntityDeadShader : MonoBehaviour, IModule
    {
        [SerializeField] private Material deadMat;
        [SerializeField] private DamageColorListSO colorList;
        
        private SpriteRenderer _render;
        private Elemental _lastDeathElement = Elemental.Normal;
        
        public void Initialize(ModuleOwner owner)
        {
            _render = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            Bus<EntityDeadElementalEvent>.OnEvent += OnDamageReceived;
        }

        private void OnDisable()
        {
            Bus<EntityDeadElementalEvent>.OnEvent -= OnDamageReceived;
        }

        private void OnDamageReceived(EntityDeadElementalEvent evt)
        {
            if (evt.Target == transform.parent.gameObject || evt.Target == gameObject)
            {
                _lastDeathElement = evt.ElementalType;
                Debug.Log("[EntityDeadShader]진짜진짜됨");
            }
        }

        public void ChangeMat()
        {
            _render.material = deadMat;
            
            if (colorList != null)
            {
                var gradient = colorList.GetColor(_lastDeathElement);
                
                if (gradient != null)
                {
                    Color targetColor = gradient.topRight;

                    if (_lastDeathElement == Elemental.Normal || _lastDeathElement == Elemental.None)
                    {
                        targetColor = Color.white;
                    }

                    _render.material.SetColor("_Color", targetColor);
                    Debug.Log($"[EntityDeadShader]{gradient.topRight}");
                }
            }
        }
        
    }
}