using Unity.Entities;
using UnityEngine;

namespace SlimeSwarm
{
    // Компонент авторинга для префаба слизня
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
                // Запекаем сущность с возможностью динамического перемещения
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // Добавляем тег слизня
                AddComponent(entity, new SlimeTag());

                // Инициализируем компоненты здоровья и параметров перемещения
                AddComponent(entity, new HealthComponent
                {
                    Current = authoring.maxHealth,
                    Max = authoring.maxHealth
                });

                AddComponent(entity, new SlimeMovementComponent
                {
                    MoveSpeed = authoring.moveSpeed,
                    AttackRange = authoring.attackRange
                });

                // Выключаемый тег смерти — изначально деактивирован
                AddComponent(entity, new DeadTag());
                SetComponentEnabled<DeadTag>(entity, false);
            }
        }
    }
}
