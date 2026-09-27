using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SlimeSwarm
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SlimeSpawnerSystem : ISystem
    {
        private Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SlimeSpawnerComponent>();
            _random = new Random(12345);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (spawner, entity) in SystemAPI.Query<RefRW<SlimeSpawnerComponent>>().WithEntityAccess())
            {
                if (spawner.ValueRO.SlimePrefab == Entity.Null)
                    continue;

                bool shouldSpawn = false;

                if (spawner.ValueRO.SpawnCount > 0)
                {
                    shouldSpawn = true;
                }
                else if (spawner.ValueRO.ContinuousSpawn)
                {
                    spawner.ValueRW.Timer += deltaTime;
                    if (spawner.ValueRW.Timer >= spawner.ValueRO.SpawnInterval)
                    {
                        spawner.ValueRW.Timer = 0f;
                        spawner.ValueRW.SpawnCount = 500; // Волна подкрепления
                        shouldSpawn = true;
                    }
                }

                if (shouldSpawn && spawner.ValueRO.SpawnCount > 0)
                {
                    int countToSpawn = spawner.ValueRO.SpawnCount;
                    spawner.ValueRW.SpawnCount = 0; // Сбрасываем разовый спавн

                    float radius = spawner.ValueRO.SpawnRadius;

                    for (int i = 0; i < countToSpawn; i++)
                    {
                        Entity newSlime = ecb.Instantiate(spawner.ValueRO.SlimePrefab);

                        // Случайный угол на окружности
                        float angle = _random.NextFloat(0f, math.PI * 2f);
                        float dist = radius + _random.NextFloat(-4f, 4f);

                        float3 spawnPos = new float3(
                            math.cos(angle) * dist,
                            0f,
                            math.sin(angle) * dist
                        );

                        // Вариация случайных характеристик для каждого слизня
                        float speedVar = _random.NextFloat(3.0f, 6.0f);
                        float jumpHeightVar = _random.NextFloat(0.8f, 1.6f);
                        float staminaVar = _random.NextFloat(30f, 60f);
                        float phaseVar = _random.NextFloat(0f, math.PI * 2f);

                        ecb.SetComponent(newSlime, LocalTransform.FromPositionRotationScale(spawnPos, quaternion.identity, 1f));

                        ecb.SetComponent(newSlime, new SlimeMovementComponent
                        {
                            MoveSpeed = speedVar,
                            JumpHeight = jumpHeightVar,
                            AttackRange = 2.0f,
                            AvoidanceRadius = 0.8f,
                            Velocity = float3.zero,
                            JumpProgress = 0f
                        });

                        ecb.SetComponent(newSlime, new StaminaComponent
                        {
                            Current = staminaVar,
                            Max = staminaVar,
                            RecoveryRate = 30f,
                            JumpCost = 15f
                        });

                        ecb.SetComponent(newSlime, new SlimeAnimationComponent
                        {
                            BaseScale = new float3(1f, 1f, 1f),
                            SquashAmount = _random.NextFloat(0.25f, 0.45f),
                            BounceSpeed = _random.NextFloat(6f, 10f),
                            PhaseOffset = phaseVar
                        });

                        ecb.SetComponent(newSlime, new SlimeStateComponent
                        {
                            State = SlimeState.Spawning,
                            StateTimer = 0f,
                            TargetStateTime = _random.NextFloat(0.2f, 0.6f)
                        });
                    }
                }
            }
        }
    }
}
