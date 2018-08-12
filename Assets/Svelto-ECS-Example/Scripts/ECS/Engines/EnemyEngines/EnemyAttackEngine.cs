using System.Collections;
using Svelto.Tasks;
using UnityEngine;

namespace Svelto.ECS.Example.Survive.Characters.Enemies
{
    public class EnemyAttackEngine : SingleEntityEngine<EnemyTargetEntityViewStruct>, IQueryingEntitiesEngine
    {
        public IEntitiesDB entitiesDB { set; private get; }

        public void Ready()
        {}

        public EnemyAttackEngine(ITime time)
        {
            _time = time;
            _taskRoutine = TaskRunner.Instance.AllocateNewTaskRoutine().SetEnumerator(CheckIfHittingEnemyTarget()).SetScheduler(StandardSchedulers.physicScheduler);
        }

        protected override void Add(ref EnemyTargetEntityViewStruct entity)
        {
            _taskRoutine.Start();
        }

        protected override void Remove(ref EnemyTargetEntityViewStruct entity)
        {
            _taskRoutine.Stop();
        }

        IEnumerator CheckIfHittingEnemyTarget()
        {
            while (true)
            {
                // Pay attention to this bit. The engine is querying a
                // EnemyTargetEntityView and not a PlayerEntityView.
                // this is more than a sophistication, it actually the implementation
                // of the rule that every engine must use its own set of
                // EntityViews to promote encapsulation and modularity
                while (entitiesDB.HasAny<EnemyTargetEntityViewStruct>(ECSGroups.PlayerGroup) == false ||
                       entitiesDB.HasAny<EnemyAttackEntityView>(ECSGroups.ActiveEnemiesGroup) == false)
                {
                    yield return null;
                }
                
                int targetsCount;
                var targetEntities =
                    entitiesDB.QueryEntities<EnemyTargetEntityViewStruct>(ECSGroups.PlayerGroup,
                                                                          out targetsCount);
                
                int enemiesCount;
                var enemiesAttackData = entitiesDB.QueryEntities<EnemyAttackStruct>(ECSGroups.ActiveEnemiesGroup, out enemiesCount);
                var enemies = entitiesDB.QueryEntities<EnemyAttackEntityView>(ECSGroups.ActiveEnemiesGroup, out enemiesCount);
                
                //this is more complex than needed code is just to show how you can use entity structs
                //this case is banal, entity structs should be use to handle hundreds or thousands
                //of entities in a cache friendly and multi threaded code. However entity structs would allow
                //the creation of entity without any allocation, so they can be handy for
                //cases where entity should be built fast! Theoretically is possible to create
                //a game using only entity structs, but entity structs make sense ONLY if they
                //hold value types, so they come with a lot of limitations
                for (int enemyIndex = 0; enemyIndex < enemiesCount; enemyIndex++)
                {
                    var enemyAttackEntityView = enemies[enemyIndex];
                    
                    enemiesAttackData[enemyIndex].entityInRange = enemyAttackEntityView.targetTriggerComponent.entityInRange;
                }

                for (int enemyTargetIndex = 0; enemyTargetIndex < targetsCount; enemyTargetIndex++)
                {
                    var targetEntityView = targetEntities[enemyTargetIndex];

                    for (int enemyIndex = 0; enemyIndex < enemiesCount; enemyIndex++)
                    {
                        if (enemiesAttackData[enemyIndex].entityInRange.collides == true)
                        {
                            //the IEnemyTriggerComponent implementors sets a the collides boolean
                            //whenever anything enters in the trigger range, but there is not more logic
                            //we have to check here if the colliding entity is actually an EnemyTarget
                            if (enemiesAttackData[enemyIndex].entityInRange.otherEntityID.GID == targetEntityView.ID.GID)
                            {
                                enemiesAttackData[enemyIndex].timer += _time.deltaTime;
                                
                                if (enemiesAttackData[enemyIndex].timer >= enemiesAttackData[enemyIndex].timeBetweenAttack)
                                {
                                    enemiesAttackData[enemyIndex].timer = 0.0f;
                                    
                                    var damageInfo = new DamageInfo(enemiesAttackData[enemyIndex]
                                                                       .attackDamage,
                                                                    Vector3.zero);
                
                                    //note how the GameObject GetInstanceID is used to identify the entity as well
                                    entitiesDB.ExecuteOnEntity(targetEntityView.ID, ref damageInfo,
                                                               (ref DamageableEntityStruct entity, ref DamageInfo info) => //
                                                               {      //never catch external variables so that the lambda doesn't allocate
                                                                   entity.damageInfo = info;
                                                               });
                                }
                            }
                        }
                    }
                }

                yield return null;
            }
        }


        readonly ITime                 _time;
        readonly ITaskRoutine          _taskRoutine;
    }
}
