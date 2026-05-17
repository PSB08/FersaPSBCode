using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Enemies
{
    public class EnemyAttackPreviewVfx : MonoBehaviour, IModule
    {
        [SerializeField] private GameObject previewFx;
        [SerializeField] private GameObject previewObj;

        public void Initialize(ModuleOwner owner)
        {
        }

        private void Awake()
        {
            if (previewFx != null)
                previewFx.SetActive(false);
        }

        public void SetPreview(BattleEnemy target)
        {
            if (previewFx == null || target == null)
                return;

            previewFx.transform.SetParent(target.transform, false);
            previewFx.transform.localPosition = Vector3.zero;
            previewFx.transform.localRotation = Quaternion.identity;

            if (!previewFx.activeSelf)
                previewFx.SetActive(true);
            
            previewFx.GetComponent<Animator>().Play("CheckerPlay", 0, 0f);
            previewObj.GetComponent<Animator>().Play("SelectAnimPlay", 0, 0f);
        }

        public void ClearPreview()
        {
            if (previewFx != null && previewFx.activeSelf)
                previewFx.SetActive(false);
        }
        
    }
}