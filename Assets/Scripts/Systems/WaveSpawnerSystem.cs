using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SlimeSwarm
{
    // Система спавна волн по методичке:
    // 1. Помещена в кастомную группу WaveSystemGroup (стр. 9-10)
    // 2. Читает префабы из Singleton-компонента PrefabHolder (стр. 8)
    // 3. Читает статическую конфигурацию волн из BlobAsset (стр. 1-5)
    // 4. Использует EntityCommandBuffer для спавна (стр. 7)
    // 5. Использует Unity.Mathematics.Random для рандомизации (стр. 7)
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
            // Получаем Singleton префабов
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();

            // Получаем Singleton держателя волн Blob
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

            // Наращиваем таймер волны
            wavesHolder.ValueRW.WaveTimer += 1.0f; // Вызывается раз в секунду из WaveSystemGroup

            ref var wave = ref wavesBlob.Waves[currentWave];

            if (wavesHolder.ValueRO.WaveTimer >= wave.WaitTimeBeforeWave)
            {
                wavesHolder.ValueRW.WaveTimer = 0f;
                wavesHolder.ValueRW.CurrentWaveIndex++;

                // Генератор случайных чисел из Singleton RandomHolder
                var randomHolder = SystemAPI.GetSingletonRW<RandomHolder>();
                ref var rng = ref randomHolder.ValueRW.Random;

                // Создаем буфер команд
                var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                float radius = 45f;

                // Спавним всех врагов текущей волны из вложенного массива Blob
                for (int enemyGroupIdx = 0; enemyGroupIdx < wave.Enemies.Length; enemyGroupIdx++)
                {
                    ref var enemyGroup = ref wave.Enemies[enemyGroupIdx];
                    Entity prefabToSpawn = enemyGroup.EnemyType == EnemyType.Red 
                        ? prefabHolder.RedSlimePrefab 
                        : prefabHolder.GreenSlimePrefab;

                    if (prefabToSpawn == Entity.Null)
                        continue;

                    for (int i = 0; i < enemyGroup.Amount; i++)
                    {
                        Entity newEnemy = ecb.Instantiate(prefabToSpawn);

                        // Случайная позиция на окружности с помощью Unity.Mathematics.Random
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
