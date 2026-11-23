using Unity.Entities;

namespace SlimeSwarm
{
    // Система передачи редких событий из мира Entity в мир GameObject
    // Работает без Burst для вызова стандартных C# событий и делегатов
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WaveNotificationSystem : ISystem
    {
        private int _lastNotifiedWave;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WavesHolder>();
            _lastNotifiedWave = 0;
        }

        public void OnUpdate(ref SystemState state)
        {
            var wavesHolder = SystemAPI.GetSingleton<WavesHolder>();
            int currentWave = wavesHolder.CurrentWaveIndex;

            // При наступлении новой волны вызываем событие для классического мира Unity
            if (currentWave > _lastNotifiedWave)
            {
                _lastNotifiedWave = currentWave;
                GameEvents.TriggerWaveCompleted(currentWave);
            }
        }
    }
}
