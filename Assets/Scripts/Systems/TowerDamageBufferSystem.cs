using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace SlimeSwarm
{
    // Система обработки динамического буфера входящего урона башни
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TowerDefenseSystem))]
    public partial struct TowerDamageBufferSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetTowerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (tower, damageBuffer) in SystemAPI.Query<RefRW<TowerComponent>, DynamicBuffer<DamageBufferElement>>())
            {
                if (damageBuffer.IsEmpty)
                    continue;

                float totalDamage = 0f;
                for (int i = 0; i < damageBuffer.Length; i++)
                {
                    totalDamage += damageBuffer[i].Value;
                }

                // Списываем суммарный накопленный урон
                tower.ValueRW.Health = math.max(0f, tower.ValueRO.Health - totalDamage);

                // Очищаем буфер для следующих попаданий
                damageBuffer.Clear();
            }
        }
    }
}
