using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace SlimeSwarm
{
    // Система обработки DynamicBuffer по методичке (стр. 5-6)
    // DynamicBuffer передается без оберток RefRO/RefRW
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
            // Обрабатываем DynamicBuffer входящего урона на башне
            foreach (var (tower, damageBuffer) in SystemAPI.Query<RefRW<TowerComponent>, DynamicBuffer<DamageBufferElement>>())
            {
                if (damageBuffer.IsEmpty)
                    continue;

                float totalDamage = 0f;
                for (int i = 0; i < damageBuffer.Length; i++)
                {
                    totalDamage += damageBuffer[i].Value;
                }

                // Применяем накопленный из буфера урон
                tower.ValueRW.Health = math.max(0f, tower.ValueRO.Health - totalDamage);

                // Очищаем буфер после обработки
                damageBuffer.Clear();
            }
        }
    }
}
