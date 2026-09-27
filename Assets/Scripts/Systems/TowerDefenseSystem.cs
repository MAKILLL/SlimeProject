using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SlimeSwarm
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SlimeSimulationSystem))]
    public partial struct TowerDefenseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetTowerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            float3 towerPos = float3.zero;
            float attackRadius = 16f;
            float damagePerSecond = 80f;
            bool hasTower = false;

            foreach (var (towerComp, towerTransform) in SystemAPI.Query<RefRO<TowerComponent>, RefRO<LocalTransform>>().WithAll<TargetTowerTag>())
            {
                towerPos = towerTransform.ValueRO.Position;
                attackRadius = towerComp.ValueRO.AttackRadius;
                damagePerSecond = towerComp.ValueRO.DamagePerSecond;
                hasTower = true;
                break;
            }

            if (!hasTower)
                return;

            float towerRadiusSq = attackRadius * attackRadius;
            float damageToSlimes = damagePerSecond * deltaTime;

            // Запускаем параллельный джоб зонального урона
            var job = new TowerDefenseDamageJob
            {
                TowerPos = towerPos,
                TowerRadiusSq = towerRadiusSq,
                DamageToSlimes = damageToSlimes
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct TowerDefenseDamageJob : IJobEntity
    {
        public float3 TowerPos;
        public float TowerRadiusSq;
        public float DamageToSlimes;

        public void Execute(ref HealthComponent health, in LocalTransform transform)
        {
            float distSq = math.distancesq(transform.Position, TowerPos);
            if (distSq <= TowerRadiusSq)
            {
                health.Current -= DamageToSlimes;
            }
        }
    }
}
