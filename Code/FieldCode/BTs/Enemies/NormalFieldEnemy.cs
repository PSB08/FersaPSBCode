using PSB.Code.FieldCode.BTs.Events;
using Work.PSB.Code.FieldCode.BTs;

namespace Code.Scripts.Enemies
{
    public class NormalFieldEnemy : FieldEnemy
    {
        private StateChange _stateChannel;

        protected override void Start()
        {
            _stateChannel = GetBlackboardVariable<StateChange>("StateChange").Value;
            EntityRenderer.OnDeadEndTrigger += DestroyObject;
        }

        private void OnDestroy()
        {
            EntityRenderer.OnDeadEndTrigger -= DestroyObject;
        }
        
        private void DestroyObject()
        {
            Destroy(gameObject);
        }
        
        public void ChangeState(EnemyState newState)
        {
            _stateChannel?.SendEventMessage(newState);
        }
        
    }
}