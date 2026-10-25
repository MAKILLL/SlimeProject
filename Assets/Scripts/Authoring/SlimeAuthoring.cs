using Unity.Entities;
using UnityEngine;

namespace SlimeSwarm
{
    // =========================================================================
    // АВТОРИНГ СЛИЗНЯ (ПРАКТИКИ №1-3)
    // Минимальный набор данных для студенческого проекта:
    // - SlimeTag (тег)
    // - HealthComponent (здоровье)
    // - SlimeMovementComponent (скорость и радиус атаки)
    // - DeadTag (выключаемый маркер гибели)
    // =========================================================================
    [DisallowMultipleComponent]
    public class SlimeAuthoring : MonoBehaviour
    {
        [Header("Здоровье")]
        public float maxHealth = 100f;

        [Header("Движение")]
        public float moveSpeed = 4.5f;
        public float attackRange = 2.0f;

        public class SlimeBaker : Baker<SlimeAuthoring>
        {
            public override void Bake(SlimeAuthoring authoring)
            {
                // Запекаем Entity с динамической трансформацией
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // Тег слизня для быстрого поиска в EntityQuery
                AddComponent(entity, new SlimeTag());

                // Компонент здоровья
                AddComponent(entity, new HealthComponent
                {
                    Current = authoring.maxHealth,
                    Max = authoring.maxHealth
                });

                // Компонент движения
                AddComponent(entity, new SlimeMovementComponent
                {
                    MoveSpeed = authoring.moveSpeed,
                    AttackRange = authoring.attackRange
                });

                // Выключаемый компонент DeadTag (IEnableableComponent)
                // На старте выключен (SetComponentEnabled = false).
                // Включается в HealthJob при Current <= 0.
                AddComponent(entity, new DeadTag());
                SetComponentEnabled<DeadTag>(entity, false);
            }
        }
    }
}
