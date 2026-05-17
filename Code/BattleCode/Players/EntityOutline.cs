using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Players
{
    public class EntityOutline : MonoBehaviour, IModule
    {
        private static readonly int Color1 = Shader.PropertyToID("_OutlineColor");
        private static readonly float OutlineThickness = Shader.PropertyToID("_OutlineThickness");
        
        [SerializeField] private SpriteRenderer mySprite;
        [SerializeField] private SpriteRenderer parent;
        [SerializeField] private Material outLineMat;
        
        private Material _cloneMat;
        
        public void Initialize(ModuleOwner owner)
        {
        }
        
        private void Start()
        {
            mySprite.sprite = parent.sprite;
            
            SetupOutlineMaterial();
        }
        
        private void LateUpdate()
        {
            if (!parent || !mySprite) return;
            mySprite.sprite = parent.sprite;
            mySprite.flipX = parent.flipX;
            mySprite.flipY = parent.flipY;
        }

        private void SetupOutlineMaterial()
        {
            if (_cloneMat != null)
            {
                Destroy(_cloneMat);
            }
            
            _cloneMat = new Material(outLineMat);

            Color outLineColor = Color.white;
            _cloneMat.SetColor(Color1, outLineColor);
            _cloneMat.SetFloat((int)OutlineThickness, 0.05f);
            
            mySprite.material = _cloneMat;
        }
        
    }
}