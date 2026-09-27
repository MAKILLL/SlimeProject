using Unity.Entities;
using UnityEngine;

namespace SlimeSwarm
{
    [DisallowMultipleComponent]
    public class TargetTowerAuthoring : MonoBehaviour
    {
        [Header("Башня Обороны")]
        public float health = 10000f;
        public float attackRadius = 15f;
        public float damagePerSecond = 50f;

        public class TargetTowerBaker : Baker<TargetTowerAuthoring>
        {
            public override void Bake(TargetTowerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new TargetTowerTag());

                AddComponent(entity, new TowerComponent
                {
                    Health = authoring.health,
                    MaxHealth = authoring.health,
                    AttackRadius = authoring.attackRadius,
                    DamagePerSecond = authoring.damagePerSecond
                });
            }
        }
    }
}
