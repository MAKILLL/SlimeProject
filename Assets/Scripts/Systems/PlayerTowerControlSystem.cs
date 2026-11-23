using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SlimeSwarm
{
    // Система перемещения защитной платформы башни на основе данных пользовательского ввода
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SlimeMovementSystem))]
    public partial struct PlayerTowerControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetTowerTag>();
            state.RequireForUpdate<PlayerInputData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var input = SystemAPI.GetSingleton<PlayerInputData>();
            if (math.lengthsq(input.MoveDirection) < 0.001f)
                return;

            float deltaTime = SystemAPI.Time.DeltaTime;
            float moveSpeed = 12f;

            if (SystemAPI.TryGetSingletonEntity<TargetTowerTag>(out Entity towerEntity))
            {
                var transform = SystemAPI.GetComponentRW<LocalTransform>(towerEntity);
                float3 newPos = transform.ValueRO.Position + input.MoveDirection * (moveSpeed * deltaTime);

                // Ограничиваем перемещение платформы в пределах игровой площадки
                newPos.x = math.clamp(newPos.x, -40f, 40f);
                newPos.z = math.clamp(newPos.z, -40f, 40f);
                newPos.y = 1.5f;

                transform.ValueRW.Position = newPos;
            }
        }
    }
}
