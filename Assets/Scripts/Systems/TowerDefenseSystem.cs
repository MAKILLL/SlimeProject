using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SlimeSwarm
{
    // Система круговой обороны башни: наносит урон приближающимся противникам
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SlimeMovementSystem))]
    public partial struct TowerDefenseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetTowerTag>();
            state.RequireForUpdate<SlimeTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<TargetTowerTag>(out Entity towerEntity))
                return;

            var towerTransform = SystemAPI.GetComponent<LocalTransform>(towerEntity);
            var towerComp = SystemAPI.GetComponent<TowerComponent>(towerEntity);

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Запускаем расчет зонального урона
            var job = new TowerAttackJob
            {
                TowerPosition = towerTransform.Position,
                RadiusSq = towerComp.AttackRadius * towerComp.AttackRadius,
                DamageThisFrame = towerComp.DamagePerSecond * deltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    // Параллельный джоб вычитания здоровья у слизней в радиусе поражения
    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct TowerAttackJob : IJobEntity
    {
        public float3 TowerPosition;
        public float RadiusSq;
        public float DamageThisFrame;

        public void Execute(ref HealthComponent health, in LocalTransform transform)
        {
            float distSq = math.distancesq(transform.Position, TowerPosition);

            // Если слизень подошел на расстояние выстрела башни
            if (distSq <= RadiusSq)
            {
                health.Current -= DamageThisFrame;
            }
        }
    }
}
