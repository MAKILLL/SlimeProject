using Unity.Entities;
using UnityEngine;

namespace SlimeSwarm
{
    // Компонент авторинга для защитной башни
    [DisallowMultipleComponent]
    public class TargetTowerAuthoring : MonoBehaviour
    {
        [Header("Параметры башни")]
        public float health = 10000f;
        public float attackRadius = 16f;
        public float damagePerSecond = 80f;

        public class TargetTowerBaker : Baker<TargetTowerAuthoring>
        {
            public override void Bake(TargetTowerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // Тег башни-цели для наведения слизней
                AddComponent(entity, new TargetTowerTag());

                // Компонент с характеристиками башни
                AddComponent(entity, new TowerComponent
                {
                    Health = authoring.health,
                    MaxHealth = authoring.health,
                    AttackRadius = authoring.attackRadius,
                    DamagePerSecond = authoring.damagePerSecond
                });

                // Буфер для сбора входящего урона
                AddBuffer<DamageBufferElement>(entity);
            }
        }
    }
}
