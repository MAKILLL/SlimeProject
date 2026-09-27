using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SlimeSwarm
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TowerDefenseSystem))]
    public partial struct SlimeHealthSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SlimeTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // 1. Находим слизней с нулевым здоровьем, переключаем выключаемый DeadTag
            foreach (var (health, slimeState, entity) in SystemAPI.Query<RefRO<HealthComponent>, RefRW<SlimeStateComponent>>()
                     .WithNone<DeadTag>()
                     .WithEntityAccess())
            {
                if (health.ValueRO.Current <= 0f)
                {
                    slimeState.ValueRW.State = SlimeState.Dead;
                    // Включаем DeadTag по методичке
                    ecb.SetComponentEnabled<DeadTag>(entity, true);
                    // Уничтожаем погибшую сущность
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
