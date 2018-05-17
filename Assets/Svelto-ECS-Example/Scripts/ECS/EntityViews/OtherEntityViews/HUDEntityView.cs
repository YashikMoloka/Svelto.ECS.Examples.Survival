namespace Svelto.ECS.Example.Survive.HUD
{
    public struct HUDEntityView : IEntityData
    {
        public IAnimationComponent      HUDAnimator;
        public IDamageHUDComponent      damageImageComponent;
        public IHealthSliderComponent   healthSliderComponent;
        public IScoreComponent          scoreComponent;
        public EGID ID { get; set; }
    }
}
