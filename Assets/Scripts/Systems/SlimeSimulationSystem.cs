using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SlimeSwarm
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SlimeSimulationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SlimeTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;

            // Безопасно находим целевую башню (без исключений при дубликатах)
            float3 targetPos = float3.zero;
            bool hasTarget = false;

            foreach (var (towerTransform, towerTag) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<TargetTowerTag>>())
            {
                targetPos = towerTransform.ValueRO.Position;
                hasTarget = true;
                break; // Берем первую башню
            }

            // Запускаем параллельный мультипоточный Burst Job на всех ядрах процессора
            var job = new SlimeSwarmParallelJob
            {
                DeltaTime = deltaTime,
                ElapsedTime = elapsedTime,
                TargetPosition = targetPos,
                HasTarget = hasTarget
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct SlimeSwarmParallelJob : IJobEntity
    {
        public float DeltaTime;
        public float ElapsedTime;
        public float3 TargetPosition;
        public bool HasTarget;

        public void Execute(
            ref LocalTransform transform,
            ref SlimeMovementComponent movement,
            ref SlimeStateComponent slimeState,
            ref StaminaComponent stamina,
            in SlimeAnimationComponent anim)
        {
            slimeState.StateTimer += DeltaTime;

            // 1. Конечный автомат поведения (FSM)
            switch (slimeState.State)
            {
                case SlimeState.Spawning:
                    // Плавное восстановление стамины
                    stamina.Current = math.min(stamina.Max, stamina.Current + stamina.RecoveryRate * DeltaTime);

                    if (slimeState.StateTimer >= slimeState.TargetStateTime)
                    {
                        slimeState.State = SlimeState.Jumping;
                        slimeState.StateTimer = 0f;
                    }
                    break;

                case SlimeState.Jumping:
                    // Расход стамины
                    stamina.Current = math.max(0f, stamina.Current - stamina.JumpCost * DeltaTime);

                    // Проверка дистанции атаки
                    if (HasTarget)
                    {
                        float distSq = math.distancesq(transform.Position, TargetPosition);
                        if (distSq <= movement.AttackRange * movement.AttackRange)
                        {
                            slimeState.State = SlimeState.Attacking;
                            slimeState.StateTimer = 0f;
                            break;
                        }
                    }

                    // Переход в отдых при нулевой выносливости
                    if (stamina.Current <= 0.1f)
                    {
                        slimeState.State = SlimeState.Resting;
                        slimeState.StateTimer = 0f;
                    }
                    break;

                case SlimeState.Resting:
                    // Регенерация стамины
                    stamina.Current = math.min(stamina.Max, stamina.Current + stamina.RecoveryRate * DeltaTime);

                    if (stamina.Current >= stamina.Max * 0.95f)
                    {
                        slimeState.State = SlimeState.Jumping;
                        slimeState.StateTimer = 0f;
                    }
                    break;

                case SlimeState.Attacking:
                    stamina.Current = math.max(0f, stamina.Current - (stamina.JumpCost * 0.5f) * DeltaTime);

                    if (HasTarget)
                    {
                        float distSq = math.distancesq(transform.Position, TargetPosition);
                        if (distSq > (movement.AttackRange + 1.5f) * (movement.AttackRange + 1.5f))
                        {
                            slimeState.State = SlimeState.Jumping;
                            slimeState.StateTimer = 0f;
                        }
                    }
                    break;
            }

            // 2. Движение и кинематика прыжка
            float squashY = 1.0f;

            if (slimeState.State == SlimeState.Jumping && HasTarget)
            {
                float3 toTarget = TargetPosition - transform.Position;
                toTarget.y = 0f;

                float dist = math.length(toTarget);
                if (dist > 0.05f)
                {
                    float3 dir = toTarget / dist;

                    // Поворот к цели
                    quaternion targetRot = quaternion.LookRotationSafe(dir, new float3(0, 1, 0));
                    transform.Rotation = math.slerp(transform.Rotation, targetRot, DeltaTime * 8f);

                    // Фаза прыжка
                    movement.JumpProgress += DeltaTime * (movement.MoveSpeed * 0.8f);
                    if (movement.JumpProgress >= 1f)
                    {
                        movement.JumpProgress -= 1f;
                    }

                    float jumpT = movement.JumpProgress;
                    float forwardSpeed = math.sin(jumpT * math.PI) * movement.MoveSpeed;

                    float3 nextPos = transform.Position + dir * (forwardSpeed * DeltaTime);
                    nextPos.y = math.sin(jumpT * math.PI) * movement.JumpHeight;

                    transform.Position = nextPos;

                    // Сжатие / растяжение в полете
                    squashY = 1.0f + math.sin(jumpT * math.PI * 2f) * anim.SquashAmount;
                }
            }
            else if (slimeState.State == SlimeState.Resting)
            {
                float3 pos = transform.Position;
                pos.y = math.lerp(pos.y, 0f, DeltaTime * 8f);
                transform.Position = pos;

                // Дыхание
                squashY = 1.0f + math.sin(ElapsedTime * 4f + anim.PhaseOffset) * 0.12f;
            }
            else if (slimeState.State == SlimeState.Spawning)
            {
                float spawnT = math.clamp(slimeState.StateTimer / slimeState.TargetStateTime, 0.1f, 1f);
                squashY = spawnT * (1f + math.sin(spawnT * math.PI * 3f) * 0.2f);
            }
            else if (slimeState.State == SlimeState.Attacking)
            {
                squashY = 1.0f + math.sin(ElapsedTime * 10f + anim.PhaseOffset) * 0.2f;
            }

            // 3. Применение процедурной анимации масштаба
            transform.Scale = math.clamp(0.6f * squashY, 0.2f, 1.2f);
        }
    }
}
