using Unity.Entities;
using Unity.Mathematics;

namespace SlimeSwarm
{
    // 1. Тег слизня (позволяет быстро фильтровать сущности в Query)
    public struct SlimeTag : IComponentData { }

    // 2. Тег цели — центральная башня (синглтон)
    public struct TargetTowerTag : IComponentData { }

    // 3. Состояния поведения слизня (Конечный автомат / FSM)
    public enum SlimeState : byte
    {
        Spawning = 0, // Появление / рождение
        Jumping = 1,  // Прыжок и движение к цели
        Resting = 2,  // Отдых и регенерация стамины
        Attacking = 3,// Атака башни
        Dead = 4      // Гибель
    }

    public struct SlimeStateComponent : IComponentData
    {
        public SlimeState State;
        public float StateTimer;
        public float TargetStateTime;
    }

    // 4. Здоровье слизня и башни
    public struct HealthComponent : IComponentData
    {
        public float Current;
        public float Max;
    }

    // 5. Выносливость (стамины)
    public struct StaminaComponent : IComponentData
    {
        public float Current;
        public float Max;
        public float RecoveryRate; // Восстановление стамины в сек
        public float JumpCost;     // Расход стамины на один прыжок
    }

    // 6. Характеристики движения и навигации
    public struct SlimeMovementComponent : IComponentData
    {
        public float MoveSpeed;
        public float JumpHeight;
        public float AttackRange;
        public float AvoidanceRadius;
        public float3 Velocity;
        public float JumpProgress; // 0..1 фаза текущего прыжка
    }

    // 7. Процедурная анимация Squash & Stretch
    public struct SlimeAnimationComponent : IComponentData
    {
        public float3 BaseScale;
        public float SquashAmount;
        public float BounceSpeed;
        public float PhaseOffset;
    }

    // 8. Параметры спавнера
    public struct SlimeSpawnerComponent : IComponentData
    {
        public Entity SlimePrefab;
        public int SpawnCount;
        public float SpawnRadius;
        public float SpawnInterval;
        public float Timer;
        public bool ContinuousSpawn;
    }

    // 9. Параметры защитной башни
    public struct TowerComponent : IComponentData
    {
        public float Health;
        public float MaxHealth;
        public float AttackRadius;
        public float DamagePerSecond;
    }

    // 10. Выключаемый компонент DeadTag (IEnableableComponent из методички)
    public struct DeadTag : IComponentData, IEnableableComponent { }
}
