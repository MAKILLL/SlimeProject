using Unity.Entities;

namespace SlimeSwarm
{
    // =========================================================================
    // ПРАКТИКА №2: КАСТОМНАЯ ГРУППА СИСТЕМ (ComponentSystemGroup)
    // Слайды 9-10 методички №2:
    // Позволяет контролировать частоту вызова систем.
    // Вместо вызова каждый кадр (60-200 раз в секунду) данная группа
    // вызывает вложенные системы ровно 1 раз в секунду, экономя ресурсы процессора.
    // =========================================================================
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
                _timer -= 1.0f;
            }
        }
    }
}
