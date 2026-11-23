using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace SlimeSwarm
{
    // Параллельная система перемещения слизней к целевой башне
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SlimeMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SlimeTag>();
            state.RequireForUpdate<TargetTowerTag>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            // Находим башню-цель
            if (!SystemAPI.TryGetSingletonEntity<TargetTowerTag>(out Entity towerEntity))
                return;

            // Читаем позицию башни через безопасный лукап
            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            float3 towerPosition = transformLookup[towerEntity].Position;

            // Получаем физический мир для проверки препятствий лучами
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            CollisionWorld collisionWorld = physicsWorld.CollisionWorld;

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Планируем параллельный джоб перемещения
            var job = new SlimeMovementJob
            {
                TargetPosition = towerPosition,
                DeltaTime = deltaTime,
                CollisionWorld = collisionWorld
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    // Джоб движения, выполняющийся параллельно на всех доступных потоках
    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct SlimeMovementJob : IJobEntity
    {
        public float3 TargetPosition;
        public float DeltaTime;
        [ReadOnly] public CollisionWorld CollisionWorld;

        public void Execute(ref LocalTransform transform, in SlimeMovementComponent movement)
        {
            // Вектор к башне в горизонтальной плоскости
            float3 toTarget = TargetPosition - transform.Position;
            toTarget.y = 0f;

            float distanceSq = math.lengthsq(toTarget);

            if (distanceSq > movement.AttackRange * movement.AttackRange)
            {
                float3 direction = math.normalize(toTarget);

                // Выпускаем луч вперед для проверки физических преград
                RaycastInput rayInput = new RaycastInput
                {
                    Start = transform.Position + new float3(0f, 0.4f, 0f),
                    End = transform.Position + new float3(0f, 0.4f, 0f) + direction * 1.5f,
                    Filter = CollisionFilter.Default
                };

                // Проверка пересечения луча с физическими объектами в мире
                bool hitSomething = CollisionWorld.CastRay(rayInput, out RaycastHit hit);

                // Поворот в сторону башни
                transform.Rotation = quaternion.LookRotationSafe(direction, math.up());

                // Перемещение вперед
                transform.Position += direction * (movement.MoveSpeed * DeltaTime);
            }
        }
    }
}
