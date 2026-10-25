using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SlimeSwarm
{
    // =========================================================================
    // ПРАКТИКА №3: ПАРАЛЛЕЛЬНАЯ АТАКА БАШНИ (IJobEntity)
    // Слайды 2-3 методички:
    // Башня наносит урон всем слизням, вошедшим в её радиус поражения.
    // Параллельный джоб обходит всех слизней на разных ядрах процессора.
    // Гонки данных нет: каждый поток модифицирует только своего слизня (HealthComponent).
    // =========================================================================
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
            // Находим башню
            if (!SystemAPI.TryGetSingletonEntity<TargetTowerTag>(out Entity towerEntity))
                return;

            var towerTransform = SystemAPI.GetComponent<LocalTransform>(towerEntity);
            var towerComp = SystemAPI.GetComponent<TowerComponent>(towerEntity);

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Создаем джоб и передаем параметры зоны поражения
            var job = new TowerAttackJob
            {
                TowerPosition = towerTransform.Position,
                RadiusSq = towerComp.AttackRadius * towerComp.AttackRadius,
                DamageThisFrame = towerComp.DamagePerSecond * deltaTime
            };

            // Запускаем расчет урона параллельно
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithNone(typeof(DeadTag))] // Атакуем только живых слизней
    public partial struct TowerAttackJob : IJobEntity
    {
        public float3 TowerPosition;
        public float RadiusSq;
        public float DamageThisFrame;

        // Execute выполняется параллельно для множества сущностей
        // ref HealthComponent — запись урона
        // in LocalTransform — чтение положения слизня
        public void Execute(ref HealthComponent health, in LocalTransform transform)
        {
            float distSq = math.distancesq(transform.Position, TowerPosition);

            // Если слизень находится в радиусе действия башни
            if (distSq <= RadiusSq)
            {
                health.Current -= DamageThisFrame;
            }
        }
    }
}
