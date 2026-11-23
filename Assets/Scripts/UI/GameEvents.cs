using System;

namespace SlimeSwarm
{
    // Статический мост для передачи редких игровых событий из мира Entity в классический мир GameObject
    public static class GameEvents
    {
        // Событие завершения волны противников (номер волны)
        public static event Action<int> OnWaveCompleted;

        // Метод вызова события из ECS-систем
        public static void TriggerWaveCompleted(int waveIndex)
        {
            OnWaveCompleted?.Invoke(waveIndex);
        }

        // Событие критического состояния башни (текущее здоровье)
        public static event Action<float> OnTowerHealthLow;

        public static void TriggerTowerHealthLow(float health)
        {
            OnTowerHealthLow?.Invoke(health);
        }
    }
}
