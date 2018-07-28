using System.Collections;
using UnityEngine;
using Svelto.Tasks;

namespace Svelto.ECS.Example.Survive.Characters.Player.Gun
{
    public class PlayerGunShootingEngine : MultiEntitiesEngine<GunEntityViewStruct, PlayerEntityViewStruct>, 
        IQueryingEntitiesEngine
    {
        public IEntitiesDB entitiesDB { set; private get; }

        public void Ready()
        {
            _taskRoutine.Start();
        }
        
        public PlayerGunShootingEngine(IRayCaster rayCaster, ITime time)
        {
            _rayCaster             = rayCaster;
            _time                  = time;
            _taskRoutine           = TaskRunner.Instance.AllocateNewTaskRoutine().SetEnumerator(Tick())
                                               .SetScheduler(StandardSchedulers.physicScheduler);
        }

        protected override void Add(ref GunEntityViewStruct entityView)
        {}

        protected override void Remove(ref GunEntityViewStruct entityView)
        {
            _taskRoutine.Stop();
        }

        protected override void Add(ref PlayerEntityViewStruct entityView)
        {}

        protected override void Remove(ref PlayerEntityViewStruct entityView)
        {
            _taskRoutine.Stop();
        }

        IEnumerator Tick()
        {
            while (entitiesDB.HasAny<PlayerEntityViewStruct>() == false || entitiesDB.HasAny<GunEntityViewStruct>() == false)
            {
                yield return null; //skip a frame
            }

            int count;
            var playerGunEntities = entitiesDB.QueryEntities<GunEntityViewStruct>(out count);
            var playerEntities = entitiesDB.QueryEntities<PlayerInputDataStruct>(out count);
            
            while (true)
            {
                var playerGunComponent = playerGunEntities[0].gunComponent;

                playerGunComponent.timer += _time.deltaTime;
                
                if (playerEntities[0].fire &&
                    playerGunComponent.timer >= playerGunEntities[0].gunComponent.timeBetweenBullets)
                    Shoot(playerGunEntities[0]);

                yield return null;
            }
        }

        /// <summary>
        /// Design note: shooting and find a target are possibly two different responsabilities
        /// and probably would need two different engines. 
        /// </summary>
        /// <param name="playerGunEntityView"></param>
        void Shoot(GunEntityViewStruct playerGunEntityView)
        {
            var playerGunComponent    = playerGunEntityView.gunComponent;
            var playerGunHitComponent = playerGunEntityView.gunHitTargetComponent;

            playerGunComponent.timer = 0;

            Vector3 point;
            var entityHit = _rayCaster.CheckHit(playerGunComponent.shootRay,
                                                playerGunComponent.range,
                                                ENEMY_LAYER,
                                                SHOOTABLE_MASK | ENEMY_MASK,
                                                out point);
            
            if (entityHit != -1)
            {
                var damageInfo =
                    new
                        DamageInfo(playerGunComponent.damagePerShot,
                                   point,
                                   EntityDamagedType.Enemy);
                
                //note how the GameObject GetInstanceID is used to identify the entity as well
                entitiesDB.ExecuteOnEntity(entityHit, ref damageInfo,
                                               (ref TargetEntityViewStruct entity, ref DamageInfo info) => //
                                               { //never catch external variables so that the lambda doesn't allocate
                                                   entity.damageInfo = info;
                                               });
            }

            playerGunHitComponent.targetHit.value = false;
        }

        readonly IRayCaster            _rayCaster;
        readonly ITime                 _time;
        readonly ITaskRoutine          _taskRoutine;

        static readonly int SHOOTABLE_MASK = LayerMask.GetMask("Shootable");
        static readonly int ENEMY_MASK     = LayerMask.GetMask("Enemies");
        static readonly int ENEMY_LAYER    = LayerMask.NameToLayer("Enemies");
    }
}