using Svelto.ECS.Example.Survive.Characters;
using Svelto.ECS.Example.Survive.Characters.Enemies;
using Svelto.ECS.Example.Survive.HUD;
using Svelto.Factories;

namespace Svelto.ECS.Example.Survive
{
    class EnemyFactory : IEnemyFactory
    {
        public EnemyFactory(IGameObjectFactory gameObjectFactory,
                            IEntityFactory entityFactory)
        {
            _gameobjectFactory = gameObjectFactory;
            _entityFactory = entityFactory;
        }
        
        public void Build(EnemySpawnData enemySpawnData, ref EnemyAttackStruct enemyAttackstruct)
        {
            // Find a random index between zero and one less than the number of spawn points.
            // Create an instance of the enemy prefab at the randomly selected spawn point position and rotation.
            var go = _gameobjectFactory.Build(enemySpawnData.enemyPrefab);
            var implementors = go.GetComponentsInChildren<IImplementor>();
            //using the GameObject GetInstanceID() will help to directly use the result of Unity functions
            //to index the entity in the Svelto database
            var initializer = _entityFactory.BuildEntity<EnemyEntityDescriptor>(new EGID(go.GetInstanceID(),ECSGroups.ActiveEnemies), 
                                                                                implementors);
            initializer.Init(enemyAttackstruct);
            initializer.Init(new HealthEntityStruct { currentHealth = 100 });
            initializer.Init(new ScoreValueEntityStruct { scoreValue = (int)(enemySpawnData.targetType + 1) * 10 });
            initializer.Init(new EnemyEntityStruct { enemyType = enemySpawnData.targetType});
            initializer.Init(new EnemySinkStruct { sinkAnimSpeed = 2.5f}); //being lazy, should come from the json too

            var transform = go.transform;
            var spawnInfo = enemySpawnData.spawnPoint;
                           
            transform.position = spawnInfo;
        }

        readonly IGameObjectFactory _gameobjectFactory;
        readonly IEntityFactory     _entityFactory;
    }
}