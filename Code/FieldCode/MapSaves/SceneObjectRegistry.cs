using System.Collections.Generic;
using System.Linq;
using PSW.Code.Talk;
using Work.PSB.Code.FieldCode.Gimmicks;

namespace Work.PSB.Code.FieldCode.MapSaves
{
    public static class SceneObjectRegistry
    {
        private static PlayerStateHandler _player;
        private static readonly List<FieldEnemyData> Enemies = new();
        private static readonly List<FieldGimmick> Gimmicks = new List<FieldGimmick>();
        private static readonly List<TalkEntity> Talks = new List<TalkEntity>();

        public static void RegisterPlayer(PlayerStateHandler handler) => _player = handler;
        public static PlayerStateHandler GetPlayer() => _player;
        
        public static FieldEnemyData FindEnemy(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return null;
            return Enemies.FirstOrDefault(e => e != null && e.EnemyID == enemyId);
        }
        
        public static void RegisterEnemy(FieldEnemyData enemy)
        {
            if (enemy == null) return;
            if (!Enemies.Contains(enemy))
                Enemies.Add(enemy);
        }
        
        public static void UnRegisterEnemy(FieldEnemyData enemy)
            => Enemies.Remove(enemy);
        
        public static IReadOnlyList<FieldEnemyData> GetEnemies() => Enemies;
        
        public static void RegisterGimmick(FieldGimmick gimmick)
        {
            if (!Gimmicks.Contains(gimmick))
            {
                Gimmicks.Add(gimmick);
            }
        }

        public static List<FieldGimmick> GetGimmicks()
        {
            return Gimmicks;
        }

        public static void RegisterTalk(TalkEntity talk)
        {
            if (!Talks.Contains(talk)) 
                Talks.Add(talk);
        }

        public static List<TalkEntity> GetTalks()
        {
            return Talks;
        }
        
        public static void UnregisterGimmick(FieldGimmick gimmick)
            => Gimmicks.Remove(gimmick);

        public static void UnregisterTalk(TalkEntity talk)
            => Talks.Remove(talk);

        public static void Clear()
        {
            _player = null;
            Enemies.Clear();
            Gimmicks.Clear();
            Talks.Clear();
        }
        
    }
}