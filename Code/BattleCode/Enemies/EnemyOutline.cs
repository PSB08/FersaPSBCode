using PSB.Code.BattleCode.Enums;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Enemies
{
    public class EnemyOutline : MonoBehaviour, IModule
    {
        private static readonly int Color1 = Shader.PropertyToID("_OutlineColor");
        private static readonly float OutlineThickness = Shader.PropertyToID("_OutlineThickness");
        
        [SerializeField] private SpriteRenderer mySprite;
        [SerializeField] private SpriteRenderer parent;
        [SerializeField] private Material outLineMat;

        private NormalBattleEnemy _entity;
        
        private Material _cloneMat;
        
        public void Initialize(ModuleOwner owner)
        {
            _entity = owner as NormalBattleEnemy;
        }
        
        private void Start()
        {
            mySprite.sprite = parent.sprite;
            
            SetupOutlineMaterial(_entity.enemySO.grade);
        }
        
        private void LateUpdate()
        {
            if (!parent || !mySprite) return;
            mySprite.sprite = parent.sprite;
            mySprite.flipX = parent.flipX;
            mySprite.flipY = parent.flipY;
        }

        private void SetupOutlineMaterial(EnemyGrade grade)
        {
            if (_cloneMat != null)
            {
                Destroy(_cloneMat);
            }
            
            _cloneMat = new Material(outLineMat);

            Color outLineColor = Color.white; //GetColorByGrade(grade);
            _cloneMat.SetColor(Color1, outLineColor);
            _cloneMat.SetFloat((int)OutlineThickness, 0.005f);
            
            mySprite.material = _cloneMat;
        }

        private Color GetColorByGrade(EnemyGrade grade)
        {
            return grade switch
            {
                EnemyGrade.Common => Color.white,
                EnemyGrade.Elite => Color.magenta,
                EnemyGrade.MiniBoss => Color.yellow,
                EnemyGrade.Boss => Color.red,
                _ => Color.white
            };
        }
        
    }
}