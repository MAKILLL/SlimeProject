using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SlimeSwarm
{
    // =========================================================================
    // ПРАКТИКА №3: ПАРАЛЛЕЛЬНАЯ СИСТЕМА ЗДОРОВЬЯ И ГИБЕЛИ (IJobEntity + ParallelWriter)
    // Слайды 4, 5, 6 методички:
    // 1. Слайд 4: Тонкая настройка выборки через QueryBuilder (WithDisabled<DeadTag>).
    // 2. Слайд 5: Параллельная запись команд изменения мира через EntityCommandBuffer.ParallelWriter.
    // 3. Слайд 5: Атрибут [ChunkIndexInQuery] для детерминированного порядка выполнения команд.
    // 4. Слайд 6: Принудительное ожидание state.Dependency.Complete() перед ecb.Playback().
    // =========================================================================
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

            // Слайд 4: Создаем EntityQuery с помощью QueryBuilder:
            // выбираем только живых сущностей (у которых выключаемый компонент DeadTag отключен)
            _aliveSlimesQuery = SystemAPI.QueryBuilder()
                .WithAll<HealthComponent, SlimeTag>()
                .WithDisabled<DeadTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Слайд 5: Создаем EntityCommandBuffer в TempJob памяти для передачи в джоб
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Слайд 5: Передаем параллельный писатель ecb.AsParallelWriter() в джобу
            var job = new SlimeDeathJob
            {
                Ecb = ecb.AsParallelWriter()
            };

            // Слайд 4: Передаем отфильтрованный _aliveSlimesQuery в ScheduleParallel
            state.Dependency = job.ScheduleParallel(_aliveSlimesQuery, state.Dependency);

            // Слайд 6: Обязательно ждем завершения параллельного джоба перед Playback,
            // чтобы все параллельные потоки успели записать свои команды
            state.Dependency.Complete();

            // Воспроизводим команды (включение тега DeadTag и удаление) на главном потоке
            ecb.Playback(state.EntityManager);

            // Освобождаем память одноразового буфера
            ecb.Dispose();
        }
    }

    // Слайд 5: Параллельный джоб проверки здоровья
    [BurstCompile]
    public partial struct SlimeDeathJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        // Слайд 5: [ChunkIndexInQuery] позволяет упорядочить команды из разных потоков
        public void Execute(
            [ChunkIndexInQuery] int chunkIndexInQuery,
            Entity entity,
            in HealthComponent health)
        {
            // Если здоровье слизня опустилось до нуля или ниже
            if (health.Current <= 0f)
            {
                // Слайд 5: Включаем компонент DeadTag через ParallelWriter
                Ecb.SetComponentEnabled<DeadTag>(chunkIndexInQuery, entity, true);

                // Записываем команду удаления погибшей сущности
                Ecb.DestroyEntity(chunkIndexInQuery, entity);
            }
        }
    }
}
