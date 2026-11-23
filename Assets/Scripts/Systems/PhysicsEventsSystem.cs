using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace SlimeSwarm
{
    // Система обработки физических коллизий в мире DOTS
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct PhysicsEventsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<TargetTowerTag>();
            state.RequireForUpdate<SlimeTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

            // Запускаем джоб обработки произошедших столкновений
            var job = new SlimeCollisionEventJob
            {
                TowerLookup = SystemAPI.GetComponentLookup<TargetTowerTag>(true),
                SlimeLookup = SystemAPI.GetComponentLookup<SlimeTag>(true),
                HealthLookup = SystemAPI.GetComponentLookup<HealthComponent>(false)
            };

            state.Dependency = job.Schedule(simulation, state.Dependency);
        }
    }

    // Обработчик событий физических контактов (ICollisionEventsJob)
    [BurstCompile]
    public partial struct SlimeCollisionEventJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentLookup<TargetTowerTag> TowerLookup;
        [ReadOnly] public ComponentLookup<SlimeTag> SlimeLookup;
        public ComponentLookup<HealthComponent> HealthLookup;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.EntityA;
            Entity entityB = collisionEvent.EntityB;

            // Проверяем факт столкновения башни и слизня в обоих направлениях
            if (TowerLookup.HasComponent(entityA) && SlimeLookup.HasComponent(entityB))
            {
                ApplyCollisionDamage(entityA, entityB);
            }
            else if (TowerLookup.HasComponent(entityB) && SlimeLookup.HasComponent(entityA))
            {
                ApplyCollisionDamage(entityB, entityA);
            }
        }

        private void ApplyCollisionDamage(Entity towerEntity, Entity slimeEntity)
        {
            // Башня получает урон при прямом физическом ударе слизня
            if (HealthLookup.TryGetRefRW(towerEntity, out RefRW<HealthComponent> towerHealth))
            {
                towerHealth.ValueRW.Current = math.max(0f, towerHealth.ValueRO.Current - 5f);
            }

            // Слизень разбивается при столкновении с башней
            if (HealthLookup.TryGetRefRW(slimeEntity, out RefRW<HealthComponent> slimeHealth))
            {
                slimeHealth.ValueRW.Current = 0f;
            }
        }
    }
}
