using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SlimeSwarm
{
    // Система спавна волн противников
    // Выполняется последовательно на главном потоке, так как здесь происходит дискретная логика таймера
    [BurstCompile]
    [UpdateInGroup(typeof(WaveSystemGroup))]
    public partial struct WaveSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabHolder>();
            state.RequireForUpdate<WavesHolder>();
            state.RequireForUpdate<RandomHolder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var wavesHolder = SystemAPI.GetSingletonRW<WavesHolder>();

            if (wavesHolder.ValueRO.AllWavesSpawned || !wavesHolder.ValueRO.WavesReference.IsCreated)
                return;

            ref var wavesBlob = ref wavesHolder.ValueRO.WavesReference.Value;
            int currentWave = wavesHolder.ValueRO.CurrentWaveIndex;

            if (currentWave >= wavesBlob.Waves.Length)
            {
                wavesHolder.ValueRW.AllWavesSpawned = true;
                return;
            }

            // Увеличиваем таймер волны раз в секунду
            wavesHolder.ValueRW.WaveTimer += 1.0f;

            ref var wave = ref wavesBlob.Waves[currentWave];

            if (wavesHolder.ValueRO.WaveTimer >= wave.WaitTimeBeforeWave)
            {
                wavesHolder.ValueRW.WaveTimer = 0f;
                wavesHolder.ValueRW.CurrentWaveIndex++;

                // Генератор случайных чисел из синглтона
                var randomHolder = SystemAPI.GetSingletonRW<RandomHolder>();
                ref var rng = ref randomHolder.ValueRW.Random;

                var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                float radius = 45f;

                // Создаем сущности для каждого типа врагов в текущей волне
                for (int groupIdx = 0; groupIdx < wave.Enemies.Length; groupIdx++)
                {
                    ref var enemyGroup = ref wave.Enemies[groupIdx];
                    Entity prefabToSpawn = enemyGroup.EnemyType == EnemyType.Red 
                        ? prefabHolder.RedSlimePrefab 
                        : prefabHolder.GreenSlimePrefab;

                    if (prefabToSpawn == Entity.Null)
                        continue;

                    for (int i = 0; i < enemyGroup.Amount; i++)
                    {
                        Entity newEnemy = ecb.Instantiate(prefabToSpawn);

                        // Случайное позиционирование на окружности вокруг башни
                        float angle = rng.NextFloat(0f, math.PI * 2f);
                        float dist = radius + rng.NextFloat(-3f, 3f);

                        float3 spawnPos = new float3(
                            math.cos(angle) * dist,
                            0f,
                            math.sin(angle) * dist
                        );

                        ecb.SetComponent(newEnemy, LocalTransform.FromPositionRotationScale(spawnPos, quaternion.identity, 1f));
                    }
                }
            }
        }
    }
}
