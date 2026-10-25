using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SlimeSwarm
{
    // =========================================================================
    // ПРАКТИКА №3: ПАРАЛЛЕЛЬНАЯ СИСТЕМА ДВИЖЕНИЯ СЛИЗНЕЙ (IJobEntity)
    // Слайды 1-3 методички:
    // Когда в игре тысячи однотипных сущностей (слизней), их перемещение
    // разбивается между всеми ядрами/потоками процессора.
    // Вместо последовательного цикла foreach мы используем структуру IJobEntity.
    // =========================================================================
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SlimeMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Система обновляется только если на сцене есть слизни
            state.RequireForUpdate<SlimeTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Находим сущность целевой башни
            if (!SystemAPI.TryGetSingletonEntity<TargetTowerTag>(out Entity towerEntity))
                return;

            // Слайд 7: компонент-лукап для безопасного чтения трансформа башни
            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            float3 towerPosition = transformLookup[towerEntity].Position;

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Слайд 3: Создаем джоб движения и передаем ему необходимые параметры
            var job = new SlimeMovementJob
            {
                TargetPosition = towerPosition,
                DeltaTime = deltaTime
            };

            // Слайд 3: job.ScheduleParallel(state.Dependency) ставит задачу в очередь планировщика
            // и возвращает новый JobHandle, обновляя state.Dependency
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    // Слайд 2: Структура джоба реализует интерфейс IJobEntity
    [BurstCompile]
    [WithNone(typeof(DeadTag))] // Фильтр: обрабатываем только живых слизней
    public partial struct SlimeMovementJob : IJobEntity
    {
        public float3 TargetPosition;
        public float DeltaTime;

        // Слайд 2: Метод Execute выполняется параллельно на рабочих потоках
        // ref LocalTransform — запись (положение и поворот меняются)
        // in SlimeMovementComponent — только чтение параметров движения
        public void Execute(ref LocalTransform transform, in SlimeMovementComponent movement)
        {
            // Вектор направления к башне в горизонтальной плоскости (XZ)
            float3 toTarget = TargetPosition - transform.Position;
            toTarget.y = 0f;

            float distanceSq = math.lengthsq(toTarget);

            // Если слизень еще не подошел вплотную к башне — шагаем вперед
            if (distanceSq > movement.AttackRange * movement.AttackRange)
            {
                float3 direction = math.normalize(toTarget);

                // Поворачиваем слизня лицом к башне
                transform.Rotation = quaternion.LookRotationSafe(direction, math.up());

                // Перемещаем вперед со скоростью MoveSpeed
                transform.Position += direction * (movement.MoveSpeed * DeltaTime);
            }
        }
    }
}
