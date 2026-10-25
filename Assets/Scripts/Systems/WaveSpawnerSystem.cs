using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SlimeSwarm
{
    // =========================================================================
    // ПРАКТИКИ №2 И №3: ПОСЛЕДОВАТЕЛЬНАЯ СИСТЕМА СПАВНА ВОЛН
    // Почему эта система НЕ распараллеливается (Слайд 1 методички №3):
    // "А вот система спавна – отсчитывает время по таймеру и раз в X секунд создаёт
    // нового Entity. В ней абсолютно нечего вычислять параллельно. Весь процесс –
    // однородный и последовательный, а значит – можно обойтись одним потоком."
    //
    // Архитектурные элементы из Практики №2:
    // 1. Помещена в кастомную группу WaveSystemGroup (срабатывает 1 раз в секунду).
    // 2. Читает префабы из Singleton-компонента PrefabHolder.
    // 3. Читает неизменяемую конфигурацию волн из плоской памяти BlobAsset.
    // 4. Использует EntityCommandBuffer для пакетного спавна.
    // 5. Использует Unity.Mathematics.Random для расчета позиций спавна на кольце.
    // =========================================================================
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
            // Получаем ссылки на синглтоны префабов и волн
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();
            var wavesHolder = SystemAPI.GetSingletonRW<WavesHolder>();

            if (wavesHolder.ValueRO.AllWavesSpawned || !wavesHolder.ValueRO.WavesReference.IsCreated)
                return;

            ref var wavesBlob = ref wavesHolder.ValueRO.WavesReference.Value;
            int currentWave = wavesHolder.ValueRO.CurrentWaveIndex;

            // Если все волны из Blob-массива уже вышли
            if (currentWave >= wavesBlob.Waves.Length)
            {
                wavesHolder.ValueRW.AllWavesSpawned = true;
                return;
            }

            // Наращиваем таймер волны (вызывается 1 раз в секунду из WaveSystemGroup)
            wavesHolder.ValueRW.WaveTimer += 1.0f;

            ref var wave = ref wavesBlob.Waves[currentWave];

            // Проверяем, наступило ли время выхода текущей волны
            if (wavesHolder.ValueRO.WaveTimer >= wave.WaitTimeBeforeWave)
            {
                wavesHolder.ValueRW.WaveTimer = 0f;
                wavesHolder.ValueRW.CurrentWaveIndex++;

                // Генератор псевдослучайных чисел из синглтона RandomHolder
                var randomHolder = SystemAPI.GetSingletonRW<RandomHolder>();
                ref var rng = ref randomHolder.ValueRW.Random;

                // Создаем буфер команд для создания сущностей
                var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                float radius = 45f;

                // Спавним всех врагов текущей волны из вложенного массива Blob
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
                        // Создаем сущность из префаба через ECB
                        Entity newEnemy = ecb.Instantiate(prefabToSpawn);

                        // Случайная позиция на окружности с помощью Unity.Mathematics.Random
                        float angle = rng.NextFloat(0f, math.PI * 2f);
                        float dist = radius + rng.NextFloat(-3f, 3f);

                        float3 spawnPos = new float3(
                            math.cos(angle) * dist,
                            0f,
                            math.sin(angle) * dist
                        );

                        // Устанавливаем координаты сущности
                        ecb.SetComponent(newEnemy, LocalTransform.FromPositionRotationScale(spawnPos, quaternion.identity, 1f));
                    }
                }
            }
        }
    }
}
