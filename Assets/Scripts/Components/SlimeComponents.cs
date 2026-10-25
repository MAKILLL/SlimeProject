using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SlimeSwarm
{
    // =========================================================================
    // ПРАКТИКА №1: БАЗОВЫЕ КОМПОНЕНТЫ И ТЕГИ
    // =========================================================================

    // 1. Тег слизня (маркер для фильтрации в EntityQuery)
    public struct SlimeTag : IComponentData { }

    // 2. Тег защитной башни (маркер для поиска башни-цели)
    public struct TargetTowerTag : IComponentData { }

    // 3. Здоровье сущности
    public struct HealthComponent : IComponentData
    {
        public float Current;
        public float Max;
    }

    // 4. Движение слизня
    public struct SlimeMovementComponent : IComponentData
    {
        public float MoveSpeed;   // Скорость движения (м/с)
        public float AttackRange; // Дистанция остановки у башни
    }

    // 5. Параметры башни
    public struct TowerComponent : IComponentData
    {
        public float Health;
        public float MaxHealth;
        public float AttackRadius;
        public float DamagePerSecond;
    }

    // 6. Выключаемый тег гибели (IEnableableComponent)
    // Стартовое состояние — false (выключен).
    // Включается системой SlimeHealthSystem при Health <= 0.
    public struct DeadTag : IComponentData, IEnableableComponent { }

    // =========================================================================
    // ПРАКТИКА №2: СТРУКТУРЫ ДАННЫХ (BLOB + DYNAMIC BUFFER + SINGLETON + RANDOM)
    // =========================================================================

    // Singleton-хранилище префабов (Слайд 8 методички №2)
    public struct PrefabHolder : IComponentData
    {
        public Entity GreenSlimePrefab;
        public Entity RedSlimePrefab;
    }

    // Типы врагов
    public enum EnemyType : byte
    {
        Green = 0,
        Red = 1
    }

    // Враг в волне (элемент массива Blob)
    public struct EnemyInWave
    {
        public EnemyType EnemyType;
        public int Amount;
    }

    // Волна (элемент массива Blob)
    public struct Wave
    {
        public float WaitTimeBeforeWave;
        public BlobArray<EnemyInWave> Enemies;
    }

    // Корневая структура BlobAsset для хранения неизменяемых волн (Слайды 1-4 методички №2)
    public struct WavesBlob
    {
        public BlobArray<Wave> Waves;
    }

    // Держатель ссылки на BlobAsset
    public struct WavesHolder : IComponentData
    {
        public BlobAssetReference<WavesBlob> WavesReference;
        public int CurrentWaveIndex;
        public float WaveTimer;
        public bool AllWavesSpawned;
    }

    // Динамический буфер входящего урона (Слайды 5-6 методички №2)
    // Встроенная емкость в чанке = 8 элементов (без лишних аллокаций памяти)
    [InternalBufferCapacity(8)]
    public struct DamageBufferElement : IBufferElementData
    {
        public float Value;
    }

    // Держатель генератора псевдослучайных чисел Unity.Mathematics.Random (Слайд 7 методички №2)
    public struct RandomHolder : IComponentData
    {
        public Unity.Mathematics.Random Random;
    }
}
