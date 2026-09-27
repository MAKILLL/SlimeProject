using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SlimeSwarm
{
    [DisallowMultipleComponent]
    public class SlimeAuthoring : MonoBehaviour
    {
        [Header("Здоровье")]
        public float maxHealth = 100f;

        [Header("Стамина")]
        public float maxStamina = 50f;
        public float staminaRecoveryRate = 35f;
        public float jumpCost = 15f;

        [Header("Движение")]
        public float moveSpeed = 4.5f;
        public float jumpHeight = 1.2f;
        public float attackRange = 2.0f;
        public float avoidanceRadius = 0.8f;

        [Header("Анимация Squash & Stretch")]
        public float squashAmount = 0.35f;
        public float bounceSpeed = 8.0f;

        public class SlimeBaker : Baker<SlimeAuthoring>
        {
            public override void Bake(SlimeAuthoring authoring)
            {
                // Запекаем Entity с поддержкой динамического перемещения и неравномерного масштабирования
                Entity entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.NonUniformScale);

                AddComponent(entity, new SlimeTag());

                AddComponent(entity, new HealthComponent
                {
                    Current = authoring.maxHealth,
                    Max = authoring.maxHealth
                });

                AddComponent(entity, new StaminaComponent
                {
                    Current = authoring.maxStamina,
                    Max = authoring.maxStamina,
                    RecoveryRate = authoring.staminaRecoveryRate,
                    JumpCost = authoring.jumpCost
                });

                AddComponent(entity, new SlimeMovementComponent
                {
                    MoveSpeed = authoring.moveSpeed,
                    JumpHeight = authoring.jumpHeight,
                    AttackRange = authoring.attackRange,
                    AvoidanceRadius = authoring.avoidanceRadius,
                    Velocity = float3.zero,
                    JumpProgress = 0f
                });

                AddComponent(entity, new SlimeStateComponent
                {
                    State = SlimeState.Spawning,
                    StateTimer = 0f,
                    TargetStateTime = 0.4f
                });

                AddComponent(entity, new SlimeAnimationComponent
                {
                    BaseScale = new float3(1f, 1f, 1f),
                    SquashAmount = authoring.squashAmount,
                    BounceSpeed = authoring.bounceSpeed,
                    PhaseOffset = 0f
                });

                // Выключаемый компонент DeadTag по методичке (стартовое состояние = false)
                AddComponent(entity, new DeadTag());
                SetComponentEnabled<DeadTag>(entity, false);
            }
        }
    }
}
