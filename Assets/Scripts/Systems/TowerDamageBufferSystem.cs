using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace SlimeSwarm
{
    // =========================================================================
    // ПРАКТИКА №2: ОБРАБОТКА ДИНАМИЧЕСКОГО БУФЕРА УРОНА БАШНИ (DynamicBuffer)
    // Слайды 5-6 методички №2:
    // DynamicBuffer<DamageBufferElement> собирает входящий урон по башне.
    // Система суммирует все элементы буфера, вычитает из здоровья и очищает буфер.
    // =========================================================================
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
            // Обрабатываем буфер урона башни
            foreach (var (tower, damageBuffer) in SystemAPI.Query<RefRW<TowerComponent>, DynamicBuffer<DamageBufferElement>>())
            {
                if (damageBuffer.IsEmpty)
                    continue;

                float totalDamage = 0f;
                for (int i = 0; i < damageBuffer.Length; i++)
                {
                    totalDamage += damageBuffer[i].Value;
                }

                // Применяем накопленный урон к здоровью башни
                tower.ValueRW.Health = math.max(0f, tower.ValueRO.Health - totalDamage);

                // Очищаем буфер для приема новых порций урона в следующем кадре
                damageBuffer.Clear();
            }
        }
    }
}
