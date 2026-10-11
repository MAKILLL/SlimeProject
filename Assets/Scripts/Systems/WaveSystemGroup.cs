using Unity.Entities;

namespace SlimeSwarm
{
    // Кастомная группа систем по методичке (стр. 9-10)
    // Управляет частотой выполнения вложенных систем (срабатывает раз в 1 секунду)
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class WaveSystemGroup : ComponentSystemGroup
    {
        private float _timer = 0f;

        protected override void OnUpdate()
        {
            _timer += SystemAPI.Time.DeltaTime;

            // Вызываем выполнение систем внутри группы раз в секунду
            if (_timer >= 1.0f)
            {
                base.OnUpdate();
                _timer = 0f;
            }
        }
    }
}
