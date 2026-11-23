using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SlimeSwarm
{
    // Компонент MonoBehaviour для сбора пользовательского ввода и передачи его в мир Entity
    public class PlayerInputController : MonoBehaviour
    {
        private EntityManager _entityManager;
        private EntityQuery _inputQuery;
        private bool _isInitialized = false;

        private void Start()
        {
            InitializeEntityManager();
        }

        private void InitializeEntityManager()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                _entityManager = world.EntityManager;
                _inputQuery = _entityManager.CreateEntityQuery(typeof(PlayerInputData));

                // Если сущность ввода еще не существует в мире ECS — создаем её
                if (_inputQuery.CalculateEntityCount() == 0)
                {
                    Entity inputEntity = _entityManager.CreateEntity(typeof(PlayerInputData));
                    _entityManager.SetComponentData(inputEntity, new PlayerInputData
                    {
                        MoveDirection = float3.zero
                    });
                }

                _isInitialized = true;
            }
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                InitializeEntityManager();
                if (!_isInitialized) return;
            }

            float h = 0f;
            float v = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                var kb = Keyboard.current;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (h == 0f && v == 0f)
            {
                h = Input.GetAxisRaw("Horizontal");
                v = Input.GetAxisRaw("Vertical");
            }
#endif

            float3 direction = new float3(h, 0f, v);
            if (math.lengthsq(direction) > 0.001f)
            {
                direction = math.normalize(direction);
            }

            // Записываем чистые данные в синглтон-компонент мира ECS
            if (_inputQuery.CalculateEntityCount() > 0)
            {
                Entity inputEntity = _inputQuery.GetSingletonEntity();
                _entityManager.SetComponentData(inputEntity, new PlayerInputData
                {
                    MoveDirection = direction
                });
            }
        }
    }
}
