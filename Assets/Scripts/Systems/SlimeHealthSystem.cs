using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SlimeSwarm
{
    // Система проверки здоровья и удаления погибших слизней
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TowerDefenseSystem))]
    public partial struct SlimeHealthSystem : ISystem
    {
        private EntityQuery _aliveSlimesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SlimeTag>();

            // Запрос только для живых слизней (компонент DeadTag выключен)
            _aliveSlimesQuery = SystemAPI.QueryBuilder()
                .WithAll<HealthComponent, SlimeTag>()
                .WithDisabled<DeadTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Буфер команд для безопасной модификации сущностей из параллельных потоков
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var job = new SlimeDeathJob
            {
                Ecb = ecb.AsParallelWriter()
            };

            // Запускаем джоб проверки смертей
            state.Dependency = job.ScheduleParallel(_aliveSlimesQuery, state.Dependency);

            // Ожидаем завершения джоба перед применением команд изменений
            state.Dependency.Complete();

            // Применяем команды включения DeadTag и удаления сущностей
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    // Параллельный джоб выявления погибших сущностей
    [BurstCompile]
    public partial struct SlimeDeathJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        // chunkIndexInQuery обеспечивает детерминированный порядок записи в буфер
        public void Execute(
            [ChunkIndexInQuery] int chunkIndexInQuery,
            Entity entity,
            in HealthComponent health)
        {
            if (health.Current <= 0f)
            {
                // Активируем маркер гибели и удаляем сущность
                Ecb.SetComponentEnabled<DeadTag>(chunkIndexInQuery, entity, true);
                Ecb.DestroyEntity(chunkIndexInQuery, entity);
            }
        }
    }
}
