using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SlimeSwarm
{
    // Классы для настройки волн в инспекторе Unity
    [Serializable]
    public class EnemiesWave
    {
        public float WaitTimeBeforeWave = 3f;
        public List<WaveEnemy> EnemiesInWave = new List<WaveEnemy>();
    }

    [Serializable]
    public class WaveEnemy
    {
        public EnemyType EnemyType;
        public int Amount;
    }

    [DisallowMultipleComponent]
    public class WavesAuthoring : MonoBehaviour
    {
        [Header("Конфигурация волн")]
        public List<EnemiesWave> _waves = new List<EnemiesWave>();

        public class WavesAuthoringBaker : Baker<WavesAuthoring>
        {
            public override void Bake(WavesAuthoring authoring)
            {
                // Формируем неизменяемый бинарный BlobAsset через BlobBuilder
                using BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var root = ref blobBuilder.ConstructRoot<WavesBlob>();

                int wavesCount = authoring._waves != null ? authoring._waves.Count : 0;
                var wavesBuilderArray = blobBuilder.Allocate(ref root.Waves, wavesCount);

                for (int i = 0; i < wavesCount; i++)
                {
                    var waveConfig = authoring._waves[i];
                    wavesBuilderArray[i].WaitTimeBeforeWave = waveConfig.WaitTimeBeforeWave;

                    int enemiesCount = waveConfig.EnemiesInWave != null ? waveConfig.EnemiesInWave.Count : 0;
                    var enemyBuilderArray = blobBuilder.Allocate(ref wavesBuilderArray[i].Enemies, enemiesCount);

                    for (int j = 0; j < enemiesCount; j++)
                    {
                        var enemyConfig = waveConfig.EnemiesInWave[j];
                        enemyBuilderArray[j] = new EnemyInWave
                        {
                            EnemyType = enemyConfig.EnemyType,
                            Amount = enemyConfig.Amount
                        };
                    }
                }

                // Создаем постоянную ссылку на собранный BlobAsset
                var wavesBlobAssetReference = blobBuilder.CreateBlobAssetReference<WavesBlob>(Allocator.Persistent);

                // Регистрируем BlobAsset, чтобы Unity отслеживал его жизненный цикл
                AddBlobAsset(ref wavesBlobAssetReference, out _);

                Entity entity = GetEntity(TransformUsageFlags.None);

                // Компонент с ссылкой на данные волн
                AddComponent(entity, new WavesHolder
                {
                    WavesReference = wavesBlobAssetReference,
                    CurrentWaveIndex = 0,
                    WaveTimer = 0f,
                    AllWavesSpawned = false
                });

                // Инициализируем генератор случайных чисел со случайным сидом
                AddComponent(entity, new RandomHolder
                {
                    Random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, 1000000))
                });
            }
        }
    }
}
