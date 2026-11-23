using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SlimeSwarm
{
    // Тег для фильтрации сущностей слизней
    public struct SlimeTag : IComponentData { }

    // Тег для идентификации защитной башни
    public struct TargetTowerTag : IComponentData { }

    // Текущее и максимальное здоровье сущности
    public struct HealthComponent : IComponentData
    {
        public float Current;
        public float Max;
    }

    // Параметры передвижения слизня
    public struct SlimeMovementComponent : IComponentData
    {
        public float MoveSpeed;   // Скорость перемещения (м/с)
        public float AttackRange; // Дистанция остановки перед башней
    }

    // Характеристики защитной башни
    public struct TowerComponent : IComponentData
    {
        public float Health;
        public float MaxHealth;
        public float AttackRadius;
        public float DamagePerSecond;
    }

    // Выключаемый тег гибели (включается при обнулении здоровья)
    public struct DeadTag : IComponentData, IEnableableComponent { }

    // Хранилище ссылок на префабы сущностей
    public struct PrefabHolder : IComponentData
    {
        public Entity GreenSlimePrefab;
        public Entity RedSlimePrefab;
    }

    // Типы противников
    public enum EnemyType : byte
    {
        Green = 0,
        Red = 1
    }

    // Описание противника в волне для Blob-массива
    public struct EnemyInWave
    {
        public EnemyType EnemyType;
        public int Amount;
    }

    // Данные отдельной волны
    public struct Wave
    {
        public float WaitTimeBeforeWave;
        public BlobArray<EnemyInWave> Enemies;
    }

    // Неизменяемая конфигурация всех волн
    public struct WavesBlob
    {
        public BlobArray<Wave> Waves;
    }

    // Компонент, хранящий ссылку на BlobAsset с волнами
    public struct WavesHolder : IComponentData
    {
        public BlobAssetReference<WavesBlob> WavesReference;
        public int CurrentWaveIndex;
        public float WaveTimer;
        public bool AllWavesSpawned;
    }

    // Буфер входящего урона (первые 8 элементов хранятся прямо в чанке)
    [InternalBufferCapacity(8)]
    public struct DamageBufferElement : IBufferElementData
    {
        public float Value;
    }

    // Компонент с генератором случайных чисел
    public struct RandomHolder : IComponentData
    {
        public Unity.Mathematics.Random Random;
    }

    // Данные пользовательского ввода, передаваемые из MonoBehaviour в мир ECS
    public struct PlayerInputData : IComponentData
    {
        public float3 MoveDirection;
    }

    // Данные о ближайшем противнике, подготовленные в ECS для отображения в UI
    public struct NearestSlimeInfo : IComponentData
    {
        public float Distance;
        public float Health;
        public bool HasTarget;
    }
}
