using Unity.Entities;

namespace SlimeSwarm
{
    // Кастомная группа систем с тактированием 1 раз в секунду
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class WaveSystemGroup : ComponentSystemGroup
    {
        private float _timer = 0f;

        protected override void OnUpdate()
        {
            _timer += SystemAPI.Time.DeltaTime;

            // Выполняем вложенные системы с фиксированным интервалом
            if (_timer >= 1.0f)
            {
                base.OnUpdate();
                _timer -= 1.0f;
            }
        }
    }
}
