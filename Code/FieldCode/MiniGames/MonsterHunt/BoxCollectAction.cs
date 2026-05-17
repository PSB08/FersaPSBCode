using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using PSW.Code.Talk;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.PSB.Code.FieldCode.MapSaves;

namespace Work.PSB.Code.FieldCode.MiniGames.MonsterHunt
{
    public class BoxCollectAction : MonoBehaviour, IInteractAction
    {
        private InteractContext _ctx;
        private FieldBoxCollectible _box;

        private void Awake()
        {
            _box = GetComponent<FieldBoxCollectible>();
        }

        public void Setup(InteractContext ctx)
        {
            _ctx = ctx;
        }

        public bool CanExecute()
        {
            if (_box == null || string.IsNullOrEmpty(_box.BoxId)) return false;

            string sceneName = SceneManager.GetActiveScene().name;
            var state = SceneSaveSystem.LoadScene(sceneName);

            if (state == null || state.boxes == null) return true;

            int idx = state.boxes.FindIndex(b => b.id == _box.BoxId);
            if (idx < 0) return true;

            return state.boxes[idx].isCollected == false;
        }

        public void Execute()
        {
            int idleHash = Animator.StringToHash("IDLE");
            _box.Animator.SetParam(idleHash, false);
            
            int openHash = Animator.StringToHash("OPEN");
            _box.Animator.SetParam(openHash, true);
            
            Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
            
            Bus<TalkFinished>.Raise(new TalkFinished(
                _ctx.talkId,
                _ctx.targetEnemyId,
                _ctx.rewardKey,
                transform.position
            ));

            string sceneName = SceneManager.GetActiveScene().name;
            SceneSaveSystem.SetBoxCollected(sceneName, _box.BoxId, true);
            
            _ctx.owner.SetStartTalkFlag(false);
        }
        
    }
}