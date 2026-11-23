using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SlimeSwarm
{
    // Система высокопроизводительного поиска ближайшего противника в мире ECS
    // Тяжелая выборка по 10 000 сущностям выполняется с Burst, а результат пишется в синглтон для UI
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SlimeMovementSystem))]
    public partial struct FindNearestSlimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetTowerTag>();

            // Создаем сущность для передачи данных в UI, если её еще нет
            Entity infoEntity = state.EntityManager.CreateEntity(typeof(NearestSlimeInfo));
            state.EntityManager.SetComponentData(infoEntity, new NearestSlimeInfo
            {
                Distance = 0f,
                Health = 0f,
                HasTarget = false
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            if (!SystemAPI.TryGetSingletonEntity<TargetTowerTag>(out Entity towerEntity))
                return;

            float3 towerPos = SystemAPI.GetComponent<LocalTransform>(towerEntity).Position;

            float minDistanceSq = float.MaxValue;
            float nearestHp = 0f;
            bool found = false;

            // Быстрый перебор всех живых слизней
            foreach (var (health, transform) in SystemAPI.Query<RefRO<HealthComponent>, RefRO<LocalTransform>>()
                         .WithAll<SlimeTag>()
                         .WithNone<DeadTag>())
            {
                float distSq = math.distancesq(transform.ValueRO.Position, towerPos);
                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    nearestHp = health.ValueRO.Current;
                    found = true;
                }
            }

            // Записываем результат в синглтон-компонент для чтения интерфейсом
            if (SystemAPI.TryGetSingletonRW<NearestSlimeInfo>(out RefRW<NearestSlimeInfo> nearestInfo))
            {
                nearestInfo.ValueRW.HasTarget = found;
                nearestInfo.ValueRW.Distance = found ? math.sqrt(minDistanceSq) : 0f;
                nearestInfo.ValueRW.Health = nearestHp;
            }
        }
    }
}
